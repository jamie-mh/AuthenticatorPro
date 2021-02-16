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
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Wear.Widget;
using AndroidX.Wear.Widget.Drawer;
using AuthenticatorPro.Droid.Shared.Query;
using AuthenticatorPro.Droid.Shared.Util;
using AuthenticatorPro.WearOS.Cache;
using AuthenticatorPro.WearOS.Data;
using AuthenticatorPro.WearOS.List;
using AuthenticatorPro.WearOS.Util;
using Newtonsoft.Json;


namespace AuthenticatorPro.WearOS.Activity
{
    [Activity(Label = "@string/displayName", MainLauncher = true, Icon = "@mipmap/ic_launcher", Theme = "@style/AppTheme")]
    internal class MainActivity : AppCompatActivity, MessageClient.IOnMessageReceivedListener
    {
        // Query Paths
        private const string ProtocolVersion = "protocol_v2.1";
        private const string ListAuthenticatorsCapability = "list_authenticators";
        private const string ListCategoriesCapability = "list_categories";
        private const string ListCustomIconsCapability = "list_custom_icons";
        private const string GetCustomIconCapability = "get_custom_icon";
        private const string GetPreferencesCapability = "get_preferences";
        private const string RefreshCapability = "refresh";
        
        // Cache Names
        private const string AuthenticatorCacheName = "authenticators";
        private const string CategoryCacheName = "categories";

        // Views
        private LinearLayout _offlineLayout;
        private RelativeLayout _loadingLayout;
        private RelativeLayout _emptyLayout;
        private WearableRecyclerView _authList;
        private WearableNavigationDrawerView _categoryList;
        
        // Data
        private AuthenticatorSource _authSource;
        private ListCache<WearAuthenticator> _authCache;
        private ListCache<WearCategory> _categoryCache;
        private CustomIconCache _customIconCache;
        private PreferenceWrapper _preferences;
        
        private AuthenticatorListAdapter _authListAdapter;
        private CategoryListAdapter _categoryListAdapter;
        
        // Connection Status
        private INode _serverNode;
        private int _responsesReceived;
        private int _responsesRequired;

        // Lifecycle Synchronisation
        private readonly SemaphoreSlim _onCreateLock;
        private readonly SemaphoreSlim _onResumeLock;
        private readonly SemaphoreSlim _refreshLock;


        public MainActivity()
        {
            _onCreateLock = new SemaphoreSlim(1, 1);
            _onResumeLock = new SemaphoreSlim(1, 1);
            _refreshLock = new SemaphoreSlim(1, 1);
        }

        #region Activity Lifecycle
        protected override async void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            
            await _onCreateLock.WaitAsync();
            SetContentView(Resource.Layout.activityMain);

            _preferences = new PreferenceWrapper(this);

            _authCache = new ListCache<WearAuthenticator>(AuthenticatorCacheName, this);
            _categoryCache = new ListCache<WearCategory>(CategoryCacheName, this);
            _customIconCache = new CustomIconCache(this);
            
            await _authCache.Init();
            await _categoryCache.Init();
            
            _authSource = new AuthenticatorSource(_authCache);
            RunOnUiThread(InitViews);

            _onCreateLock.Release();
            
            await _onResumeLock.WaitAsync();
            _onResumeLock.Release();

            HandleDefaults();
        }

        private void HandleDefaults()
        {
            var defaultAuth = _preferences.DefaultAuth;

            if(defaultAuth != null)
            {
                var authPosition = _authSource.FindIndex(a => a.Secret.GetHashCode() == defaultAuth);
                OnItemClick(null, authPosition);
            }
            
            var defaultCategory = _preferences.DefaultCategory;

            if(defaultCategory == null)
            {
                RunOnUiThread(CheckEmptyState);
                return;
            }

            _authSource.SetCategory(defaultCategory);
            var categoryPosition = _categoryCache.FindIndex(c => c.Id == defaultCategory) + 1;

            if(categoryPosition < 0)
                return;
            
            RunOnUiThread(delegate
            {
                _categoryList.SetCurrentItem(categoryPosition, false);
                _authListAdapter.NotifyDataSetChanged();
                CheckEmptyState();
            });
        }

