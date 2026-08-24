# Performance Backlog — Top 20

> Items 1-14 are fixed. Item 15 is partly done (filters yes, ack batching deliberately not). The rest are open.

Findings from a read-through of the non-test source in this repository, ranked roughly by
expected impact (throughput / latency / allocations) weighted by how hot the code path is.

---

## 1. [DONE] Redis subscriber blocks on `.Wait()` for every message

`messaging/Squidex.Messaging.Redis/RedisTopicSubscription.cs:27`

```csharp
callback(new TransportResult(deserialized, null), this, default).Wait();
```

The handler runs on StackExchange.Redis' message-processing thread. Blocking it for the full
duration of the user handler (which `DelegatingConsumer` allows to run up to `ChannelOptions.Timeout`)
serialises all Redis consumption on this connection and starves the multiplexer. Sync-over-async on a
library-owned thread is also a deadlock risk.

**Fix:** make the subscription async (`subscriber.SubscribeAsync` + async handler), or push onto a
bounded `Channel<T>` and let a dedicated worker await the callback.

---

## 2. [DONE] File log writer flushes to disk on every line

`log/Squidex.Log/Internal/FileLogProcessor.cs:50`

```csharp
writer = new StreamWriter(fs, Encoding.UTF8) { AutoFlush = true };
```

`AutoFlush` forces a write syscall per log entry, which defeats the point of the background queue —
the dedicated logging thread becomes I/O bound and the bounded `BlockingCollection` (1024) starts
back-pressuring application threads under load.

**Fix:** leave `AutoFlush = false` and flush when the queue drains (`messageQueue.Count == 0`)
and/or on a timer.

---

## 3. [DONE] `ILogger` adapter never filters by level

`log/Squidex.Log/Adapter/SemanticLogLogger.cs:116`

```csharp
public bool IsEnabled(LogLevel logLevel) => true;
```

Returning `true` unconditionally tells the whole ASP.NET Core / EF Core / driver stack to materialise
every Trace and Debug message — format strings, state lists, boxed values — before `SemanticLog.Log`
drops it against `SemanticLogOptions.Level`. On a chatty framework that is a lot of wasted work per
request. The pre-scan of `state` for exceptions (lines 45-53) also runs before the level check.

**Fix:** map `logLevel` and compare against `IOptions<SemanticLogOptions>.Value.Level` in `IsEnabled`,
and move the state scan after that check.

---

## 4. [DONE] `StoreManyAsync` is a per-entry insert driven by exceptions

`messaging/Squidex.Messaging.EntityFramework/EFMessagingDataStore.cs:88-96`

Each entry does `AddAsync` + `SaveChangesAsync`, and the update path is reached only by catching
`DbUpdateException`. Since `MessagingDataProvider.UpdateAliveAsync` re-stores every local entry on a
30-second timer, the *steady state* is: one failing INSERT (throwing a `DbUpdateException`) plus one
UPDATE, per subscription, per heartbeat. Exception throw/catch is orders of magnitude more expensive
than the query it is standing in for.

**Fix:** a real upsert — `ExecuteUpdateAsync` first and insert only when it affects 0 rows, or a
provider-native `ON CONFLICT` / `MERGE` batch.

---

## 5. [DONE] `DeleteByPrefixAsync` deletes objects one request at a time

- `assets/Squidex.Assets.S3/AmazonS3AssetStore.cs:314`
- `assets/Squidex.Assets.Azure/AzureBlobAssetStore.cs:193`
- `assets/Squidex.Assets.GoogleCloud/GoogleCloudAssetStore.cs:137`

All three list the prefix, then issue one sequential `DeleteObjectAsync` / `DeleteBlobIfExistsAsync`
round trip per object. Deleting an asset folder with 10k objects is 10k serialised network round trips.

**Fix:** S3 has `DeleteObjectsAsync` (1000 keys per call); Azure has `BlobBatchClient.DeleteBlobsAsync`.
For GCS, at minimum bound the concurrency with `Parallel.ForEachAsync`.

---

## 6. [DONE] `FolderAssetStore` opens synchronous file handles

`assets/Squidex.Assets/FolderAssetStore.cs:90` and `:116`

```csharp
await using (var fileStream = file.OpenRead())          // download
await using (var fileStream = file.Open(mode, access))  // upload
```

Neither passes `FileOptions.Asynchronous`, so every `CopyToAsync` on these streams performs blocking
I/O on a thread-pool thread. Under concurrent asset traffic that causes thread-pool starvation and the
usual latency cliff. The codebase already knows the right pattern — see `TempHelper.GetTempStream()`.

**Fix:** `new FileStream(path, ..., BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan)`.

---

## 7. [DONE] `FolderAssetStore.DeleteByPrefixAsync` materialises the entire tree

