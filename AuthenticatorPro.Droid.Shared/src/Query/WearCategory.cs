// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

namespace AuthenticatorPro.Droid.Shared.Query
{
    public class WearCategory
    {
        public readonly string Id;
        public readonly string Name;

        public WearCategory(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}