// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared;
using System.Text.RegularExpressions;

namespace AuthenticatorPro.Droid.Shared
{
    public class IconResolver : IIconResolver
    {
        public const string Default = "default";

        public static int GetService(string key, bool isDark)
        {
            if (isDark)
            {
                if (key == null)
                {
                    return IconMap.ServiceDark[Default];
                }

                if (IconMap.ServiceDark.TryGetValue(key, out var darkIcon))
                {
                    return darkIcon;
                }

                return IconMap.Service.TryGetValue(key, out var fallbackLightIcon)
                    ? fallbackLightIcon
                    : IconMap.ServiceDark[Default];
            }

            if (key == null)
            {
                return IconMap.Service[Default];
            }

            return IconMap.Service.TryGetValue(key, out var lightIcon)
                ? lightIcon
                : IconMap.Service[Default];
        }

        public string FindServiceKeyByName(string name)
        {
            static string Simplify(string input)
            {
                input = input.ToLower();
                input = Regex.Replace(input, @"[^a-z0-9]", "");
                return input.Trim();
            }

            var key = Simplify(name);

            if (IconMap.Service.ContainsKey(key))
            {
                return key;
            }

            var firstWordKey = Simplify(name.Split(new[] { ' ', '.' }, 2)[0]);

            return IconMap.Service.ContainsKey(firstWordKey)
                ? firstWordKey
                : null;
        }
    }
}