`assets/Squidex.Assets/FolderAssetStore.cs:139`

```csharp
foreach (var file in directory.GetFiles("*.*", SearchOption.AllDirectories))
```

`GetFiles` returns a fully-populated `FileInfo[]` for *every file in the whole asset store* before the
first prefix comparison, then `Path.GetRelativePath` + `Replace` allocate two strings per file.

**Fix:** `EnumerateFiles` (lazy), and prefer enumerating only the subdirectory the prefix maps to.

---

## 8. [DONE] HTML transform middleware copies the response body three times

`hosting/Squidex.Hosting/Web/HtmlTransformMiddleware.cs:47,59`

```csharp
var html = Encoding.UTF8.GetString(memoryStream.ToArray());
...
var bytes = Encoding.UTF8.GetBytes(html);
```

`RecyclableMemoryStreamManager` is used to avoid allocations, and then `ToArray()` allocates a
contiguous copy of the whole body anyway — for a large SPA index page that lands on the LOH. Add the
decoded `string` and the re-encoded `byte[]` and every HTML response allocates roughly 3x its size.
This runs on every `text/html` request.

**Fix:** decode from `GetReadOnlySequence()`/`GetBuffer()` with an explicit length, and write the
transformed text into a rented buffer via `Encoding.UTF8.GetBytes(html, buffer)`.

---

## 9. [DONE] Asset range-copy uses a private pool, a small buffer, and array overloads

`assets/Squidex.Assets/StreamExtensions.cs:16,21,42`

Three problems in the one method that streams every ranged asset download:

- `ArrayPool<byte>.Create()` builds a private, lock-based pool instead of the tuned, per-core
  `ArrayPool<byte>.Shared`.
- The buffer is 8 KB; the rest of the codebase uses 81920 for stream copies. Small chunks mean more
  syscalls and more `await` state transitions per megabyte.
- The file suppresses `CA1835` and uses `ReadAsync(byte[], int, int, ct)` / `WriteAsync(byte[], ...)`,
  which allocate a `Task` per chunk instead of the `ValueTask` overloads.

**Fix:** `ArrayPool<byte>.Shared.Rent(81920)`, drop the pragma, use the `Memory<byte>` overloads.

---

## 10. [DONE] `JsonLogWriter.End()` decodes via `StreamReader` and concatenates the newline

`log/Squidex.Log/JsonLogWriter.cs:255,259`

```csharp
var json = streamReader.ReadToEnd();
if (formatLine) { json += Environment.NewLine; }
```

`ReadToEnd` on a `MemoryStream` allocates an intermediate char buffer plus the string, and `json +=`
allocates a second full copy of every log line. The result is then re-encoded to UTF-8 by the channel —
so the pooled UTF-8 bytes the writer just produced are round-tripped through a string for nothing.

**Fix (small):** `Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length)`, and append the
newline into the stream rather than onto the string.
**Fix (better):** let `ILogChannel` accept `ReadOnlyMemory<byte>` so the UTF-8 buffer flows straight
to the file/console.

---

## 11. [DONE] A new `Utf8JsonWriter` is allocated per log entry despite the writer being pooled

`log/Squidex.Log/JsonLogWriter.cs:236`

```csharp
jsonWriter = new Utf8JsonWriter(stream, formatting);
```

`JsonLogWriterFactory` goes to the trouble of pooling `JsonLogWriter` instances, then `Start()` throws
away and reallocates the most expensive part of it — the `Utf8JsonWriter` owns its own internal output
buffer. `Utf8JsonWriter.Reset(Stream)` exists precisely for this.

**Fix:** create the `Utf8JsonWriter` once in the constructor, call `Reset(stream)` in `Start()`, and
dispose it from the pool policy.

---

## 12. [DONE] Stream-prefix filter bakes prefixes into the expression tree as constants

`events/Squidex.Events.EntityFramework/FilterBuilder.cs:78`

```csharp
Expression.Call(DbLikeMethod, DbFunctions, EventStreamMember, Expression.Constant($"{prefix}%"))
```

`Expression.Constant` makes EF Core emit the prefix as a SQL *literal*, not a parameter. Every distinct
prefix (and every distinct prefix *count*) produces distinct SQL, so:

- EF's compiled-query cache fills with one entry per prefix — unbounded growth for tenant- or
  app-scoped stream names,
- the database's plan cache is polluted the same way.

**Fix:** source the value from a captured closure / member access so EF parameterises it, or collapse
the OR-of-LIKEs into a single parameterised predicate.

---

## 13. [DONE] EF read paths do not opt out of change tracking

- `events/Squidex.Events.EntityFramework/EFEventStore_Reader.cs:28,39,65,101`
- `flows/Squidex.Flows.EntityFramework/EFFlowStateStore.cs:93,107,132`
- `flows/Squidex.Flows.EntityFramework/EFCronJobStore.cs:30`
- `assets/Squidex.Assets.EntityFramework/EFAssetKeyValueStore.cs:39`

