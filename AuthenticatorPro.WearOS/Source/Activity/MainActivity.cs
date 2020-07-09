using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Gms.Common.Apis;
using Android.Gms.Wearable;
using Android.OS;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Wear.Widget;
using AuthenticatorPro.Shared.Query;
using AuthenticatorPro.Shared.Util;
using AuthenticatorPro.WearOS.Cache;
using AuthenticatorPro.WearOS.List;
using Google.Android.Material.Button;
using Newtonsoft.Json;


namespace AuthenticatorPro.WearOS.Activity
{
    [Activity(Label = "@string/displayName", MainLauncher = true, Icon = "@mipmap/ic_launcher", Theme = "@style/AppTheme")]
    internal class MainActivity : AppCompatActivity, MessageClient.IOnMessageReceivedListener
    {
        private const string QueryCapability = "query";
        private const string ListCapability = "list";
        private const string RefreshCapability = "refresh";
        private const string ListCustomIconsCapability = "list_custom_icons";
        private const string GetCustomIconCapability = "get_custom_icon";
        
        private INode _serverNode;
        private int _responsesReceived;
        private int _responsesRequired;

        private LinearLayout _loadingLayout;
        private LinearLayout _emptyLayout;
        private LinearLayout _disconnectedLayout;

        private MaterialButton _retryButton;
        private WearableRecyclerView _authList;
        
        private CustomIconCache _customIconCache;
        private AuthenticatorListAdapter _authenticatorListAdapter;


        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.activityMain);

            _loadingLayout = FindViewById<LinearLayout>(Resource.Id.layoutLoading);
            _emptyLayout = FindViewById<LinearLayout>(Resource.Id.layoutEmpty);
            _disconnectedLayout = FindViewById<LinearLayout>(Resource.Id.layoutDisconnected);

            _retryButton = FindViewById<MaterialButton>(Resource.Id.buttonRetry);
            _retryButton.Click += OnRetryClick; 

            _authList = FindViewById<WearableRecyclerView>(Resource.Id.list);
            _authList.EdgeItemsCenteringEnabled = true;
            _authList.HasFixedSize = true;
            _authList.SetItemViewCacheSize(12);
            _authList.SetItemAnimator(null);

            var layoutCallback = new ScrollingListLayoutCallback(Resources.Configuration.IsScreenRound);
            _authList.SetLayoutManager(new WearableLinearLayoutManager(this, layoutCallback));

            _customIconCache = new CustomIconCache(this);
            
