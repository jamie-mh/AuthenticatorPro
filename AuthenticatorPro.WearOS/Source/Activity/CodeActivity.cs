using System;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Android.App;
using Android.Gms.Wearable;
using Android.OS;
using Android.Support.Wearable.Activity;
using Android.Views;
using Android.Widget;
using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Shared.Query;
using Newtonsoft.Json;

namespace AuthenticatorPro.WearOS.Activity
{
    [Activity]
    internal class CodeActivity : WearableActivity, MessageClient.IOnMessageReceivedListener
    {
        private const int MaxCodeGroupSize = 4;
        private const string WearGetCodeCapability = "get_code";

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

            _progressBar = FindViewById<ProgressBar>(Resource.Id.activityCode_progress);
            _codeTextView = FindViewById<TextView>(Resource.Id.activityCode_code);

            var usernameText = FindViewById<TextView>(Resource.Id.activityCode_username);
            usernameText.Text = Intent.Extras.GetString("username");

            var iconView = FindViewById<ImageView>(Resource.Id.activityCode_icon);
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

            if(_type == AuthenticatorType.Totp)
            {
                _timer.Enabled = true;
                _timer.Elapsed += Tick;
            }
            else if(_type == AuthenticatorType.Hotp)
                _progressBar.Visibility = ViewStates.Invisible;

            await InitWearCapabilities();
        }

        private async Task Refresh()
        {
            // Send the position as a string instead of an int, because dealing with endianess sucks.
            var data = Encoding.UTF8.GetBytes(_position.ToString());

            await WearableClass.GetMessageClient(this)
                .SendMessageAsync(_nodeId, WearGetCodeCapability, data);

            if(_type == AuthenticatorType.Totp)
                _timer.Start();
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

        protected override void OnResume()
        {
            base.OnResume();
            Tick();
        }

        protected override void OnPause()
        {
            base.OnPause();
            _timer.Stop();
        }

        protected override async void OnStop()
        {
            base.OnStop();
            await PauseWearCapabilities();
        }

        public void OnMessageReceived(IMessageEvent messageEvent)
        {
            if(messageEvent.Path != WearGetCodeCapability)
                return;

            // Invalid position, return to list
            if(messageEvent.GetData().Length == 0)
            {
                Finish();
                return;
            }

            var json = Encoding.UTF8.GetString(messageEvent.GetData());
            var update = JsonConvert.DeserializeObject<WearAuthenticatorCodeResponse>(json);

            _timeRenew = update.TimeRenew;

            var code = update.Code;

            if(code == null)
                code = "".PadRight(_digits, '-');

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
            UpdateProgressBar();
        }

        private async Task InitWearCapabilities()
        {
            await WearableClass.GetMessageClient(this).AddListenerAsync(this);
        }

        private async Task PauseWearCapabilities()
        {
            await WearableClass.GetMessageClient(this).RemoveListenerAsync(this);
        }
    }
}