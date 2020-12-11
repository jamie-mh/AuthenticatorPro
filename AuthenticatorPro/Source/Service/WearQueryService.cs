using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Gms.Wearable;
using AuthenticatorPro.Data;
using AuthenticatorPro.Data.Source;
using AuthenticatorPro.Shared.Query;
using Newtonsoft.Json;
using SQLite;

namespace AuthenticatorPro.Service
{
    [Service]
    [IntentFilter(
        new[] { MessageApi.ActionMessageReceived },
        DataScheme = "wear",
        DataHost = "*"
    )]
    internal class WearQueryService : WearableListenerService
    {
        private const string ListAuthenticatorsCapability = "list_authenticators";
        private const string ListCategoriesCapability = "list_categories";
        private const string ListCustomIconsCapability = "list_custom_icons";
        private const string GetCustomIconCapability = "get_custom_icon";

        private readonly Lazy<Task> _initTask;
        
        private SQLiteAsyncConnection _connection;
        private AuthenticatorSource _authSource;
        private CategorySource _categorySource;
        private CustomIconSource _customIconSource;
        

        public WearQueryService()
        {
            _initTask = new Lazy<Task>(async () =>
            {
                _connection = await Database.Connect(ApplicationContext);
                _customIconSource = new CustomIconSource(_connection);
                _categorySource = new CategorySource(_connection);
                _authSource = new AuthenticatorSource(_connection);
                _authSource.SetType(AuthenticatorType.Totp);
            });
        }

        public override async void OnDestroy()
        {
            base.OnDestroy();

            if(_connection != null)
                await _connection.CloseAsync();
        }

        private async Task ListAuthenticators(string nodeId)
        {
            await _authSource.Update();
            var items = new List<WearAuthenticator>();

            foreach(var auth in _authSource.GetView())
            {
                var categoryIds = _authSource.CategoryBindings
                    .Where(c => c.AuthenticatorSecret == auth.Secret)
                    .Select(c => c.CategoryId).ToList();
                
                var item = new WearAuthenticator(
                    auth.Secret, auth.Icon, auth.Issuer, auth.Username, auth.Period, auth.Digits, auth.Algorithm, categoryIds); 
                
                items.Add(item);
            }
            
            var json = JsonConvert.SerializeObject(items);
            var data = Encoding.UTF8.GetBytes(json);

            await WearableClass.GetMessageClient(this)
                .SendMessageAsync(nodeId, ListAuthenticatorsCapability, data);
        }

        private async Task ListCategories(string nodeId)
        {
            await _categorySource.Update();
            
            var categories = 
                _categorySource.GetView().Select(c => new WearCategory(c.Id, c.Name)).ToList();
            
            var json = JsonConvert.SerializeObject(categories);
            var data = Encoding.UTF8.GetBytes(json);

            await WearableClass.GetMessageClient(this)
                .SendMessageAsync(nodeId, ListCategoriesCapability, data);
        }

        private async Task ListCustomIcons(string nodeId)
        {
            await _customIconSource.Update();
            
            var ids = new List<string>();
            _customIconSource.GetView().ForEach(i => ids.Add(i.Id));

            var json = JsonConvert.SerializeObject(ids);
            var data = Encoding.UTF8.GetBytes(json);

            await WearableClass.GetMessageClient(this)
                .SendMessageAsync(nodeId, ListCustomIconsCapability, data);
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

            await WearableClass.GetMessageClient(this)
                .SendMessageAsync(nodeId, GetCustomIconCapability, data);
        }

        public override async void OnMessageReceived(IMessageEvent messageEvent)
        {
            await _initTask.Value;

            switch(messageEvent.Path)
            {
                case ListAuthenticatorsCapability:
                    await ListAuthenticators(messageEvent.SourceNodeId);
                    break;
                
                case ListCategoriesCapability:
                    await ListCategories(messageEvent.SourceNodeId);
                    break;
                
                case ListCustomIconsCapability:
                    await ListCustomIcons(messageEvent.SourceNodeId);
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