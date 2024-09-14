// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;

namespace Stratum.Droid.Interface
{
    public class SheetMenuItem
    {
        public readonly int Icon;
        public readonly int Title;
        public readonly EventHandler Handler;
        public readonly int? Description;
        public readonly bool IsSensitive;

        public SheetMenuItem(int icon, int title, EventHandler handler, int? description = null,
            bool isSensitive = false)
        {
            Icon = icon;
            Title = title;
            Handler = handler;
            Description = description;
            IsSensitive = isSensitive;
        }
    }
}