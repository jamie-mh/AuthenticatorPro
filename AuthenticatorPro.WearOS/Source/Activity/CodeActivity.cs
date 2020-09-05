using System;
using System.Timers;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Widget;
using AndroidX.AppCompat.App;
using AuthenticatorPro.Shared.Data;
using OtpNet;

namespace AuthenticatorPro.WearOS.Activity
{
    [Activity]
    internal class CodeActivity : AppCompatActivity
    {
        private const int MaxCodeGroupSize = 4;

        private Totp _totp;
        private Timer _timer;

        private int _period;
        private int _digits;

        private ProgressBar _progressBar;
        private DateTime _timeRenew;
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

            var secret = Base32Encoding.ToBytes(Intent.Extras.GetString("secret"));
            _totp = new Totp(secret, _period, algorithm, _digits);

            _timer = new Timer {Interval = 1000, AutoReset = true, Enabled = true};
            _timer.Elapsed += Tick;

            UpdateCode();
            UpdateProgressBar();
        }

        private void UpdateProgressBar()
        {
            var secondsRemaining = (_timeRenew - DateTime.Now).TotalSeconds;
            _progressBar.Progress = (int) Math.Ceiling(100d * secondsRemaining / _period);
        }

        private void Tick(object sender = null, ElapsedEventArgs e = null)
        {
            if(_timeRenew <= DateTime.Now)
                UpdateCode();
            
            UpdateProgressBar();
        }

        protected override void OnPause()
        {
            base.OnStop();
            _timer.Stop();
            Finish();
        }

        private void UpdateCode()
        {
            var code = _totp.ComputeTotp();
            _timeRenew = DateTime.Now.AddSeconds(_totp.RemainingSeconds());
            
            code ??= "".PadRight(_digits, '-');

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
        }
    }
}