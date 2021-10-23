// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Data;

namespace AuthenticatorPro.Droid.Shared.Query
{
    public class WearPreferences
    {
        public readonly string DefaultCategory;
        public readonly SortMode SortMode;
        public readonly int CodeGroupSize;

        public WearPreferences(string defaultCategory, SortMode sortMode, int codeGroupSize)
        {
            DefaultCategory = defaultCategory;
            SortMode = sortMode;
            CodeGroupSize = codeGroupSize;
        }
    }
}