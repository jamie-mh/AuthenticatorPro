// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Content;
using AuthenticatorPro.Droid.Shared.Query;
using AuthenticatorPro.Droid.Shared.Util;
using AuthenticatorPro.Shared.Data;

namespace AuthenticatorPro.WearOS.Util
{
    internal class PreferenceWrapper : BasePreferenceWrapper
    {
        private const string DefaultCategoryKey = "defaultCategory";
        private const string DefaultCategoryDefault = null;

        public string DefaultCategory
        {
            get => Preferences.GetString(DefaultCategoryKey, DefaultCategoryDefault);
            set => SetPreference(DefaultCategoryKey, value);
        }

        private const string DefaultAuthKey = "defaultAuth";
        private static readonly int? DefaultAuthDefault = null;

        public int? DefaultAuth
        {
            get => GetNullableIntPreference(DefaultAuthKey, DefaultAuthDefault);
            set => SetNullableIntPreference(DefaultAuthKey, value);
        }

        private const string SortModeKey = "sortMode";
        private const SortMode SortModeDefault = SortMode.AlphabeticalAscending;

        public SortMode SortMode
        {
            get => GetEnumPreference(SortModeKey, SortModeDefault);
            set => SetEnumPreference(SortModeKey, value);
        }

        public PreferenceWrapper(Context context) : base(context) { }

        public void ApplySyncedPreferences(WearPreferences preferences)
        {
            DefaultCategory = preferences.DefaultCategory;
            SortMode = preferences.SortMode;
        }
    }
}