// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Views;

namespace AuthenticatorPro.Droid.Activity
{
    public abstract class SensitiveSubActivity : BaseActivity
    {
        protected SensitiveSubActivity(int layout) : base(layout)
        {
        }

        protected override async void OnResume()
        {
            base.OnResume();
            var database = Dependencies.Resolve<Database>();

            if (!await database.IsOpenAsync(Database.Origin.Activity))
            {
                Finish();
            }

            var preferences = new PreferenceWrapper(this);
            var windowFlags = !preferences.AllowScreenshots ? WindowManagerFlags.Secure : 0;
            Window.SetFlags(windowFlags, windowFlags);
        }
    }
}