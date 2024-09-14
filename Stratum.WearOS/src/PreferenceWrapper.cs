// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Content;
using Stratum.Core;
using Stratum.Droid.Shared;
using Stratum.Droid.Shared.Wear;

namespace Stratum.WearOS
{
    public class PreferenceWrapper : BasePreferenceWrapper
    {
        private const string DefaultCategoryKey = "defaultCategory";
        private const string DefaultCategoryDefault = null;

        public string DefaultCategory
        {
            get => Preferences.GetString(DefaultCategoryKey, DefaultCategoryDefault);
            set => SetPreference(DefaultCategoryKey, value);
        }

        private const string DefaultAuthKey = "defaultAuth";
        private static readonly string DefaultAuthDefault = null;

        public string DefaultAuth
        {
            get => Preferences.GetString(DefaultAuthKey, DefaultAuthDefault);
            set => SetPreference(DefaultAuthKey, value);
        }

        private const string SortModeKey = "sortMode";
        private const SortMode SortModeDefault = SortMode.AlphabeticalAscending;

        public SortMode SortMode
        {
            get => GetEnumPreference(SortModeKey, SortModeDefault);
            set => SetEnumPreference(SortModeKey, value);
        }

        private const string CodeGroupSizeKey = "codeGroupSize";
        private const int CodeGroupSizeDefault = 3;

        public int CodeGroupSize
        {
            get => GetStringBackedIntPreference(CodeGroupSizeKey, CodeGroupSizeDefault);
            set => SetPreference(CodeGroupSizeKey, value.ToString());
        }
        
        private const string ShowUsernamesKey = "showUsernames";
        private const bool ShowUsernamesDefault = true;

        public bool ShowUsernames
        {
            get => Preferences.GetBoolean(ShowUsernamesKey, ShowUsernamesDefault);
            set => SetPreference(ShowUsernamesKey, value);
        }

        public PreferenceWrapper(Context context) : base(context) { }

        public void ApplySyncedPreferences(WearPreferences preferences)
        {
            DefaultCategory = preferences.DefaultCategory;
            SortMode = preferences.SortMode;
            CodeGroupSize = preferences.CodeGroupSize;
            ShowUsernames = preferences.ShowUsernames;
        }
    }
}