None of the read-only queries call `AsNoTracking()`. Two call sites explicitly note that the *host*
may configure `QueryTrackingBehavior.NoTracking` (`ai/Squidex.AI.EntityFramework/EFChatStore.cs:60`,
`messaging/Squidex.Messaging.EntityFramework/EFSubscription.cs:65`) — so the default assumption is that
tracking is **on**. Every entity read is then snapshotted into the change tracker: extra allocation per
row, identity-map inserts, and O(n) fixup. `QueryPendingAsync` and `QueryAllAsync` stream unbounded row
counts through one context, so the tracker grows for the whole enumeration.

**Fix:** add `.AsNoTracking()` to every read-only query. The two write paths already opt back in with
`.AsTracking()`, so the intent stays explicit in both directions.

---

## 14. [DONE] Polling event subscription issues an unbounded query per tick

`events/Squidex.Events/PollingSubscription.cs:36` → `EFEventStore_Reader.QueryAllAsync`

`QueryAllAsync` is called without `take`, so it defaults to `int.MaxValue` and EF emits
`LIMIT 2147483647`. Combined with #13 there is no upper bound on what the change tracker (and the
driver's read buffer) holds during one catch-up poll. The `await Task.Delay(100, ct)` retry loop then
runs a second, always-empty query, because the unbounded first query already drained the stream.

**Fix:** pass a real page size (e.g. 1000), loop while a page comes back full, and drop the fixed 100 ms
delay.

---

## 15. [PARTIAL] Mongo queue subscription rebuilds a LINQ expression per poll and acks one-by-one

`messaging/Squidex.Messaging.Mongo/MongoSubscription.cs:141,69,90,198`

`CreateFilter(now)` returns a fresh `Expression<Func<MongoMessage, bool>>` closing over `now` and
`queueFilter` on every poll. The Mongo driver walks and translates that tree to BSON each time — per
subscription, per polling interval, forever. Separately `OnSuccessAsync` issues one `DeleteOneAsync`
round trip per consumed message, and `PollPrefetchAsync` needs three round trips (find candidates →
`UpdateMany` → find again) to fetch one batch.

**Fix (done):** build a `FilterDefinition<MongoMessage>` once with `Builders<>.Filter` and substitute
only the timestamp. Every per-call LINQ lambda in this class is now a `FilterDefinition`, so the driver
no longer translates an expression tree on each poll and each ack.

**Not done, deliberately:** batching acks into `DeleteManyAsync`. It was implemented and then reverted.
Deferring the delete means that after a crash, acked-but-undeleted rows keep `TimeHandled` set and
linger until their TTL instead of disappearing. Nothing is lost or redelivered, but it changes the
durability characteristics of the queue for a throughput gain, which is not a trade worth making
silently in this code path. `OnSuccessAsync` still costs one round trip per message.

---

## 16. EF queue subscription: one DbContext and one message per round trip

`messaging/Squidex.Messaging.EntityFramework/EFSubscription.cs:53,67,117,138`

Each poll creates a `DbContext`, fetches a single message (`FirstOrDefaultAsync`), saves, and invokes
the callback. Each ack creates *another* `DbContext` for one `ExecuteDeleteAsync`. At n messages/sec
that is ~2n contexts and ~3n round trips, and throughput is hard-capped by the polling interval
whenever the queue is shallow.

