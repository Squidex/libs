// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets
{
    public static class TempHelper
    {
        public static FileStream GetTempStream()
        {
            // Path.GetTempFileName scans for a free name, creates the file and gives up after 65535
            // files in the temp folder. A random name has neither problem.
            var tempFileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

            return new FileStream(tempFileName,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.Delete,
                1024 * 16,
                FileOptions.Asynchronous |
                FileOptions.DeleteOnClose |
                FileOptions.SequentialScan);
        }
    }
}
