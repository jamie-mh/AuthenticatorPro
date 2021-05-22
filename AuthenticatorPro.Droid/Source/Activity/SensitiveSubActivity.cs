// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

namespace AuthenticatorPro.Droid.Activity
{
    internal class SensitiveSubActivity : BaseActivity
    {
        protected override void OnResume()
        {
            base.OnResume();
            
            if(BaseApplication.IsLocked)
                Finish();
        }
    }
}