using System;
using System.Threading;
using System.Timers;
using Android.Animation;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Views.Animations;
using Android.Widget;
using AndroidX.AppCompat.App;
using AuthenticatorPro.Droid.Shared.Data;
using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Shared.Data.Generator;
using AuthenticatorPro.Shared.Util;
using SteamOtp = AuthenticatorPro.Shared.Data.Generator.SteamOtp;
using Timer = System.Timers.Timer;
using Totp = AuthenticatorPro.Shared.Data.Generator.Totp;

namespace AuthenticatorPro.WearOS.Activity
{
    [Activity]
    internal class CodeActivity : AppCompatActivity
    {
        private ITimeBasedGenerator _generator;
        private Timer _timer;
        private float _animationScale;

        private int _period;
        private int _digits;

        private ProgressBar _progressBar;
        private int _secondsRemaining;
        private TextView _codeTextView;


        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.activityCode);

            _progressBar = FindViewById<ProgressBar>(Resource.Id.progressBar);
            _codeTextView = FindViewById<TextView>(Resource.Id.textCode);

            _animationScale = Settings.Global.GetFloat(ContentResolver, Settings.Global.AnimatorDurationScale, 1.0f);

            var usernameText = FindViewById<TextView>(Resource.Id.textUsername);
            var username = Intent.Extras.GetString("username");

            if(String.IsNullOrEmpty(username))
            {
                var issuer = Intent.Extras.GetString("issuer");
                usernameText.Text = issuer;
            }
            else
                usernameText.Text = username;

            var iconView = FindViewById<ImageView>(Resource.Id.imageIcon);
            var hasCustomIcon = Intent.Extras.GetBoolean("hasCustomIcon");

            if(hasCustomIcon)
            {
                var bitmap = (Bitmap) Intent.Extras.GetParcelable("icon");
                
                if(bitmap != null)
                    iconView.SetImageBitmap(bitmap);
                else
                    iconView.SetImageResource(IconResolver.GetService(IconResolver.Default, true));
            }
            else
                iconView.SetImageResource(IconResolver.GetService(Intent.Extras.GetString("icon"), true));

            _period = Intent.Extras.GetInt("period");
            _digits = Intent.Extras.GetInt("digits");
            var algorithm = (Algorithm) Intent.Extras.GetInt("algorithm");

            var secret = Intent.Extras.GetString("secret");
            var type = (AuthenticatorType) Intent.Extras.GetInt("type");

            _generator = type switch
            {
                AuthenticatorType.MobileOtp => new MobileOtp(secret, _digits, _period),
                AuthenticatorType.SteamOtp => new SteamOtp(secret),
                _ => new Totp(secret, _period, algorithm, _digits)
            };

            _timer = new Timer {Interval = 1000, AutoReset = true};
            _timer.Elapsed += Tick;

            GenerateCode();
        }

        protected override void OnResume()
        {
            base.OnResume();
            _timer?.Start();
            
            _secondsRemaining = 0;
            Tick();
        }

        protected override void OnPause()
        {
            base.OnPause();
            _timer?.Stop();
        }

        private void Tick(object sender = null, ElapsedEventArgs e = null)
        {
            var secondsRemaining = Math.Max(0, Interlocked.Decrement(ref _secondsRemaining));
            
            if(_animationScale > 0)
            {
                if(secondsRemaining > 0)
                    return;

                var code = GenerateCode();
                secondsRemaining = Interlocked.CompareExchange(ref _secondsRemaining, 0, 0);

                var remainingProgress = GetRemainingProgress(secondsRemaining);
                var duration = (int) (secondsRemaining * 1000 / _animationScale);

                RunOnUiThread(delegate
                {
                    _codeTextView.Text = code;
                    _progressBar.Progress = remainingProgress;
                
                    var animator = ObjectAnimator.OfInt(_progressBar, "progress", 0);
                    animator.SetDuration(duration);
                    animator.SetInterpolator(new LinearInterpolator());
                    animator.Start();
                });
            }
            else
            {
                if(secondsRemaining <= 0)
                {
                    var code = GenerateCode();
                    RunOnUiThread(delegate { _codeTextView.Text = code; });
                }
                
                var remainingProgress = GetRemainingProgress(secondsRemaining);
                RunOnUiThread(delegate { _progressBar.Progress = remainingProgress; });
            }
        }

        private int GetRemainingProgress(int secondsRemaining)
        {
            return (int) Math.Floor((double) _progressBar.Max * secondsRemaining / _period);
        }

        private string GenerateCode()
        {
            var code = CodeUtil.PadCode(_generator.Compute(), _digits);
            Interlocked.Exchange(ref _secondsRemaining, _period - (int) DateTimeOffset.UtcNow.ToUnixTimeSeconds() % _period);
            return code;
        }
    }
}