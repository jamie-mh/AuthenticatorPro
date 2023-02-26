// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared;

namespace AuthenticatorPro.Droid.Shared.Wear
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