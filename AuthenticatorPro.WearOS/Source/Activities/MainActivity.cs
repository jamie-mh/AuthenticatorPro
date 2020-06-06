using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
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
        private const string QueryCapability = "query";
        private const string ListCapability = "list";
        private const string RefreshCapability = "refresh";

        private INode _serverNode;

        private LinearLayout _emptyLayout;
        private LinearLayout _disconnectedLayout;
        private Button _retryButton;

        private WearableRecyclerView _authList;
        private AuthAdapter _authAdapter;


        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.activityMain);

            _emptyLayout = FindViewById<LinearLayout>(Resource.Id.activityMain_emptyLayout);
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
            var capabilityInfo = await WearableClass.GetCapabilityClient(this)
                .GetCapabilityAsync(QueryCapability, CapabilityClient.FilterReachable);

            _serverNode = 
                capabilityInfo.Nodes.FirstOrDefault(n => n.IsNearby);
        }

        private async void OnRetryClick(object sender, EventArgs e)
        {
            FadeOutView(_disconnectedLayout);
            await FindServerNode();
            await Refresh();
        }

        private async Task Refresh()
        {
            if(_serverNode == null)
            {
                FadeInView(_disconnectedLayout);
                return;
            }

            FadeOutView(_disconnectedLayout);
            FadeOutView(_emptyLayout);
            _authList.Visibility = ViewStates.Invisible;

            await WearableClass.GetMessageClient(this)
                .SendMessageAsync(_serverNode.Id, ListCapability, new byte[] { });
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
        }

        protected override async void OnPause()
        {
            base.OnPause();
            FadeOutView(_authList);
            await WearableClass.GetMessageClient(this).RemoveListenerAsync(this);
        }

        public async void OnMessageReceived(IMessageEvent messageEvent)
        {
            switch(messageEvent.Path)
            {
                case ListCapability:
                {
                    var json = Encoding.UTF8.GetString(messageEvent.GetData());
                    _authAdapter.Items = JsonConvert.DeserializeObject<List<WearAuthenticatorResponse>>(json);

                    if(_authAdapter.Items.Count == 0)
                        FadeInView(_emptyLayout);
                    else
                        FadeOutView(_emptyLayout);

                    _authAdapter.NotifyDataSetChanged();
                    FadeInView(_authList);

                    break;
                }

                case RefreshCapability:
                    await Refresh();
                    break;
            }
        }

        private static void FadeInView(View view)
        {
            if(view.Visibility != ViewStates.Invisible)
                return;

            var anim = new AlphaAnimation(0f, 1f)
            {
                Duration = 200
            };

            anim.AnimationEnd += (sender, e) =>
            {
                view.Visibility = ViewStates.Visible;
            };

            view.StartAnimation(anim);
        }

        private static void FadeOutView(View view)
        {
            if(view.Visibility != ViewStates.Visible)
                return;

            var anim = new AlphaAnimation(1f, 0f)
            {
                Duration = 200
            };

            anim.AnimationEnd += (sender, e) =>
            {
                view.Visibility = ViewStates.Invisible;
            };

            view.StartAnimation(anim);
        }
    }
}


