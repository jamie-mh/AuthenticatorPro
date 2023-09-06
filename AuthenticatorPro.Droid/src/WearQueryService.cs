// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

#if !FDROID

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Gms.Wearable;
using AuthenticatorPro.Core.Service;
using AuthenticatorPro.Droid.Persistence.View;
using AuthenticatorPro.Droid.Shared.Wear;
using Java.IO;
using Newtonsoft.Json;

namespace AuthenticatorPro.Droid
{
    [Service(Exported = true)]
    [IntentFilter(
        new[] { ChannelApi.ActionChannelEvent },
        DataScheme = "wear",
        DataHost = "*"
    )]
    public class WearQueryService : WearableListenerService
    {
        private const string GetSyncBundleCapability = "get_sync_bundle";

        private readonly Database _database;
        private readonly SemaphoreSlim _lock;

        private readonly IAuthenticatorView _authenticatorView;
        private readonly ICategoryService _categoryService;
        private readonly ICustomIconService _customIconService;
        private SecureStorageWrapper _secureStorageWrapper;

        public WearQueryService()
        {
            _database = new Database();
            _lock = new SemaphoreSlim(1, 1);

            using var container = Dependencies.GetChildContainer();
            container.Register(_database);
            Dependencies.RegisterRepositories(container);
            Dependencies.RegisterServices(container);
            Dependencies.RegisterViews(container);

            _authenticatorView = container.Resolve<IAuthenticatorView>();
            _categoryService = container.Resolve<ICategoryService>();
            _customIconService = container.Resolve<ICustomIconService>();
        }

        public override void OnCreate()
        {
            base.OnCreate();
            _secureStorageWrapper = new SecureStorageWrapper(this);
        }

        private async Task OpenDatabaseAsync()
        {
            var password = _secureStorageWrapper.GetDatabasePassword();
            await _database.OpenAsync(password, Database.Origin.Wear);
        }

        private async Task CloseDatabaseAsync()
        {
            await _database.CloseAsync(Database.Origin.Wear);
        }

        private async Task<T> UseDatabaseAsync<T>(Func<Task<T>> action)
        {
            await _lock.WaitAsync();
            T result;

            try
            {
                await OpenDatabaseAsync();
                result = await action();
                await CloseDatabaseAsync();
            }
            finally
            {
                _lock.Release();
            }

            return result;
        }

        private async Task<byte[]> GetSyncBundleAsync()
        {
            await _authenticatorView.LoadFromPersistenceAsync();
            var auths = new List<WearAuthenticator>();

            var authCategories = await _categoryService.GetAllBindingsAsync();

            foreach (var auth in _authenticatorView)
            {
                var bindings = authCategories
                    .Where(c => c.AuthenticatorSecret == auth.Secret)
                    .Select(c => new WearAuthenticatorCategory { CategoryId = c.CategoryId, Ranking = c.Ranking })
                    .ToList();

                var item = new WearAuthenticator
                {
                    Type = auth.Type,
                    Secret = auth.Secret,
                    Pin = auth.Pin,
                    Icon = auth.Icon,
                    Issuer = auth.Issuer,
                    Username = auth.Username,
                    Period = auth.Period,
                    Digits = auth.Digits,
                    Algorithm = auth.Algorithm,
                    Ranking = auth.Ranking,
                    CopyCount = auth.CopyCount,
                    Categories = bindings
                };

                auths.Add(item);
            }

            var categories = (await _categoryService.GetAllCategoriesAsync())
                .Select(c => new WearCategory { Id = c.Id, Name = c.Name, Ranking = c.Ranking })
                .ToList();

            var customIcons = (await _customIconService.GetAllAsync())
                .Select(i => new WearCustomIcon { Id = i.Id, Data = i.Data })
                .ToList();

            var preferenceWrapper = new PreferenceWrapper(this);
            var preferences = new WearPreferences
            {
                DefaultCategory = preferenceWrapper.DefaultCategory,
                SortMode = preferenceWrapper.SortMode,
                CodeGroupSize = preferenceWrapper.CodeGroupSize,
                ShowUsernames = preferenceWrapper.ShowUsernames
            };

            var bundle = new WearSyncBundle
            {
                Authenticators = auths,
                Categories = categories,
                CustomIcons = customIcons,
                Preferences = preferences
            };

            var json = JsonConvert.SerializeObject(bundle);
            return Encoding.UTF8.GetBytes(json);
        }

        private async Task SendSyncBundleAsync(ChannelClient.IChannel channel)
        {
            var client = WearableClass.GetChannelClient(this);
            var bundle = await UseDatabaseAsync(GetSyncBundleAsync);

            OutputStream stream = null;

            try
            {
                stream = await client.GetOutputStreamAsync(channel);
                await stream.WriteAsync(bundle);
            }
            finally
            {
                stream?.Close();
            }
        }

        public override async void OnChannelOpened(ChannelClient.IChannel channel)
        {
            Logger.Debug($"Wear channel opened: {channel.Path}");

            if (channel.Path != GetSyncBundleCapability)
            {
                return;
            }

            try
            {
                await SendSyncBundleAsync(channel);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}

#endif