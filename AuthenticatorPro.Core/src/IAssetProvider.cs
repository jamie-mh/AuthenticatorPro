// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Threading.Tasks;

namespace AuthenticatorPro.Core
{
    public interface IAssetProvider
    {
        public Task<byte[]> ReadBytesAsync(string path);
        public Task<string> ReadStringAsync(string path);
    }
}