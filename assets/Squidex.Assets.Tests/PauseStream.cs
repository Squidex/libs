// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets;

public class PauseStream : DelegateStream
{
    private double pauseAfter = 1;
    private int totalRead;

    public PauseStream(Stream innerStream, double pauseAfter)
        : base(innerStream)
    {
        this.pauseAfter = pauseAfter;
    }

    public void Reset(double? newPauseAfter = null)
    {
        totalRead = 0;

        if (newPauseAfter.HasValue)
        {
            pauseAfter = newPauseAfter.Value;
        }
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        if (totalRead >= Length * pauseAfter)
        {
            return 0;
        }

        var bytesRead = await base.ReadAsync(buffer, cancellationToken);

        totalRead += bytesRead;

        return bytesRead;
    }
}
