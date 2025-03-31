// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets;

public class PauseStream(Stream innerStream, double pauseAfter) : DelegateStream(innerStream)
{
    private double pauseTime = pauseAfter;
    private int totalRead;

    public void Reset(double? newPauseAfter = null)
    {
        totalRead = 0;

        if (newPauseAfter.HasValue)
        {
            pauseTime = newPauseAfter.Value;
        }
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        if (totalRead >= Length * pauseTime)
        {
            return 0;
        }

        var bytesRead = await base.ReadAsync(buffer, cancellationToken);

        totalRead += bytesRead;

        return bytesRead;
    }
}
