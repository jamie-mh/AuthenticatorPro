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
using Android.Widget;
using AuthenticatorPro.Shared.Query;
using AuthenticatorPro.Shared.Util;
using AuthenticatorPro.WearOS.List;
using Newtonsoft.Json;

namespace AuthenticatorPro.WearOS.Activity
{
    [Activity(Label = "@string/displayName", MainLauncher = true, Icon = "@mipmap/ic_launcher", Theme = "@style/AppTheme")]
    internal class MainActivity : WearableActivity, MessageClient.IOnMessageReceivedListener
    {
        private const string QueryCapability = "query";
        private const string ListCapability = "list";
        private const string RefreshCapability = "refresh";

        private INode _serverNode;

        private LinearLayout _emptyLayout;
        private LinearLayout _disconnectedLayout;
        private Button _retryButton;

        private WearableRecyclerView _authList;
        private AuthenticatorListAdapter _authenticatorListAdapter;


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

            var layoutCallback = new ScrollingListLayoutCallback(Resources.Configuration.IsScreenRound);
            _authList.SetLayoutManager(new WearableLinearLayoutManager(this, layoutCallback));

            _authenticatorListAdapter = new AuthenticatorListAdapter();
            _authenticatorListAdapter.ItemClick += ItemClick;
            _authList.SetAdapter(_authenticatorListAdapter);            

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
            AnimUtil.FadeOutView(_disconnectedLayout, 200);

            await FindServerNode();
            await Refresh();
        }

        private async Task Refresh()
        {
            if(_serverNode == null)
            {
                AnimUtil.FadeInView(_disconnectedLayout, 200, true);
                return;
            }

            AnimUtil.FadeOutView(_disconnectedLayout, 200);
            AnimUtil.FadeOutView(_emptyLayout, 200);
            AnimUtil.FadeOutView(_authList, 200);

            await WearableClass.GetMessageClient(this)
                .SendMessageAsync(_serverNode.Id, ListCapability, new byte[] { });
        }
        
        private void ItemClick(object sender, int position)
        {
            var item = _authenticatorListAdapter.Items.ElementAtOrDefault(position);

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
            bundle.PutInt("digits", item.Digits);

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
            AnimUtil.FadeOutView(_authList, 200);
            await WearableClass.GetMessageClient(this).RemoveListenerAsync(this);
        }

        public async void OnMessageReceived(IMessageEvent messageEvent)
        {
            switch(messageEvent.Path)
            {
                case ListCapability:
                {
                    var json = Encoding.UTF8.GetString(messageEvent.GetData());
                    _authenticatorListAdapter.Items = JsonConvert.DeserializeObject<List<WearAuthenticatorResponse>>(json);

                    if(_authenticatorListAdapter.Items.Count == 0)
                        AnimUtil.FadeInView(_emptyLayout, 200);
                    else
                        AnimUtil.FadeOutView(_emptyLayout, 200);

                    _authenticatorListAdapter.NotifyDataSetChanged();
                    AnimUtil.FadeInView(_authList, 200, true);

                    break;
                }

                case RefreshCapability:
                    await Refresh();
                    break;
            }
        }
    }
}


