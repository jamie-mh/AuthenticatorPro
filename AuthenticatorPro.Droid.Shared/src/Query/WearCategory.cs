// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

namespace AuthenticatorPro.Droid.Shared.Query
{
    public class WearCategory
    {
        public readonly string Id;
        public readonly string Name;
        public readonly int Ranking;

        public WearCategory(string id, string name, int ranking)
        {
            Id = id;
            Name = name;
            Ranking = ranking;
        }
    }
}