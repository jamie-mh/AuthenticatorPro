// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

namespace Stratum.Core.Generator
{
    public interface IGenerator
    {
        public string Compute(long counter);
    }
}