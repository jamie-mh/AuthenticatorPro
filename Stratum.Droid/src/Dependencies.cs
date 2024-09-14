// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;
using Android.Content;
using Stratum.Core;
using Stratum.Core.Backup.Encryption;
using Stratum.Core.Comparer;
using Stratum.Core.Entity;
using Stratum.Core.Persistence;
using Stratum.Core.Service;
using Stratum.Core.Service.Impl;
using Stratum.Droid.Shared;
using Stratum.Droid.Interface;
using Stratum.Droid.Persistence;
using Stratum.Droid.Persistence.View;
using Stratum.Droid.Persistence.View.Impl;
using TinyIoC;

namespace Stratum.Droid
{
    internal static class Dependencies
    {
        private static readonly TinyIoCContainer Container = TinyIoCContainer.Current;

        public static void Register(Database database)
        {
            Container.Register(database);
            Container.RegisterMultiple<IBackupEncryption>(new[]
            {
                typeof(StrongBackupEncryption), typeof(LegacyBackupEncryption), typeof(NoBackupEncryption)
            });

            Container.Register<IAssetProvider, AssetProvider>();
            Container.Register<ICustomIconDecoder, CustomIconDecoder>();
            Container.Register<IIconResolver, IconResolver>();

            RegisterRepositories(Container);
            RegisterServices(Container);
            RegisterViews(Container);
        }

        public static void RegisterApplicationContext(Context context)
        {
            Container.Register(context);
        }

        public static TinyIoCContainer GetChildContainer()
        {
            return Container.GetChildContainer();
        }

        public static void RegisterRepositories(TinyIoCContainer container)
        {
            container.Register<IAuthenticatorRepository, AuthenticatorRepository>();
            container.Register<ICategoryRepository, CategoryRepository>();
            container.Register<IAuthenticatorCategoryRepository, AuthenticatorCategoryRepository>();
            container.Register<ICustomIconRepository, CustomIconRepository>();
            container.Register<IIconPackRepository, IconPackRepository>();
            container.Register<IIconPackEntryRepository, IconPackEntryRepository>();
        }

        public static void RegisterServices(TinyIoCContainer container)
        {
            container.Register<IEqualityComparer<Authenticator>, AuthenticatorComparer>();
            container.Register<IEqualityComparer<Category>, CategoryComparer>();
            container.Register<IEqualityComparer<AuthenticatorCategory>, AuthenticatorCategoryComparer>();

            container.Register<IAuthenticatorService, AuthenticatorService>();
            container.Register<IBackupService, BackupService>();
            container.Register<ICategoryService, CategoryService>();
            container.Register<ICustomIconService, CustomIconService>();
            container.Register<IIconPackService, IconPackService>();
            container.Register<IImportService, ImportService>();
            container.Register<IRestoreService, RestoreService>();
        }

        public static void RegisterViews(TinyIoCContainer container)
        {
            container.Register<IAuthenticatorView, AuthenticatorView>().AsMultiInstance();
            container.Register<ICategoryView, CategoryView>().AsMultiInstance();
            container.Register<ICustomIconView, CustomIconView>().AsMultiInstance();
            container.Register<IDefaultIconView, DefaultIconView>().AsMultiInstance();
            container.Register<IIconPackEntryView, IconPackEntryView>().AsMultiInstance();
            container.Register<IIconPackView, IconPackView>().AsMultiInstance();
        }

        public static T Resolve<T>() where T : class
        {
            return Container.Resolve<T>();
        }

        public static IEnumerable<T> ResolveAll<T>() where T : class
        {
            return Container.ResolveAll<T>();
        }
    }
}