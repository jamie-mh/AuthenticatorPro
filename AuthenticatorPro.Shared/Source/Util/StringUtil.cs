// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;

namespace AuthenticatorPro.Shared.Util
{
    public static class StringUtil
    {
        public static string Truncate(this string value, int maxLength)
        {
            if(String.IsNullOrEmpty(value))
                return value;

            return value.Length <= maxLength ? value : value[..maxLength];
        }
    }
}