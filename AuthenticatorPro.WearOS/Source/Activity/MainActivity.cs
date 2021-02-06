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
using AndroidX.Wear.Widget.Drawer;
using AuthenticatorPro.Droid.Shared.Query;
using AuthenticatorPro.Droid.Shared.Util;
using AuthenticatorPro.WearOS.Cache;
using AuthenticatorPro.WearOS.Data;
using AuthenticatorPro.WearOS.List;
using Newtonsoft.Json;


namespace AuthenticatorPro.WearOS.Activity
{
    [Activity(Label = "@string/displayName", MainLauncher = true, Icon = "@mipmap/ic_launcher", Theme = "@style/AppTheme")]
    internal class MainActivity : AppCompatActivity, MessageClient.IOnMessageReceivedListener
    {
        // Query Paths
        private const string ProtocolVersion = "protocol_v2";
        private const string ListAuthenticatorsCapability = "list_authenticators";
        private const string ListCategoriesCapability = "list_categories";
        private const string ListCustomIconsCapability = "list_custom_icons";
        private const string GetCustomIconCapability = "get_custom_icon";
        private const string RefreshCapability = "refresh";
        
        // Cache Names
        private const string AuthenticatorCacheName = "authenticators";
        private const string CategoryCacheName = "categories";

        // Views
        private LinearLayout _offlineLayout;
        private RelativeLayout _loadingLayout;
        private RelativeLayout _emptyLayout;
        private WearableRecyclerView _authList;
        
        // Data
        private AuthenticatorSource _authSource;
        private ListCache<WearAuthenticator> _authCache;
        private ListCache<WearCategory> _categoryCache;
        private CustomIconCache _customIconCache;
        
        private AuthenticatorListAdapter _authListAdapter;
        private CategoryListAdapter _categoryListAdapter;
        
        // Connection Status
        private INode _serverNode;
        private int _responsesReceived;
        private int _responsesRequired;

        // Lifecycle Synchronisation
        private readonly SemaphoreSlim _onCreateSemaphore;


        public MainActivity()
        {
            _onCreateSemaphore = new SemaphoreSlim(1, 1);            
        }

        #region Activity Lifecycle
        protected override async void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            
            await _onCreateSemaphore.WaitAsync();
            SetContentView(Resource.Layout.activityMain);

            _authCache = new ListCache<WearAuthenticator>(AuthenticatorCacheName, this);
            _categoryCache = new ListCache<WearCategory>(CategoryCacheName, this);
            _customIconCache = new CustomIconCache(this);
            
            await _authCache.Init();
            await _categoryCache.Init();
            
            _authSource = new AuthenticatorSource(_authCache);
            RunOnUiThread(InitViews);

            _onCreateSemaphore.Release();
        }

        protected override async void OnResume()
        {
            base.OnResume();
            
            await _onCreateSemaphore.WaitAsync();
            _onCreateSemaphore.Release();

            try
            {
                await WearableClass.GetMessageClient(this).AddListenerAsync(this);
                await FindServerNode();
            }
            catch(ApiException)
            {
                RunOnUiThread(UpdateViewState);
                return;
            }

            RunOnUiThread(UpdateViewState);
            await Refresh();
        }

        protected override async void OnPause()
        {
            base.OnPause();

            try
            {
                await WearableClass.GetMessageClient(this).RemoveListenerAsync(this);
            }
            catch(ApiException) { }
        }
        #endregion

        #region Authenticators and Categories 
        private void InitViews()
        {
            _loadingLayout = FindViewById<RelativeLayout>(Resource.Id.layoutLoading);
            _emptyLayout = FindViewById<RelativeLayout>(Resource.Id.layoutEmpty);
            _offlineLayout = FindViewById<LinearLayout>(Resource.Id.layoutOffline);

            _authList = FindViewById<WearableRecyclerView>(Resource.Id.list);
            _authList.EdgeItemsCenteringEnabled = true;
            _authList.HasFixedSize = true;
            _authList.SetItemViewCacheSize(12);
            _authList.SetItemAnimator(null);
            
            var layoutCallback = new AuthenticatorListLayoutCallback(this);
            _authList.SetLayoutManager(new WearableLinearLayoutManager(this, layoutCallback));
            
            _authListAdapter = new AuthenticatorListAdapter(_authSource, _customIconCache);
            _authListAdapter.ItemClick += OnItemClick;
            _authListAdapter.HasStableIds = true;
            _authList.SetAdapter(_authListAdapter);
            
            var categoriesDrawer = FindViewById<WearableNavigationDrawerView>(Resource.Id.drawerCategories);
            _categoryListAdapter = new CategoryListAdapter(this, _categoryCache);
            categoriesDrawer.SetAdapter(_categoryListAdapter);
            categoriesDrawer.ItemSelected += OnCategorySelected;
        }

        private void OnCategorySelected(object sender, WearableNavigationDrawerView.ItemSelectedEventArgs e)
        {
            if(e.Pos > 0)
            {
                var category = _categoryCache.Get(e.Pos - 1);

                if(category == null)
                    return;

                _authSource.SetCategory(category.Id);
            }
            else
                _authSource.SetCategory(null);

            _authListAdapter.NotifyDataSetChanged();
            UpdateViewState();
        }

