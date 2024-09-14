// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;
using Google.Android.Material.TextField;

namespace Stratum.Droid.Util
{
    internal static class TextInputUtil
    {
        public static void EnableAutoErrorClear(IEnumerable<TextInputLayout> layouts)
        {
            foreach (var layout in layouts)
            {
                EnableAutoErrorClear(layout);
            }
        }

        public static void EnableAutoErrorClear(TextInputLayout layout)
        {
            layout.EditText.TextChanged += delegate
            {
                if (!string.IsNullOrEmpty(layout.Error))
                {
                    layout.Error = null;
                }
            };
        }
    }
}