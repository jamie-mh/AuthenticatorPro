using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Gms.Wearable;
using Android.Util;
using AuthenticatorPro.AuthenticatorList;
using AuthenticatorPro.Shared;
using Newtonsoft.Json;
using SQLite;

namespace AuthenticatorPro
{
    [Service]
    [IntentFilter(
        new[] { MessageApi.ActionMessageReceived },
        DataScheme = "wear",
        DataHost = "*"
    )]
    internal class WearQueryService : WearableListenerService
    {
        private const string WearListAuthenticatorsCapability = "list_authenticators";
        private const string WearGetCodeCapability = "get_code";

        private SQLiteAsyncConnection _connection;
        private AuthSource _source;

        private async Task Init()
        {
            _connection = await Database.Connect(ApplicationContext);
            _source = new AuthSource(_connection);
        }

        public override async void OnDestroy()
        {
            base.OnDestroy();
            await _connection.CloseAsync();
        }

        private async Task ListAuthenticators(string nodeId)
        {
            var response = _source.Authenticators.Select(item => 
                new WearAuthenticatorResponse(item.Type, item.Icon, item.Issuer, item.Username, item.Period)).ToList();

            var json = JsonConvert.SerializeObject(response);
            var data = Encoding.UTF8.GetBytes(json);

            await WearableClass.GetMessageClient(this).SendMessageAsync(nodeId,
                WearListAuthenticatorsCapability, data);
        }

        private async Task GetCode(int position, string nodeId)
        {
            var auth = _source.Get(position);
            var response = new WearAuthenticatorCodeResponse(auth.Code, auth.TimeRenew);

            var json = JsonConvert.SerializeObject(response);
            var data = Encoding.UTF8.GetBytes(json);

            await WearableClass.GetMessageClient(this).SendMessageAsync(nodeId,
                WearGetCodeCapability, data);
        }

        public override async void OnMessageReceived(IMessageEvent messageEvent)
        {
            if(_connection == null || _source == null)
                await Init();

            await _source.UpdateSource();

            switch(messageEvent.Path)
            {
                case WearListAuthenticatorsCapability:
                    await ListAuthenticators(messageEvent.SourceNodeId);
                    break;

                case WearGetCodeCapability:
                {
                    var position = Int32.Parse(Encoding.UTF8.GetString(messageEvent.GetData()));
                    await GetCode(position, messageEvent.SourceNodeId);
                    break;
                }
            }
        }
    }
}