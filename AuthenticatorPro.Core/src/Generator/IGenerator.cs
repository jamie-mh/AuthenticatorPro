// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

namespace AuthenticatorPro.Core.Generator
{
    public interface IGenerator
    {
        public string Compute(long counter);
    }
}