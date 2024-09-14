// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Google.Android.Material.Snackbar;

namespace Stratum.Droid.Callback
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
            Shown?.Invoke(sb, EventArgs.Empty);
        }
    }
}