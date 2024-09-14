// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.OS;
using AndroidX.Preference;

namespace Stratum.Droid.Interface.Fragment
{
    public class SettingsFragment : PreferenceFragmentCompat
    {
        public event EventHandler PreferencesCreated;

        public override void OnCreatePreferences(Bundle savedInstanceState, string rootKey)
        {
            AddPreferencesFromResource(Resource.Xml.settings);
            PreferencesCreated?.Invoke(this, EventArgs.Empty);
        }
    }
}