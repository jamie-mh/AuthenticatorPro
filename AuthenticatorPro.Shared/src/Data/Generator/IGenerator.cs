// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

namespace AuthenticatorPro.Shared.Data.Generator
{
    public interface IGenerator
    {
        public string Compute(long counter);
    }
}