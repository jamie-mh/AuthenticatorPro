// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Service;
using AuthenticatorPro.Core.Service.Impl;
using Microsoft.Extensions.DependencyInjection;

namespace AuthenticatorPro.Test
{
    public class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
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