// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Threading.Tasks;
using AuthenticatorPro.Core.Entity;

namespace AuthenticatorPro.Core
{
    public interface ICustomIconDecoder
    {
        public Task<CustomIcon> DecodeAsync(byte[] data, bool shouldPreProcess);
    }
}