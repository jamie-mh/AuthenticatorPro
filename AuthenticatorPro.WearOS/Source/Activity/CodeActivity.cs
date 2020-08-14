using System;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Android.App;
using Android.Gms.Common.Apis;
using Android.Gms.Wearable;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Shared.Query;
using Newtonsoft.Json;

namespace AuthenticatorPro.WearOS.Activity
{
    [Activity]
    internal class CodeActivity : AppCompatActivity, MessageClient.IOnMessageReceivedListener
    {
        private const int MaxCodeGroupSize = 4;

        private const string WearGetCodeCapability = "get_code";
        private const string RefreshCapability = "refresh";

        private Timer _timer;

        private int _position;
        private string _nodeId;

        private AuthenticatorType _type;
        private int _period;
        private int _digits;

        private ProgressBar _progressBar;
        private DateTime _timeRenew;
        private TextView _codeTextView;


        protected override async void OnCreate(Bundle bundle)
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

            _nodeId = Intent.Extras.GetString("nodeId");
            _position = Intent.Extras.GetInt("position");

            _period = Intent.Extras.GetInt("period");
            _digits = Intent.Extras.GetInt("digits");
            _type = (AuthenticatorType) Intent.Extras.GetInt("type");

            _timer = new Timer {
                Interval = 1000,
                AutoReset = true
            };

            switch(_type)
            {
                case AuthenticatorType.Totp:
                    _timer.Enabled = true;
                    _timer.Elapsed += Tick;
                    break;

                case AuthenticatorType.Hotp:
                    _progressBar.Visibility = ViewStates.Invisible;
                    break;
            }

            var placeholderCode = FormatCode(null, _digits);
            placeholderCode = placeholderCode.Replace("-", "- ").TrimEnd();
            _codeTextView.Text = placeholderCode;
            
            await WearableClass.GetMessageClient(this).AddListenerAsync(this);
            await Refresh();
        }

        private async Task Refresh()
        {
            // Send the position as a string instead of an int, because dealing with endianess sucks.
            var data = Encoding.UTF8.GetBytes(_position.ToString());

            try
            {
                await WearableClass.GetMessageClient(this)
                    .SendMessageAsync(_nodeId, WearGetCodeCapability, data);
            }
            // If the connection has dropped, just go back
            catch(ApiException)
            {
                Finish();
            }
        }

        private void UpdateProgressBar()
        {
            var secondsRemaining = (_timeRenew - DateTime.Now).TotalSeconds;
            _progressBar.Progress = (int) Math.Ceiling(100d * secondsRemaining / _period);
        }

        private async void Tick(object sender = null, ElapsedEventArgs e = null)
        {
            UpdateProgressBar();

            if(_timeRenew <= DateTime.Now)
                await Refresh();
        }

        protected override async void OnStop()
        {
            base.OnStop();
            await WearableClass.GetMessageClient(this).RemoveListenerAsync(this);
            _timer.Stop();
            Finish();
        }

        private static string FormatCode(string code, int digits)
        {
            code ??= "".PadRight(digits, '-');

            var spacesInserted = 0;
            var groupSize = Math.Min(MaxCodeGroupSize, digits / 2);

            for(var i = 0; i < digits; ++i)
            {
                if(i % groupSize == 0 && i > 0)
                {
                    code = code.Insert(i + spacesInserted, " ");
                    spacesInserted++;
                }
            }

            return code;
        }

        public void OnMessageReceived(IMessageEvent messageEvent)
        {
            switch(messageEvent.Path)
            {
                case WearGetCodeCapability:
                {
                    // Invalid position, return to list
                    if(messageEvent.GetData().Length == 0)
                    {
                        Finish();
                        return;
                    }

                    var json = Encoding.UTF8.GetString(messageEvent.GetData());
                    var update = JsonConvert.DeserializeObject<WearAuthenticatorCodeResponse>(json);

                    _timeRenew = update.TimeRenew;
                    _codeTextView.Text = FormatCode(update.Code, _digits);
                    UpdateProgressBar();
                    break;
                }

                case RefreshCapability:
                    Finish(); // We don't know what changed, just go back
                    break;
            }
        }
    }
}