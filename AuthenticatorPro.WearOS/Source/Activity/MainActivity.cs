using System;
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
using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Shared.Data.Generator;
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
        private const string ProtocolVersion = "protocol_v3.0";
        private const string GetSyncBundleCapability = "get_sync_bundle";
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
        private WearableNavigationDrawerView _categoryList;
        
        // Data
        private AuthenticatorSource _authSource;
        private ListCache<WearAuthenticator> _authCache;
        private ListCache<WearCategory> _categoryCache;
        private CustomIconCache _customIconCache;
        private PreferenceWrapper _preferences;
        private bool _justLaunched;
        private bool _preventCategorySelectEvent;
        
        private AuthenticatorListAdapter _authListAdapter;
        private CategoryListAdapter _categoryListAdapter;
        
        // Connection Status
        private INode _serverNode;
        private int _responsesReceived;
        private int _responsesRequired;

        // Lifecycle Synchronisation
        private readonly SemaphoreSlim _onCreateLock;
        private readonly SemaphoreSlim _responseLock;


        public MainActivity()
        {
            _justLaunched = true;
            _onCreateLock = new SemaphoreSlim(1, 1);
            _responseLock = new SemaphoreSlim(0, 1);
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
            var defaultCategory = _preferences.DefaultCategory;

            if(defaultCategory != null)
                _authSource.SetCategory(defaultCategory);
            
            RunOnUiThread(delegate
            {
                InitViews();
                AnimUtil.FadeOutView(_loadingLayout, AnimUtil.LengthShort, false, delegate
                {
                    CheckEmptyState();
                    _onCreateLock.Release();
                });
            });
        }

        protected override async void OnResume()
        {
            base.OnResume();
            
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

            if(_justLaunched)
            {
                _justLaunched = false;
                var defaultAuth = _preferences.DefaultAuth;

                if(defaultAuth != null)
                {
                    var authPosition = _authSource.FindIndex(a => a.Secret.GetHashCode() == defaultAuth);
                    OnItemClick(null, authPosition);
                }
            }

            await Refresh();
            
            RunOnUiThread(delegate
            {
                CheckOfflineState();
                CheckEmptyState();
            });
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

            if(_authSource.CategoryId == null)
                return;
            
            var categoryPosition = _categoryCache.FindIndex(c => c.Id == _authSource.CategoryId) + 1;

            if(categoryPosition <= -1)
                return;
            
            _preventCategorySelectEvent = true; 
            _categoryList.SetCurrentItem(categoryPosition, false);
        }

        private void OnCategorySelected(object sender, WearableNavigationDrawerView.ItemSelectedEventArgs e)
        {
            if(_preventCategorySelectEvent)
            {
                _preventCategorySelectEvent = false;
                return;
            }
            
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
            if(!_authSource.GetView().Any())
            {
                _emptyLayout.Visibility = ViewStates.Visible;
                _authList.Visibility = ViewStates.Invisible;
            }
            else
            {
                _emptyLayout.Visibility = ViewStates.Invisible;
                _authList.Visibility = ViewStates.Visible;
                _authList.RequestFocus();
            }
        }
        
        private async void OnItemClick(object sender, int position)
        {
            var item = _authSource.Get(position);

            if(item == null)
                return;

            if(item.Type.GetGenerationMethod() == GenerationMethod.Counter)
            {
                Toast.MakeText(this, Resource.String.hotpNotSupported, ToastLength.Short).Show();
                return;
            }

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
                var id = item.Icon[1..];
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

            Interlocked.Exchange(ref _responsesReceived, 0);
            Interlocked.Exchange(ref _responsesRequired, 1);
            
            var client = WearableClass.GetMessageClient(this);
            await client.SendMessageAsync(_serverNode.Id, GetSyncBundleCapability, new byte[] { });
            
            await _responseLock.WaitAsync();
        }
        
        private async Task OnSyncBundleReceived(byte[] data)
        {
            var json = Encoding.UTF8.GetString(data);
            var bundle = JsonConvert.DeserializeObject<WearSyncBundle>(json);

            _preferences.DefaultCategory = bundle.Preferences.DefaultCategory;
            
            if(_authCache.Dirty(bundle.Authenticators, new WearAuthenticatorComparer()))
            {
                await _authCache.Replace(bundle.Authenticators);
                _authSource.UpdateView();
                RunOnUiThread(_authListAdapter.NotifyDataSetChanged);
            }
            
            if(_categoryCache.Dirty(bundle.Categories, new WearCategoryComparer()))
            {
                await _categoryCache.Replace(bundle.Categories);
                RunOnUiThread(_categoryListAdapter.NotifyDataSetChanged);
            }
           
            var inCache = _customIconCache.GetIcons();

            var toRequest = bundle.CustomIconIds.Where(i => !inCache.Contains(i)).ToList();
            var toRemove = inCache.Where(i => !bundle.CustomIconIds.Contains(i)).ToList();

            foreach(var icon in toRemove)
                _customIconCache.Remove(icon);
            
            if(!toRequest.Any())
                return;
            
            var client = WearableClass.GetMessageClient(this);
            Interlocked.Add(ref _responsesRequired, toRequest.Count);
            
            foreach(var icon in toRequest)
                await client.SendMessageAsync(_serverNode.Id, GetCustomIconCapability, Encoding.UTF8.GetBytes(icon));
        }

        private async Task OnCustomIconReceived(byte[] data)
        {
            var json = Encoding.UTF8.GetString(data);
            var icon = JsonConvert.DeserializeObject<WearCustomIcon>(json);
            
            await _customIconCache.Add(icon.Id, icon.Data);
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
                case GetSyncBundleCapability:
                    await OnSyncBundleReceived(messageEvent.GetData());
                    break;
                
                case GetCustomIconCapability:
                    await OnCustomIconReceived(messageEvent.GetData());
                    break;

                case RefreshCapability:
                    await OnRefreshRecieved();
                    break;
            }

            Interlocked.Increment(ref _responsesReceived);
            
            var received = Interlocked.CompareExchange(ref _responsesReceived, 0, 0);
            var required = Interlocked.CompareExchange(ref _responsesRequired, 0, 0);

            if(received == required)
                _responseLock.Release();
        }
        #endregion
    }
}

