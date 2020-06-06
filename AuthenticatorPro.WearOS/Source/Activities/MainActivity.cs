using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Gms.Common.Apis;
using Android.Gms.Wearable;
using Android.OS;
using Android.Support.Wear.Widget;
using Android.Support.Wearable.Activity;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using AuthenticatorPro.Shared;
using AuthenticatorPro.WearOS.AuthenticatorList;
using Newtonsoft.Json;

namespace AuthenticatorPro.WearOS.Activities
{
    [Activity(Label = "@string/displayName", MainLauncher = true, Icon = "@mipmap/ic_launcher", Theme = "@style/AppTheme")]
    public class MainActivity : WearableActivity, MessageClient.IOnMessageReceivedListener
    {
        private const string WearListAuthenticatorsCapability = "list_authenticators";

        private bool _justLaunched = true;
        private INode _serverNode;

        private LinearLayout _disconnectedLayout;
        private Button _retryButton;

        private WearableRecyclerView _authList;
        private AuthAdapter _authAdapter;


        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.activityMain);

            _disconnectedLayout = FindViewById<LinearLayout>(Resource.Id.activityMain_disconnectedLayout);
            _retryButton = FindViewById<Button>(Resource.Id.activityMain_retryButton);
            _retryButton.Click += OnRetryClick; 

            _authList = FindViewById<WearableRecyclerView>(Resource.Id.activityMain_authList);
            _authList.EdgeItemsCenteringEnabled = true;

            var layoutCallback = new AuthScrollingLayoutCallback(Resources.Configuration.IsScreenRound);
            _authList.SetLayoutManager(new WearableLinearLayoutManager(this, layoutCallback));

            _authAdapter = new AuthAdapter();
            _authAdapter.ItemClick += ItemClick;
            _authList.SetAdapter(_authAdapter);            

            SetAmbientEnabled();
        }

        private async Task FindServerNode()
        {
            _serverNode = 
                (await WearableClass.GetNodeClient(this).GetConnectedNodesAsync())
                .FirstOrDefault(n => n.IsNearby);
        }

        private async void OnRetryClick(object sender, EventArgs e)
        {
            await FindServerNode();
            await Refresh();
        }

        private async Task Refresh()
        {
            if(_serverNode == null)
            {
                _disconnectedLayout.Visibility = ViewStates.Visible;
                return;
            }

            _disconnectedLayout.Visibility = ViewStates.Gone;
            _authList.Visibility = ViewStates.Invisible;

            await WearableClass.GetMessageClient(this)
                .SendMessageAsync(_serverNode.Id, WearListAuthenticatorsCapability, new byte[] { });
        }
        
        private void ItemClick(object sender, int position)
        {
            var item = _authAdapter.Items.ElementAtOrDefault(position);

            if(item == null)
                return;

            var intent = new Intent(this, typeof(CodeActivity));
            var bundle = new Bundle();

            bundle.PutInt("position", position);
            bundle.PutString("nodeId", _serverNode.Id);

            bundle.PutInt("type", (int) item.Type);
            bundle.PutString("username", item.Username);
            bundle.PutString("issuer", item.Issuer);
            bundle.PutString("icon", item.Icon);
            bundle.PutInt("period", item.Period);

            intent.PutExtras(bundle);
            StartActivity(intent);
        }

        protected override async void OnResume()
        {
            base.OnResume();

            await WearableClass.GetMessageClient(this).AddListenerAsync(this);

            await FindServerNode();
            await Refresh();

            if(_justLaunched && _serverNode != null)
            {
                var message = Resources.GetString(Resource.String.connectedTo);
                Toast.MakeText(this, string.Format(message, _serverNode.DisplayName), ToastLength.Short).Show();
            }

            _justLaunched = false;
        }

        protected override async void OnPause()
        {
            base.OnPause();

            var fadeOut = new AlphaAnimation(1.0f, 0.0f)
            {
                Duration = 200
            };
            
            fadeOut.AnimationEnd += (sender, e) =>
            {
                _authList.Visibility = ViewStates.Invisible;
            };

            _authList.StartAnimation(fadeOut);

            await WearableClass.GetMessageClient(this).RemoveListenerAsync(this);
        }

        public void OnMessageReceived(IMessageEvent messageEvent)
        {
            if(messageEvent.Path != WearListAuthenticatorsCapability)
                return;

            var json = Encoding.UTF8.GetString(messageEvent.GetData());
            _authAdapter.Items = JsonConvert.DeserializeObject<List<WearAuthenticatorResponse>>(json);

            _authAdapter.NotifyDataSetChanged();

            var fadeIn = new AlphaAnimation(0.0f, 1.0f)
            {
                Duration = 200
            };
               
            fadeIn.AnimationEnd += (sender, e) =>
            {
                _authList.Visibility = ViewStates.Visible;
            };

            _authList.StartAnimation(fadeIn);
        }
    }
}


