// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Threading.Tasks;
using Stratum.Core.Entity;

namespace Stratum.Core
{
    public interface ICustomIconDecoder
    {
        public Task<CustomIcon> DecodeAsync(byte[] data, bool shouldPreProcess);
    }
}