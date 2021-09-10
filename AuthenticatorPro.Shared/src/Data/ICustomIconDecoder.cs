// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Entity;
using System.Threading.Tasks;

namespace AuthenticatorPro.Shared.Data
{
    public interface ICustomIconDecoder
    {
        public Task<CustomIcon> Decode(byte[] data);
    }
}