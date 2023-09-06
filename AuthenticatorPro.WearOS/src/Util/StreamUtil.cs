// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Threading.Tasks;
using Java.IO;

namespace AuthenticatorPro.WearOS.Util
{
    internal static class StreamUtil
    {
        private const int BufferSize = 1024;

        public static async Task<byte[]> ReadAllBytesAsync(InputStream stream)
        {
            var buffer = new byte[BufferSize];
            var offset = 0;
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, offset, BufferSize)) > -1)
            {
                Array.Resize(ref buffer, buffer.Length + BufferSize);
                offset += bytesRead;
            }

            return buffer;
        }
    }
}