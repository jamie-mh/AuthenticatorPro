// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AuthenticatorPro.Core.Backup;
using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Util;
using SimpleBase;
using SQLite;

namespace AuthenticatorPro.Core.Converter
{
    public class AuthenticatorPlusBackupConverter : BackupConverter
    {
        private const string BlizzardIssuer = "Blizzard";
        private const int BlizzardDigits = 8;

        public AuthenticatorPlusBackupConverter(IIconResolver iconResolver) : base(iconResolver)
        {
        }

        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Always;

        public override async Task<ConversionResult> ConvertAsync(byte[] data, string password = null)
        {
            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            try
            {
                await File.WriteAllBytesAsync(path, data);

                var connStr = new SQLiteConnectionString(path, true, password, null,
                    conn => { conn.ExecuteScalar<string>("PRAGMA cipher_compatibility = 3"); });

                var connection = new SQLiteAsyncConnection(connStr);

                try
                {
                    return await ConvertFromConnectionAsync(connection);
                }
                catch (SQLiteException e)
                {
                    throw new ArgumentException("Database cannot be opened", e);
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
            finally
            {
                File.Delete(path);
            }
        }

        private async Task<ConversionResult> ConvertFromConnectionAsync(SQLiteAsyncConnection connection)
        {
            var sourceAccounts = await connection.QueryAsync<Account>("SELECT * FROM accounts");
            var sourceCategories = await connection.QueryAsync<Category>("SELECT * FROM category");

            var authenticators = new List<Authenticator>();
            var categories = sourceCategories.Select(category => category.Convert()).ToList();
            var bindings = new List<AuthenticatorCategory>();
            var failures = new List<ConversionFailure>();

            foreach (var account in sourceAccounts)
            {
                Authenticator auth;

                try
                {
                    auth = account.Convert(IconResolver);
                    auth.Validate();
                }
                catch (Exception e)
                {
                    failures.Add(new ConversionFailure { Description = account.Issuer, Error = e.Message });

                    continue;
                }

                authenticators.Add(auth);

                if (account.CategoryName != "All Accounts")
                {
                    var category = categories.FirstOrDefault(c => c.Name == account.CategoryName);

                    if (category == null)
                    {
                        continue;
                    }

                    var binding = new AuthenticatorCategory
                    {
                        AuthenticatorSecret = auth.Secret, CategoryId = category.Id
                    };

                    bindings.Add(binding);
                }
            }

            var backup = new Backup.Backup
            {
                Authenticators = authenticators, Categories = categories, AuthenticatorCategories = bindings
            };

            return new ConversionResult { Failures = failures, Backup = backup };
        }

        private enum Type
        {
            Totp = 0,
            Hotp = 1,
            Blizzard = 2
        }

        private sealed class Account
        {
            [Column("email")]
            public string Email { get; set; }

            [Column("secret")]
            public string Secret { get; set; }

            [Column("counter")]
            public int Counter { get; set; }

            [Column("type")]
            public Type Type { get; set; }

            [Column("issuer")]
            public string Issuer { get; set; }

            [Column("original_name")]
            public string OriginalName { get; set; }

            [Column("category")]
            public string CategoryName { get; set; }

            private string ConvertSecret(AuthenticatorType type)
            {
                if (Type == Type.Blizzard)
                {
                    var bytes = Base16.Decode(Secret);
                    var base32Secret = Base32.Rfc4648.Encode(bytes);
                    return SecretUtil.Clean(base32Secret, type);
                }

                return SecretUtil.Clean(Secret, type);
            }

            public Authenticator Convert(IIconResolver iconResolver)
            {
                var type = Type switch
                {
                    Type.Totp => AuthenticatorType.Totp,
                    Type.Hotp => AuthenticatorType.Hotp,
                    Type.Blizzard => AuthenticatorType.Totp,
                    _ => throw new ArgumentException($"Type '{Type}' not supported")
                };

                string issuer;
                string username = null;

                if (!string.IsNullOrEmpty(Issuer))
                {
                    issuer = Type == Type.Blizzard ? BlizzardIssuer : Issuer;

                    if (!string.IsNullOrEmpty(Email))
                    {
                        username = Email;
                    }
                }
                else
                {
                    var originalNameParts = OriginalName.Split(new[] { ':' }, 2);

                    if (originalNameParts.Length == 2)
                    {
                        issuer = originalNameParts[0];

                        if (issuer == "")
                        {
                            issuer = Email;
                        }
                        else
                        {
                            username = Email;
                        }
                    }
                    else
                    {
                        issuer = Email;
                    }
                }

                var digits = Type == Type.Blizzard ? BlizzardDigits : type.GetDefaultDigits();
                var secret = ConvertSecret(type);

                return new Authenticator
                {
                    Issuer = issuer.Truncate(Authenticator.IssuerMaxLength),
                    Username = username.Truncate(Authenticator.UsernameMaxLength),
                    Type = type,
                    Secret = secret,
                    Counter = Counter,
                    Digits = digits,
                    Icon = iconResolver.FindServiceKeyByName(issuer)
                };
            }
        }

        private sealed class Category
        {
            [Column("name")]
            public string Name { get; set; }

            public Entity.Category Convert()
            {
                return new Entity.Category(Name);
            }
        }
    }
}