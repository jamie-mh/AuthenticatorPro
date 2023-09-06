// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Timers;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AuthenticatorPro.Core;
using AuthenticatorPro.Core.Generator;
using AuthenticatorPro.Core.Util;
using AuthenticatorPro.Droid.Shared;
using AuthenticatorPro.WearOS.Interface;
using AuthenticatorPro.WearOS.Util;

namespace AuthenticatorPro.WearOS.Activity
{
    [Activity(Theme = "@style/AppTheme")]
    public class CodeActivity : AppCompatActivity
    {
        private IGenerator _generator;

        private int _period;
        private int _digits;
        private int _codeGroupSize;

        private AuthProgressLayout _authProgressLayout;
        private TextView _codeTextView;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.activityCode);

            var preferences = new PreferenceWrapper(this);
            _codeGroupSize = preferences.CodeGroupSize;

            _authProgressLayout = FindViewById<AuthProgressLayout>(Resource.Id.layoutAuthProgress);

            _codeTextView = FindViewById<TextView>(Resource.Id.textCode);

            var issuerText = FindViewById<TextView>(Resource.Id.textIssuer);
            var usernameText = FindViewById<TextView>(Resource.Id.textUsername);

            var username = Intent.Extras.GetString("username");
            var issuer = Intent.Extras.GetString("issuer");

            issuerText.Text = issuer;

            if (string.IsNullOrEmpty(username))
            {
                usernameText.Visibility = ViewStates.Gone;
            }
            else
            {
                usernameText.Text = username;
            }

            var iconView = FindViewById<ImageView>(Resource.Id.imageIcon);
            var hasCustomIcon = Intent.Extras.GetBoolean("hasCustomIcon");

            if (hasCustomIcon)
            {
#pragma warning disable CA1422
                // TODO: Use SDK 33 method
                var bitmap = (Bitmap) Intent.Extras.GetParcelable("icon");
#pragma warning restore CA1422

                if (bitmap != null)
                {
                    iconView.SetImageBitmap(bitmap);
                }
                else
                {
                    iconView.SetImageResource(IconResolver.GetService(IconResolver.Default, true));
                }
            }
            else
            {
                iconView.SetImageResource(IconResolver.GetService(Intent.Extras.GetString("icon"), true));
            }

            _period = Intent.Extras.GetInt("period");
            _digits = Intent.Extras.GetInt("digits");

            var algorithm = (HashAlgorithm) Intent.Extras.GetInt("algorithm");

            var secret = Intent.Extras.GetString("secret");
            var pin = Intent.Extras.GetString("pin");

            var type = (AuthenticatorType) Intent.Extras.GetInt("type");

            _generator = AuthenticatorUtil.GetGenerator(type, secret, pin, _period, algorithm, _digits);

            _authProgressLayout.Period = _period * 1000;
            _authProgressLayout.TimerFinished += Refresh;
        }

        protected override void OnResume()
        {
            base.OnResume();
            Refresh();
        }

        protected override void OnPause()
        {
            base.OnPause();
            _authProgressLayout.StopTimer();
        }

        private void Refresh(object sender = null, ElapsedEventArgs args = null)
        {
            var (code, secondsRemaining) = AuthenticatorUtil.GetCodeAndRemainingSeconds(_generator, _period);

            RunOnUiThread(delegate
            {
                _codeTextView.Text = CodeUtil.PadCode(code, _digits, _codeGroupSize);
                _authProgressLayout.StartTimer((_period - secondsRemaining) * 1000);
            });
        }
    }
}