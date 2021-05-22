// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.OS;
using AndroidX.Preference;

namespace AuthenticatorPro.Droid.Fragment
{
    internal class SettingsFragment : PreferenceFragmentCompat
    {
        public event EventHandler PreferencesCreated;
        
        public override void OnCreatePreferences(Bundle savedInstanceState, string rootKey)
        {
            AddPreferencesFromResource(Resource.Xml.settings);
            PreferencesCreated?.Invoke(this, null);
        }
    }
}