            _authenticatorListAdapter = new AuthenticatorListAdapter(_customIconCache);
            _authenticatorListAdapter.ItemClick += ItemClick;
            _authenticatorListAdapter.HasStableIds = true;
            _authList.SetAdapter(_authenticatorListAdapter);
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
            await FindServerNode();
            await Refresh();
        }

        private async Task Refresh()
        {
            Interlocked.Exchange(ref _responsesReceived, 0);
            Interlocked.Exchange(ref _responsesRequired, 0);
            
            if(_serverNode == null)
            {
                AnimUtil.FadeInView(_disconnectedLayout, 200, true);
                _loadingLayout.Visibility = ViewStates.Invisible;
                return;
            }

            AnimUtil.FadeOutView(_disconnectedLayout, 200);
            AnimUtil.FadeOutView(_emptyLayout, 200);
            AnimUtil.FadeOutView(_authList, 200);

            Interlocked.Exchange(ref _responsesRequired, 2);
            
            await WearableClass.GetMessageClient(this)
                .SendMessageAsync(_serverNode.Id, ListCapability, new byte[] { });

            await WearableClass.GetMessageClient(this)
                .SendMessageAsync(_serverNode.Id, ListCustomIconsCapability, new byte[] { });
        }
        
        private async void ItemClick(object sender, int position)
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
            bundle.PutInt("period", item.Period);
            bundle.PutInt("digits", item.Digits);

            var hasCustomIcon = item.Icon.StartsWith(CustomIconCache.Prefix);
            bundle.PutBoolean("hasCustomIcon", hasCustomIcon);

            if(hasCustomIcon)
            {
                var id = item.Icon.Substring(1);
                var bitmap = await _customIconCache.GetBitmap(id);
                bundle.PutParcelable("icon", bitmap);    
            }
            else
                bundle.PutString("icon", item.Icon);
            
            intent.PutExtras(bundle);
            StartActivity(intent);
        }

        protected override async void OnResume()
        {
            base.OnResume();
            _loadingLayout.Visibility = ViewStates.Visible;

            try
            {
                await WearableClass.GetMessageClient(this).AddListenerAsync(this);
                await FindServerNode();
                await Refresh();
            }
            catch(ApiException)
            {
                Toast.MakeText(this, Resource.String.connectionError, ToastLength.Long).Show();
                _disconnectedLayout.Visibility = ViewStates.Visible;
            }
        }

        protected override async void OnPause()
        {
            base.OnPause();
            AnimUtil.FadeOutView(_authList, 200);
            await WearableClass.GetMessageClient(this).RemoveListenerAsync(this);
        }

        private void OnReady()
        {
            if(_authenticatorListAdapter.Items.Count == 0)
                AnimUtil.FadeInView(_emptyLayout, 200);
            else
                AnimUtil.FadeOutView(_emptyLayout, 200);

            var anim = new AlphaAnimation(0f, 1f) { Duration = 200 };

            anim.AnimationEnd += (sender, e) =>
            {
                _authList.Visibility = ViewStates.Visible;
                _authList.RequestFocus();
            };

            _authList.StartAnimation(anim);
            _loadingLayout.Visibility = ViewStates.Invisible;
        }

        private void OnAuthenticatorListReceived(byte[] data)
        {
            var json = Encoding.UTF8.GetString(data);
            _authenticatorListAdapter.Items = JsonConvert.DeserializeObject<List<WearAuthenticatorResponse>>(json);
            _authenticatorListAdapter.NotifyDataSetChanged();
        }

        private async Task OnCustomIconListReceived(byte[] data)
        {
            var json = Encoding.UTF8.GetString(data);
            var iconIds = JsonConvert.DeserializeObject<List<string>>(json);

            var iconsInCache = _customIconCache.GetIcons();

            var iconsToRequest = iconIds.Where(i => !iconsInCache.Contains(i)).ToList();
            var iconsToRemove = iconsInCache.Where(i => !iconIds.Contains(i)).ToList();

            var client = WearableClass.GetMessageClient(this);

            Interlocked.Add(ref _responsesRequired, iconsToRequest.Count);
            
            foreach(var icon in iconsToRequest)
                await client.SendMessageAsync(_serverNode.Id, GetCustomIconCapability, Encoding.UTF8.GetBytes(icon));

            foreach(var icon in iconsToRemove)
                _customIconCache.Remove(icon);
        }

        private async Task OnCustomIconReceived(byte[] data)
        {
            var json = Encoding.UTF8.GetString(data);
            var icon = JsonConvert.DeserializeObject<WearCustomIconResponse>(json);
            
            await _customIconCache.Add(icon.Id, icon.Data);
        }

        public async void OnMessageReceived(IMessageEvent messageEvent)
        {
            switch(messageEvent.Path)
            {
                case ListCapability:
                    OnAuthenticatorListReceived(messageEvent.GetData());
                    break;
                
                case ListCustomIconsCapability:
                    await OnCustomIconListReceived(messageEvent.GetData());
                    break;
                
                case GetCustomIconCapability:
                    await OnCustomIconReceived(messageEvent.GetData());
                    break;

                case RefreshCapability:
                    await Refresh();
                    break;
            }

            Interlocked.Add(ref _responsesReceived, 1);

            if(_responsesReceived == _responsesRequired)
                OnReady();
        }
    }
}


