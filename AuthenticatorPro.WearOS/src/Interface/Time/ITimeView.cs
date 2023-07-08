// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Views;

namespace AuthenticatorPro.WearOS.Interface.Time
{
    public interface ITimeView
    {
        public string Text { get; set; }
        public ViewStates Visibility { get; set; }
    }
}