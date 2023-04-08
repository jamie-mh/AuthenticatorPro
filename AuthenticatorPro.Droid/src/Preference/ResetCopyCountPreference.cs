// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Widget;
using AuthenticatorPro.Core.Service;
using System;

namespace AuthenticatorPro.Droid.Preference
{
    internal class ResetCopyCountPreference : AndroidX.Preference.Preference
    {
        private readonly Context _context;
        private readonly IAuthenticatorService _authenticatorService;

        public ResetCopyCountPreference(Context context) : base(context)
        {
            _context = context;
            _authenticatorService = Dependencies.Resolve<IAuthenticatorService>();
        }

        public ResetCopyCountPreference(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            _context = context;
            _authenticatorService = Dependencies.Resolve<IAuthenticatorService>();
        }

        public ResetCopyCountPreference(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs,
            defStyleAttr)
        {
            _context = context;
            _authenticatorService = Dependencies.Resolve<IAuthenticatorService>();
        }

        public ResetCopyCountPreference(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(
            context,
            attrs, defStyleAttr, defStyleRes)
        {
            _context = context;
            _authenticatorService = Dependencies.Resolve<IAuthenticatorService>();
        }

        protected ResetCopyCountPreference(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        protected override async void OnClick()
        {
            try
            {
                await _authenticatorService.ResetCopyCountsAsync();
                Toast.MakeText(_context, Resource.String.copyCountReset, ToastLength.Short).Show();
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Toast.MakeText(_context, Resource.String.genericError, ToastLength.Short).Show();
            }
        }
    }
}