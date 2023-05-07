// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Entity;
using System.Threading.Tasks;

namespace AuthenticatorPro.Core
{
    public interface ICustomIconDecoder
    {
        public Task<CustomIcon> DecodeAsync(byte[] data, bool shouldPreProcess);
    }
}