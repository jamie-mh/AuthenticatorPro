// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Entity;
using System.Threading.Tasks;

namespace AuthenticatorPro.Core.Service
{
    public interface IIconPackService
    {
        public Task ImportPackAsync(IconPack pack);
        public Task DeletePackAsync(IconPack pack);
    }
}