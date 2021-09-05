// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Google.Android.Material.Snackbar;
using System;

namespace AuthenticatorPro.Droid.Callback
{
    public class SnackbarCallback : Snackbar.Callback
    {
        public event EventHandler<int> Dismissed;
        public event EventHandler Shown;

        public override void OnDismissed(Snackbar transientBottomBar, int e)
        {
            base.OnDismissed(transientBottomBar, e);
            Dismissed?.Invoke(transientBottomBar, e);
        }

        public override void OnShown(Snackbar sb)
        {
            base.OnShown(sb);
            Shown?.Invoke(sb, null);
        }
    }
}