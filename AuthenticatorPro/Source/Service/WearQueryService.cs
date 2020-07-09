using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Gms.Wearable;
using Android.Util;
using AuthenticatorPro.Data;
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
        private const string ListCapability = "list";
        private const string GetCodeCapability = "get_code";
        private const string ListCustomIconsCapability = "list_custom_icons";
        private const string GetCustomIconCapability = "get_custom_icon";

        private readonly Lazy<Task> _initTask;
        
        private SQLiteAsyncConnection _connection;
        private AuthenticatorSource _authenticatorSource;
        private CustomIconSource _customIconSource;
        

        public WearQueryService()
        {
            _initTask = new Lazy<Task>(async () =>
            {
                _connection = await Database.Connect(ApplicationContext);
                _authenticatorSource = new AuthenticatorSource(_connection);
                _customIconSource = new CustomIconSource(_connection);
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
            await _authenticatorSource.Update();
            
            var response = _authenticatorSource.Authenticators.Select(item => 
                new WearAuthenticatorResponse(item.Type, item.Icon, item.Issuer, item.Username, item.Period, item.Digits)).ToList();

            var json = JsonConvert.SerializeObject(response);
            var data = Encoding.UTF8.GetBytes(json);

            await WearableClass.GetMessageClient(this)
                .SendMessageAsync(nodeId, ListCapability, data);
        }

        private async Task GetCode(int position, string nodeId)
        {
            await _authenticatorSource.Update();
            
            var auth = _authenticatorSource.Authenticators.ElementAtOrDefault(position);
            var data = new byte[] {};

            if(auth != null)
            {
                var code = auth.GetCode();
                var response = new WearAuthenticatorCodeResponse(code, auth.TimeRenew);

                var json = JsonConvert.SerializeObject(response);
                data = Encoding.UTF8.GetBytes(json);
            }

            await WearableClass.GetMessageClient(this)
                .SendMessageAsync(nodeId, GetCodeCapability, data);
        }

        private async Task ListCustomIcons(string nodeId)
        {
            await _customIconSource.Update();
            
            var ids = new List<string>();
            _customIconSource.Icons.ForEach(i => ids.Add(i.Id));

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
                var response = new WearCustomIconResponse(icon.Id, icon.Data);
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
                case ListCapability:
                    await ListAuthenticators(messageEvent.SourceNodeId);
                    break;

                case GetCodeCapability:
                {
                    var position = Int32.Parse(Encoding.UTF8.GetString(messageEvent.GetData()));
                    await GetCode(position, messageEvent.SourceNodeId);
                    break;
                }
                
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