// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Droid.Shared.Data;

namespace AuthenticatorPro.Droid.Shared.Query
{
    public class WearPreferences
    {
        public readonly string DefaultCategory;
        public readonly SortMode SortMode;

        public WearPreferences(string defaultCategory, SortMode sortMode)
        {
            DefaultCategory = defaultCategory;
            SortMode = sortMode;
        }
    }
}