        private void UpdateViewState()
        {
            if(_loadingLayout.Visibility == ViewStates.Visible)
                AnimUtil.FadeOutView(_loadingLayout, AnimUtil.LengthShort);
            
            _emptyLayout.Visibility = ViewStates.Gone;

            _offlineLayout.Visibility = _serverNode == null
                ? ViewStates.Visible
                : ViewStates.Gone;

            if(_authSource.GetView().Count == 0)
                _emptyLayout.Visibility = ViewStates.Visible;
            else
            {
                if(_authList.Visibility == ViewStates.Invisible)
                {
                    AnimUtil.FadeInView(_authList, AnimUtil.LengthShort, false, delegate
                    {
                        _authList.RequestFocus();
                    });
                }
                else
                    _authList.RequestFocus();
            }
        }
        
        private async void OnItemClick(object sender, int position)
        {
            var item = _authSource.Get(position);

            if(item == null)
                return;

            var intent = new Intent(this, typeof(CodeActivity));
            var bundle = new Bundle();
           
            bundle.PutInt("type", (int) item.Type);
            bundle.PutString("issuer", item.Issuer);
            bundle.PutString("username", item.Username);
            bundle.PutString("issuer", item.Issuer);
            bundle.PutInt("period", item.Period);
            bundle.PutInt("digits", item.Digits);
            bundle.PutString("secret", item.Secret);
            bundle.PutInt("algorithm", (int) item.Algorithm);

            var hasCustomIcon = !String.IsNullOrEmpty(item.Icon) && item.Icon.StartsWith(CustomIconCache.Prefix);
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
        #endregion

        #region Message Handling
        private async Task FindServerNode()
        {
            var capabilityInfo = await WearableClass.GetCapabilityClient(this)
                .GetCapabilityAsync(ProtocolVersion, CapabilityClient.FilterReachable);

            var capableNode = capabilityInfo.Nodes.FirstOrDefault(n => n.IsNearby);

            if(capableNode == null)
            {
                _serverNode = null;
                return;
            }

            // Immediately after disconnecting from the phone, the device may still show up in the list of reachable nodes.
            // But since it's disconnected, any attempt to send a message will fail.
            // So, make sure that the phone *really* is connected before continuing.
            try
            {
                await WearableClass.GetMessageClient(this).SendMessageAsync(capableNode.Id, ProtocolVersion, new byte[] { });
                _serverNode = capableNode;
            }
            catch(ApiException)
            {
                _serverNode = null;
            }
        }

        private async Task Refresh()
        {
            if(_serverNode == null)
                return;
            
            Interlocked.Exchange(ref _responsesReceived, 0);
            Interlocked.Exchange(ref _responsesRequired, 3);
            
            await WearableClass.GetMessageClient(this)
                .SendMessageAsync(_serverNode.Id, ListAuthenticatorsCapability, new byte[] { });
            
            await WearableClass.GetMessageClient(this)
                .SendMessageAsync(_serverNode.Id, ListCategoriesCapability, new byte[] { });

            await WearableClass.GetMessageClient(this)
                .SendMessageAsync(_serverNode.Id, ListCustomIconsCapability, new byte[] { });
        }
        
        private async Task OnAuthenticatorListReceived(byte[] data)
        {
            var json = Encoding.UTF8.GetString(data);
            var items = JsonConvert.DeserializeObject<List<WearAuthenticator>>(json);

            if(_authCache.Dirty(items, new WearAuthenticatorComparer()))
            {
                await _authCache.Replace(items);
                _authSource.UpdateView();
                RunOnUiThread(_authListAdapter.NotifyDataSetChanged);
            }
        }

        private async Task OnCategoriesListReceived(byte[] data)
        {
            var json = Encoding.UTF8.GetString(data);
            var items = JsonConvert.DeserializeObject<List<WearCategory>>(json);

            if(_categoryCache.Dirty(items, new WearCategoryComparer()))
            {
                await _categoryCache.Replace(items);
                RunOnUiThread(_categoryListAdapter.NotifyDataSetChanged);
            }
        }
        
        private async Task OnCustomIconListReceived(byte[] data)
        {
            var json = Encoding.UTF8.GetString(data);
            var ids = JsonConvert.DeserializeObject<List<string>>(json);

            var inCache = _customIconCache.GetIcons();

            var toRequest = ids.Where(i => !inCache.Contains(i)).ToList();
            var toRemove = inCache.Where(i => !ids.Contains(i)).ToList();

            var client = WearableClass.GetMessageClient(this);

            Interlocked.Add(ref _responsesRequired, toRequest.Count);
            
            foreach(var icon in toRequest)
                await client.SendMessageAsync(_serverNode.Id, GetCustomIconCapability, Encoding.UTF8.GetBytes(icon));

            foreach(var icon in toRemove)
                _customIconCache.Remove(icon);
        }

        private async Task OnCustomIconReceived(byte[] data)
        {
            var json = Encoding.UTF8.GetString(data);
            var icon = JsonConvert.DeserializeObject<WearCustomIcon>(json);
            
            await _customIconCache.Add(icon.Id, icon.Data);
        }

        public async void OnMessageReceived(IMessageEvent messageEvent)
        {
            switch(messageEvent.Path)
            {
                case ListAuthenticatorsCapability:
                    await OnAuthenticatorListReceived(messageEvent.GetData());
                    break;
                
                case ListCategoriesCapability:
                    await OnCategoriesListReceived(messageEvent.GetData());
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
            
            var received = Interlocked.CompareExchange(ref _responsesReceived, 0, 0);
            var required = Interlocked.CompareExchange(ref _responsesRequired, 0, 0);

            if(received == required)
                RunOnUiThread(UpdateViewState);
        }
        #endregion
    }
}