**Fix:** claim a batch per poll (as the Mongo transport's `Prefetch` path does), reuse the context
across claim and ack within a poll, and batch the deletes.

---

## 17. `BackgroundCache` allocates an async state machine on every cache *hit*

`caching/Squidex.Caching/BackgroundCache.cs:37,50,73`

`RefreshInBackground(...).Forget()` runs on every hit. It is an `async Task` method, so each hit
allocates the state-machine box plus a `Task`, does a `ConcurrentDictionary.ContainsKey`, and awaits
`entry.Value` before it can decide — at line 87 — that the entry is still fresh and there is nothing
to do. That freshness check is a pure comparison and could gate everything.

**Fix:** check `entry.Expires > now && isValid == null` synchronously in `GetOrCreateAsync` and only
call the async method when a refresh is actually plausible.

Also at `:113`: `Math.Abs(key.GetHashCode() % locks.Length)` throws `OverflowException` when the hash
is `int.MinValue`. Use a power-of-two length and `& (locks.Length - 1)`. Same bug at
`flows/Squidex.Flows/Internal/Execution/Utils/PartitionedScheduler.cs:93`.

---

## 18. Event append costs four-plus round trips and enumerates its input twice

`events/Squidex.Events.EntityFramework/EFEventStore_Writer.cs`

- `AppendAsync` (`:76,101,102,105,106`): version query → INSERT + `SaveChanges` → `BeginTransaction` →
  `UpdatePosition` → `Commit`. Four to five round trips per commit, with the position update in a
  *separate* transaction from the insert, all wrapped in a 20-attempt retry loop.
- `AppendUnsafeAsync` (`:26,39`): `commits` is an `IEnumerable<EventCommit>` and is enumerated twice —
  once to project the entities, once for `commits.Select(x => x.Id)`. A deferred source is evaluated
  twice (double the serialisation work, and inconsistent results if it is not stable).
- `:101` uses `AddAsync`, which is only needed for value generators such as HiLo; `Add` avoids the
  async machinery.

**Fix:** materialise `commits` once into an array; combine the insert and the position update into a
single transaction.

---

## 19. Enum name round-trip on every image resize

`assets/Squidex.Assets.ImageSharp/ImageSharpThumbnailGenerator.cs:112`

```csharp
if (!Enum.TryParse<ImageSharpMode>(options.Mode.ToString(), true, out var resizeMode))
```

Per thumbnail: `Enum.ToString()` followed by a case-*insensitive* `Enum.TryParse`, which walks the
enum's name table doing string comparisons. Not free, and it silently depends on the two enums keeping
identical member names.

**Fix:** an explicit `switch` (or a `static readonly` lookup array indexed by the source enum) — faster,
and it makes the mapping contract explicit.

---

## 20. FTP client pool completes waiters synchronously while holding its lock

`assets/Squidex.Assets.FTP/FTPClientPool.cs:57,77`

`new TaskCompletionSource<...>()` is created without `TaskCreationOptions.RunContinuationsAsynchronously`,
and `Return` calls `waiting.TrySetResult(...)` **inside `lock (queue)`**. The waiter's continuation —
i.e. the caller's entire FTP upload/download — therefore runs inline on the returning thread while the
pool lock is still held, blocking every other client acquisition for that whole duration. A lock convoy,
and a deadlock risk if a continuation re-enters the pool.

Also at `:32`: `clientTask.Result` in the cancellation path is a blocking call (hence the suppressed
`MA0042`). It is guarded by a `RanToCompletion` check so it will not block in practice, but
`GetAwaiter().GetResult()` expresses that without the suppression.

**Fix:** pass `TaskCreationOptions.RunContinuationsAsynchronously` to the TCS and move `TrySetResult`
outside the lock.

---

## Runners-up

Real, but lower impact than the twenty above:

- `text/Squidex.Text/SlugifyExtensions.cs:606` and `text/Squidex.Text/HtmlExtensions.cs:24` —
  `sb.ToString().Trim(...)` allocates the string twice; trim the `StringBuilder` in place instead.
  `SlugifyExtensions.cs:580` also computes `char.ToLowerInvariant` before the diacritics lookup that
  usually makes it unnecessary.
- `log/Squidex.Log/Adapter/SemanticLogLogger.cs:88,95` — `key.Trim('{','}',' ')` plus a private
  `StringBuilder`-based `ToCamelCase` allocate two strings *per property per log entry*. Cache the
  camel-cased name per key; the set of message-template keys is small and fixed.
- `log/Squidex.Log/SemanticLog.cs:146` — `appenders.Union(Enumerable.Repeat(appender, 1))` builds a
  `HashSet` and an iterator for what is an array append, and `Union` would silently drop a duplicate
  appender. Use `[.. appenders, appender]`.
- `messaging/Squidex.Messaging/Implementation/MessagingDataProvider.cs:82` — expired entries are deleted
  one round trip at a time inside the read loop. Collect and delete in one batch after the loop.
- `messaging/Squidex.Messaging/Implementation/DelegatingConsumer.cs:133` — a linked
  `CancellationTokenSource` (with its registration) is allocated per *handler*, not per message. Hoist
  it out of the `foreach`.
- `flows/Squidex.Flows/CronJobs/Internal/DefaultCronJobManager.cs:58,198` — `CronExpression.TryParse`
  runs for every due job on every scheduler tick. Cache parsed expressions (and `TimeZoneInfo` lookups)
  in a `ConcurrentDictionary` keyed by the expression string.
- `assets/Squidex.Assets/TempHelper.cs:14` — `Path.GetTempFileName()` scans for a free name and caps out
  at 65535 files in the temp directory. Use `Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))`.
- `caching/Squidex.Caching/LRUCache.cs:59` — `cacheMap[key] = node;` re-assigns the same node reference
  `TryGetValue` just returned; a dead dictionary write on every `Set` hit.
- `events/Squidex.Events.EntityFramework/EFEventStore_Reader.cs:76` — `commit.Filtered(...).Reverse()`
  buffers each commit's events into an array; iterate the events backwards directly.
