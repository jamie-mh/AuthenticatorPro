// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Content;
using AuthenticatorPro.Droid.Persistence;
using AuthenticatorPro.Droid.Shared.Data;
using AuthenticatorPro.Droid.Shared.View;
using AuthenticatorPro.Droid.Shared.View.Impl;
using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Shared.Persistence;
using AuthenticatorPro.Shared.Service;
using AuthenticatorPro.Shared.Service.Impl;
using AuthenticatorPro.Shared.View;
using AuthenticatorPro.Shared.View.Impl;
using TinyIoC;

namespace AuthenticatorPro.Droid
{
    internal static class Dependencies
    {
        private static readonly TinyIoCContainer Container = TinyIoCContainer.Current;

        public static void Register()
        {
            Container.Register<Database>().AsSingleton();
            Container.Register<IAssetProvider, AssetProvider>();
            Container.Register<ICustomIconDecoder, CustomIconDecoder>();
            Container.Register<IIconResolver, IconResolver>();

            RegisterRepositories();
            RegisterServices();
            RegisterViews();
        }

        public static void RegisterApplicationContext(Context context)
        {
            Container.Register(context);
        }

        private static void RegisterRepositories()
        {
            Container.Register<IAuthenticatorRepository, AuthenticatorRepository>();
            Container.Register<ICategoryRepository, CategoryRepository>();
            Container.Register<IAuthenticatorCategoryRepository, AuthenticatorCategoryRepository>();
            Container.Register<ICustomIconRepository, CustomIconRepository>();
        }

        private static void RegisterServices()
        {
            Container.Register<IAuthenticatorCategoryService, AuthenticatorCategoryService>();
            Container.Register<IAuthenticatorService, AuthenticatorService>();
            Container.Register<IBackupService, BackupService>();
            Container.Register<ICategoryService, CategoryService>();
            Container.Register<ICustomIconService, CustomIconService>();
            Container.Register<IImportService, ImportService>();
            Container.Register<IQrCodeService, QrCodeService>();
            Container.Register<IRestoreService, RestoreService>();
        }

        private static void RegisterViews()
        {
            Container.Register<IAuthenticatorView, AuthenticatorView>().AsMultiInstance();
            Container.Register<ICategoryView, CategoryView>().AsMultiInstance();
            Container.Register<IIconView, IconView>().AsMultiInstance();
        }

        public static T Resolve<T>() where T : class
        {
            return Container.Resolve<T>();
        }
    }
}