        protected override async void OnResume()
        {
            base.OnResume();
            await _onResumeLock.WaitAsync();
            
            await _onCreateLock.WaitAsync();
            _onCreateLock.Release();

            try
            {
                await WearableClass.GetMessageClient(this).AddListenerAsync(this);
                await FindServerNode();
            }
            catch(ApiException)
            {
                RunOnUiThread(CheckOfflineState);
                return;
            }

            RunOnUiThread(CheckOfflineState);
            
            await Refresh();
            _onResumeLock.Release();
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
            _authListAdapter.ItemLongClick += OnItemLongClick;
            _authListAdapter.HasStableIds = true;
            _authListAdapter.DefaultAuth = _preferences.DefaultAuth;
            _authList.SetAdapter(_authListAdapter);
            
            _categoryList = FindViewById<WearableNavigationDrawerView>(Resource.Id.drawerCategories);
            _categoryListAdapter = new CategoryListAdapter(this, _categoryCache);
            _categoryList.SetAdapter(_categoryListAdapter);
            _categoryList.ItemSelected += OnCategorySelected;
        }

        private void OnCategorySelected(object sender, WearableNavigationDrawerView.ItemSelectedEventArgs e)
        {
            if(e.Pos > 0)
            {
                var category = _categoryCache[e.Pos - 1];

                if(category == null)
                    return;

                _authSource.SetCategory(category.Id);
            }
            else
                _authSource.SetCategory(null);

            _authListAdapter.NotifyDataSetChanged();
            CheckEmptyState();
        }

        private void CheckOfflineState()
        {
            if(_serverNode == null)
            {
                AnimUtil.FadeOutView(_loadingLayout, AnimUtil.LengthShort);
                _offlineLayout.Visibility = ViewStates.Visible;
            }
            else
                _offlineLayout.Visibility = ViewStates.Invisible;
        }

        private void CheckEmptyState()
        {
            if(_loadingLayout.Visibility == ViewStates.Visible)
                AnimUtil.FadeOutView(_loadingLayout, AnimUtil.LengthShort);
            
            _emptyLayout.Visibility = ViewStates.Gone;

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

        private void OnItemLongClick(object sender, int position)
        {
            var item = _authSource.Get(position);

            if(item == null)
                return;

            var oldDefault = _preferences.DefaultAuth;
            var newDefault = item.Secret.GetHashCode();

            if(oldDefault == newDefault)
                _authListAdapter.DefaultAuth = _preferences.DefaultAuth = null;
            else
            {
                _authListAdapter.DefaultAuth = _preferences.DefaultAuth = newDefault;
                _authListAdapter.NotifyItemChanged(position);
            }

            if(oldDefault != null)
            {
                var oldPosition = _authSource.FindIndex(a => a.Secret.GetHashCode() == oldDefault);
                
                if(oldPosition > -1)
                    _authListAdapter.NotifyItemChanged(oldPosition);
            }
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

            await _refreshLock.WaitAsync();
            
            Interlocked.Exchange(ref _responsesReceived, 0);
            Interlocked.Exchange(ref _responsesRequired, 4);
            
            var client = WearableClass.GetMessageClient(this);

            async Task MakeRequest(string capability)
            {
                await client.SendMessageAsync(_serverNode.Id, capability, new byte[] { });
            }

            await MakeRequest(ListAuthenticatorsCapability);
            await MakeRequest(ListCategoriesCapability);
            await MakeRequest(ListCustomIconsCapability);
            await MakeRequest(GetPreferencesCapability);
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
        
        private void OnPreferencesReceived(byte[] data)
        {
            var json = Encoding.UTF8.GetString(data);
            var prefs = JsonConvert.DeserializeObject<WearPreferences>(json);

            _preferences.DefaultCategory = prefs.DefaultCategory;
        }

        private async Task OnRefreshRecieved()
        {
            await Refresh();
            RunOnUiThread(CheckEmptyState);
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
                
                case GetPreferencesCapability:
                    OnPreferencesReceived(messageEvent.GetData());
                    break;

                case RefreshCapability:
                    await OnRefreshRecieved();
                    break;
            }

            Interlocked.Increment(ref _responsesReceived);
            
            var received = Interlocked.CompareExchange(ref _responsesReceived, 0, 0);
            var required = Interlocked.CompareExchange(ref _responsesRequired, 0, 0);

            if(received == required)
                _refreshLock.Release();
        }
        #endregion
    }
}

