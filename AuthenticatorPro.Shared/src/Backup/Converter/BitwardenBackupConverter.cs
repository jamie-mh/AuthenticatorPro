// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Entity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthenticatorPro.Shared.Backup.Converter
{
    public class BitwardenBackupConverter : BackupConverter
    {
        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Never;
        private const int LoginType = 1;

        public BitwardenBackupConverter(IIconResolver iconResolver) : base(iconResolver) { }

        public override Task<ConversionResult> ConvertAsync(byte[] data, string password = null)
        {
            var json = Encoding.UTF8.GetString(data);
            var export = JsonConvert.DeserializeObject<Export>(json);

            var convertableItems = export.Items.Where(item =>
                item.Type == LoginType && item.Login != null && !String.IsNullOrEmpty(item.Login.Totp)).ToList();

            var authenticators = new List<Authenticator>();
            var categories = export.Folders.Select(f => f.Convert()).ToList();
            var bindings = new List<AuthenticatorCategory>();
            var failures = new List<ConversionFailure>();

            foreach (var item in convertableItems)
            {
                Authenticator auth;

                try
                {
                    auth = item.Convert(IconResolver);
                    auth.Validate();
                }
                catch (Exception e)
                {
                    failures.Add(new ConversionFailure
                    {
                        Description = item.Name,
                        Error = e.Message
                    });

                    continue;
                }

                authenticators.Add(auth);

                if (item.FolderId != null)
                {
                    var folderName = export.Folders.First(f => f.Id == item.FolderId).Name;
                    var category = categories.First(c => c.Name == folderName);

                    bindings.Add(new AuthenticatorCategory(auth.Secret, category.Id));
                }
            }

            for (var i = 0; i < convertableItems.Count; ++i)
            {
                var folderId = convertableItems[i].FolderId;

                if (folderId == null)
                {
                    continue;
                }

                var folderName = export.Folders.First(f => f.Id == folderId).Name;
                var category = categories.First(c => c.Name == folderName);
                var auth = authenticators[i];

                bindings.Add(new AuthenticatorCategory(auth.Secret, category.Id));
            }

            var backup = new Backup(authenticators, categories, bindings);
            var result = new ConversionResult { Failures = failures, Backup = backup };

            return Task.FromResult(result);
        }

        private class Export
        {
            [JsonProperty(PropertyName = "folders")]
            public List<Folder> Folders { get; set; }

            [JsonProperty(PropertyName = "items")] public List<Item> Items { get; set; }
        }

        private class Folder
        {
            [JsonProperty(PropertyName = "id")] public string Id { get; set; }

            [JsonProperty(PropertyName = "name")] public string Name { get; set; }

            public Category Convert()
            {
                return new Category(Name);
            }
        }

        private class Item
        {
            [JsonProperty(PropertyName = "name")] public string Name { get; set; }

            [JsonProperty(PropertyName = "folderId")]
            public string FolderId { get; set; }

            [JsonProperty(PropertyName = "type")] public int Type { get; set; }

            [JsonProperty(PropertyName = "login")] public Login Login { get; set; }

            public Authenticator Convert(IIconResolver iconResolver)
            {
                Authenticator ConvertFromInfo(AuthenticatorType type, string secret)
                {
                    return new Authenticator
                    {
                        Issuer = Name,
                        Username = Login.Username,
                        Type = type,
                        Algorithm = Authenticator.DefaultAlgorithm,
                        Digits = type.GetDefaultDigits(),
                        Period = type.GetDefaultPeriod(),
                        Icon = iconResolver.FindServiceKeyByName(Name),
                        Secret = Authenticator.CleanSecret(secret, type)
                    };
                }

                if (Login.Totp.StartsWith("otpauth"))
                {
                    return Authenticator.ParseUri(Login.Totp, iconResolver).Authenticator;
                }

                if (Login.Totp.StartsWith("steam"))
                {
                    var secret = Login.Totp["steam://".Length..];
                    return ConvertFromInfo(AuthenticatorType.SteamOtp, secret);
                }

                return ConvertFromInfo(AuthenticatorType.Totp, Login.Totp);
            }
        }

        private class Login
        {
            [JsonProperty(PropertyName = "username")]
            public string Username { get; set; }

            [JsonProperty(PropertyName = "totp")] public string Totp { get; set; }
        }
    }
}