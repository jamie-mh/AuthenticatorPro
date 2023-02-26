// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared;
using AuthenticatorPro.Shared.Backup;
using AuthenticatorPro.Shared.Persistence;
using AuthenticatorPro.Shared.Service;
using AuthenticatorPro.Shared.Service.Impl;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.IO;

namespace AuthenticatorPro.Test
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IIconResolver, MockIconResolver>();

            services.AddTransient(provider =>
            {
                var contents = File.ReadAllText("test.authpro");
                return JsonConvert.DeserializeObject<Backup>(contents);
            });

            services.AddSingleton<IAuthenticatorRepository, InMemoryAuthenticatorRepository>();
            services.AddSingleton<IAuthenticatorCategoryRepository, InMemoryAuthenticatorCategoryRepository>();
            services.AddSingleton<ICategoryRepository, InMemoryCategoryRepository>();
            services.AddSingleton<ICustomIconRepository, InMemoryCustomIconRepository>();

            services.AddScoped<IAuthenticatorCategoryService, AuthenticatorCategoryService>();
            services.AddScoped<IAuthenticatorService, AuthenticatorService>();
            services.AddScoped<IBackupService, BackupService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<ICustomIconService, CustomIconService>();
            services.AddScoped<IImportService, ImportService>();
            services.AddScoped<IQrCodeService, QrCodeService>();
            services.AddScoped<IRestoreService, RestoreService>();
        }
    }
}