// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Entity;
using System.Threading.Tasks;

namespace AuthenticatorPro.Shared
{
    public interface ICustomIconDecoder
    {
        public Task<CustomIcon> Decode(byte[] data);
    }
}