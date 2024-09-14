// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.Content;
using AndroidX.Preference;
using Uri = Android.Net.Uri;

namespace Stratum.Droid.Shared
{
    public abstract class BasePreferenceWrapper
    {
        protected readonly ISharedPreferences Preferences;

        protected BasePreferenceWrapper(Context context)
        {
            Preferences = PreferenceManager.GetDefaultSharedPreferences(context);
        }

        protected T GetEnumPreference<T>(string key, T defaultValue) where T : Enum
        {
            return (T) (object) Preferences.GetInt(key, (int) (object) defaultValue);
        }

        protected void SetEnumPreference<T>(string key, T value) where T : Enum
        {
            Preferences.Edit().PutInt(key, (int) (object) value).Commit();
        }

        protected bool? GetNullableBooleanPreference(string key, bool? defaultValue)
        {
            var defaultStr = defaultValue switch
            {
                null => null,
                false => "false",
                true => "true"
            };

            return Preferences.GetString(key, defaultStr) switch
            {
                null => null,
                "false" => false,
                _ => true
            };
        }

        protected void SetNullableBooleanPreference(string key, bool? value)
        {
            Preferences.Edit().PutString(key, value switch
            {
                null => null,
                false => "false",
                true => "true"
            }).Commit();
        }

        protected int? GetNullableIntPreference(string key, int? defaultValue)
        {
            var defaultStr = defaultValue switch
            {
                null => null,
                _ => defaultValue.ToString()
            };

            var result = Preferences.GetString(key, defaultStr);

            return result switch
            {
                null => null,
                _ => int.Parse(result)
            };
        }

        protected void SetNullableIntPreference(string key, int? value)
        {
            Preferences.Edit().PutString(key, value switch
            {
                null => null,
                _ => value.ToString()
            }).Commit();
        }

        protected Uri GetUriPreference(string key, Uri defaultValue)
        {
            var value = Preferences.GetString(key, null);

            return value == null
                ? defaultValue
                : Uri.Parse(value);
        }

        protected void SetUriPreference(string key, Uri value)
        {
            SetPreference(key, value?.ToString());
        }

        protected int GetStringBackedIntPreference(string key, int defaultValue)
        {
            return int.TryParse(Preferences.GetString(key, defaultValue.ToString()), out var value)
                ? value
                : defaultValue;
        }

        protected void SetPreference(string key, string value)
        {
            Preferences.Edit().PutString(key, value).Commit();
        }

        protected void SetPreference(string key, bool value)
        {
            Preferences.Edit().PutBoolean(key, value).Commit();
        }

        protected void SetPreference(string key, long value)
        {
            Preferences.Edit().PutLong(key, value).Commit();
        }
    }
}