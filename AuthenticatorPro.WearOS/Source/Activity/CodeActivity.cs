using System;
using System.Timers;
using Android.Animation;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Views.Animations;
using Android.Widget;
using AndroidX.AppCompat.App;
using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Shared.Data.Generator;
using OtpNet;
using Totp = AuthenticatorPro.Shared.Data.Generator.Totp;

namespace AuthenticatorPro.WearOS.Activity
{
    [Activity]
    internal class CodeActivity : AppCompatActivity
    {
        private const int MaxCodeGroupSize = 4;

        private IGenerator _generator;
        private Timer _timer;

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

            var usernameText = FindViewById<TextView>(Resource.Id.textUsername);
            usernameText.Text = Intent.Extras.GetString("username");

            var iconView = FindViewById<ImageView>(Resource.Id.imageIcon);
            var hasCustomIcon = Intent.Extras.GetBoolean("hasCustomIcon");

            if(hasCustomIcon)
            {
                var bitmap = (Bitmap) Intent.Extras.GetParcelable("icon");
                
                if(bitmap != null)
                    iconView.SetImageBitmap(bitmap);
                else
                    iconView.SetImageResource(Icon.GetService(Icon.Default, true));
            }
            else
                iconView.SetImageResource(Icon.GetService(Intent.Extras.GetString("icon"), true));

            _period = Intent.Extras.GetInt("period");
            _digits = Intent.Extras.GetInt("digits");
            var algorithm = (OtpHashMode) Intent.Extras.GetInt("algorithm");

            var secret = Intent.Extras.GetString("secret");
            var type = (AuthenticatorType) Intent.Extras.GetInt("type");

            _generator = type switch
            {
                AuthenticatorType.MobileOtp => new MobileOtp(secret, _digits, _period),
                _ => new Totp(secret, _period, algorithm, _digits),
            };

            _timer = new Timer {Interval = 1000, AutoReset = true};
            _timer.Elapsed += Tick;

            UpdateCode();
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
            if(--_secondsRemaining > 0)
                return;
            
            RunOnUiThread(UpdateCode);
        }

        private void UpdateCode()
        {
            var code = _generator.Compute();
            
            var spacesInserted = 0;
            var groupSize = Math.Min(MaxCodeGroupSize, _digits / 2);

            for(var i = 0; i < _digits; ++i)
            {
                if(i % groupSize == 0 && i > 0)
                {
                    code = code.Insert(i + spacesInserted, " ");
                    spacesInserted++;
                }
            }

            _codeTextView.Text = code;
            
            _secondsRemaining = _period - (int) DateTimeOffset.Now.ToUnixTimeSeconds() % _period;
            var progress = (int) Math.Floor((double) _progressBar.Max * _secondsRemaining / _period);
            _progressBar.Progress = progress;
            
            var animator = ObjectAnimator.OfInt(_progressBar, "progress", 0);
            animator.SetDuration(_secondsRemaining * 1000);
            animator.SetInterpolator(new LinearInterpolator());
            animator.Start();
        }
    }
}