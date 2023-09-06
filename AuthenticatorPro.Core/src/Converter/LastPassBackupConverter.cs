// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthenticatorPro.Core.Backup;
using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Generator;
using AuthenticatorPro.Core.Util;
using Newtonsoft.Json;

namespace AuthenticatorPro.Core.Converter
{
    public class LastPassBackupConverter : BackupConverter
    {
        public LastPassBackupConverter(IIconResolver iconResolver) : base(iconResolver)
        {
        }

        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Never;

        public override Task<ConversionResult> ConvertAsync(byte[] data, string password = null)
        {
            var json = Encoding.UTF8.GetString(data);
            var export = JsonConvert.DeserializeObject<Export>(json);

            if (export.Version != 3)
            {
                throw new ArgumentException($"Unsupported backup version {export.Version}");
            }

            var authenticators = new List<Authenticator>();
            var categories = new List<Category>();
            var bindings = new List<AuthenticatorCategory>();
            var failures = new List<ConversionFailure>();

            foreach (var account in export.Accounts)
            {
                Authenticator auth;

                try
                {
                    auth = account.Convert(IconResolver);
                    auth.Validate();
                }
                catch (Exception e)
                {
                    failures.Add(new ConversionFailure { Description = account.IssuerName, Error = e.Message });
                    continue;
                }

                if (account.FolderData.FolderId > 0)
                {
                    var folder = export.Folders.First(f => f.Id == account.FolderData.FolderId);
                    var category = categories.FirstOrDefault(c => c.Name == folder.Name);

                    if (category == null)
                    {
                        category = new Category(folder.Name);
                        categories.Add(category);
                    }

                    bindings.Add(new AuthenticatorCategory
                    {
                        CategoryId = category.Id,
                        AuthenticatorSecret = auth.Secret,
                        Ranking = account.FolderData.Position
                    });
                }

                authenticators.Add(auth);
            }

            var backup = new Backup.Backup
            {
                Authenticators = authenticators, Categories = categories, AuthenticatorCategories = bindings
            };

            var result = new ConversionResult { Failures = failures, Backup = backup };
            return Task.FromResult(result);
        }

        private sealed class Export
        {
            [JsonProperty(PropertyName = "version")]
            public int Version { get; set; }

            [JsonProperty(PropertyName = "accounts")]
            public List<Account> Accounts { get; set; }

            [JsonProperty(PropertyName = "folders")]
            public List<Folder> Folders { get; set; }
        }

        private sealed class Account
        {
            [JsonProperty(PropertyName = "issuerName")]
            public string IssuerName { get; set; }

            [JsonProperty(PropertyName = "userName")]
            public string UserName { get; set; }

            [JsonProperty(PropertyName = "secret")]
            public string Secret { get; set; }

            [JsonProperty(PropertyName = "timeStep")]
            public int TimeStep { get; set; }

            [JsonProperty(PropertyName = "digits")]
            public int Digits { get; set; }

            [JsonProperty(PropertyName = "algorithm")]
            public string Algorithm { get; set; }

            [JsonProperty(PropertyName = "folderData")]
            public FolderData FolderData { get; set; }

            public Authenticator Convert(IIconResolver iconResolver)
            {
                var algorithm = Algorithm switch
                {
                    "SHA1" => HashAlgorithm.Sha1,
                    "SHA256" => HashAlgorithm.Sha256,
                    "SHA512" => HashAlgorithm.Sha512,
                    _ => throw new ArgumentException($"Algorithm '{Algorithm}' not supported")
                };

                string issuer;
                string username;

                if (string.IsNullOrEmpty(IssuerName))
                {
                    issuer = UserName;
                    username = null;
                }
                else
                {
                    issuer = IssuerName;
                    username = UserName;
                }

                return new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Algorithm = algorithm,
                    Secret = SecretUtil.Clean(Secret, AuthenticatorType.Totp),
                    Digits = Digits,
                    Period = TimeStep,
                    Issuer = issuer.Truncate(Authenticator.IssuerMaxLength),
                    Username = username.Truncate(Authenticator.UsernameMaxLength),
                    Icon = iconResolver.FindServiceKeyByName(issuer)
                };
            }
        }

        private sealed class FolderData
        {
            [JsonProperty(PropertyName = "folderId")]
            public int FolderId { get; set; }

            [JsonProperty(PropertyName = "position")]
            public int Position { get; set; }
        }

        private sealed class Folder
        {
            [JsonProperty(PropertyName = "id")]
            public int Id { get; set; }

            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }
        }
    }
}