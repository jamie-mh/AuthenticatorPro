using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Gms.Wearable;
using AuthenticatorPro.Droid.Data;
using AuthenticatorPro.Droid.Data.Source;
using AuthenticatorPro.Droid.Util;
using AuthenticatorPro.Droid.Shared.Query;
using AuthenticatorPro.Shared.Source.Data.Generator;
using Newtonsoft.Json;
using SQLite;

namespace AuthenticatorPro.Droid.Service
{
    [Service]
    [IntentFilter(
        new[] { MessageApi.ActionMessageReceived },
        DataScheme = "wear",
        DataHost = "*"
    )]
    internal class WearQueryService : WearableListenerService
    {
        private const string GetSyncBundleCapability = "get_sync_bundle";
        private const string GetCustomIconCapability = "get_custom_icon";

        private readonly Lazy<Task> _initTask;
        
        private SQLiteAsyncConnection _connection;
        private AuthenticatorSource _authSource;
        private CategorySource _categorySource;
        private CustomIconSource _customIconSource;
        

        public WearQueryService()
        {
            _initTask = new Lazy<Task>(async delegate
            {
                var password = await SecureStorageWrapper.GetDatabasePassword();
                _connection = await Database.GetPrivateConnection(password);
                _customIconSource = new CustomIconSource(_connection);
                _categorySource = new CategorySource(_connection);
                _authSource = new AuthenticatorSource(_connection);
                _authSource.SetGenerationMethod(GenerationMethod.Time);
            });
        }

        public override async void OnDestroy()
        {
            base.OnDestroy();

            if(_connection != null)
                await _connection.CloseAsync();
        }

        private async Task GetSyncBundle(string nodeId)
        {
            await _authSource.Update();
            var auths = new List<WearAuthenticator>();
            
            foreach(var auth in _authSource.GetView())
            {
                var bindings = _authSource.CategoryBindings
                    .Where(c => c.AuthenticatorSecret == auth.Secret)
                    .Select(c => new WearAuthenticatorCategory(c.CategoryId, c.Ranking))
                    .ToList();
                
                var item = new WearAuthenticator(
                    auth.Type, auth.Secret, auth.Icon, auth.Issuer, auth.Username, auth.Period, auth.Digits, auth.Algorithm, auth.Ranking, bindings); 
                
                auths.Add(item);
            }
            
            await _categorySource.Update();
            var categories = _categorySource.GetAll().Select(c => new WearCategory(c.Id, c.Name)).ToList();

            await _customIconSource.Update();
            var customIconIds = _customIconSource.GetAll().Select(i => i.Id).ToList();
            
            var preferenceWrapper = new PreferenceWrapper(this);
            var preferences = new WearPreferences(preferenceWrapper.DefaultCategory);
            
            var bundle = new WearSyncBundle(auths, categories, customIconIds, preferences);
            
            var json = JsonConvert.SerializeObject(bundle);
            var data = Encoding.UTF8.GetBytes(json);

            await WearableClass.GetMessageClient(this).SendMessageAsync(nodeId, GetSyncBundleCapability, data);
        }

        private async Task GetCustomIcon(string customIconId, string nodeId)
        {
            await _customIconSource.Update();
            var icon = _customIconSource.Get(customIconId);
            
            var data = new byte[] { };

            if(icon != null)
            {
                var response = new WearCustomIcon(icon.Id, icon.Data);
                var json = JsonConvert.SerializeObject(response);
                data = Encoding.UTF8.GetBytes(json);
            }

            await WearableClass.GetMessageClient(this).SendMessageAsync(nodeId, GetCustomIconCapability, data);
        }

        public override async void OnMessageReceived(IMessageEvent messageEvent)
        {
            await _initTask.Value;

            switch(messageEvent.Path)
            {
                case GetSyncBundleCapability:
                    await GetSyncBundle(messageEvent.SourceNodeId);
                    break;
                
                case GetCustomIconCapability:
                {
                    var id = Encoding.UTF8.GetString(messageEvent.GetData());
                    await GetCustomIcon(id, messageEvent.SourceNodeId);
                    break;
                }
            }
        }
    }
}