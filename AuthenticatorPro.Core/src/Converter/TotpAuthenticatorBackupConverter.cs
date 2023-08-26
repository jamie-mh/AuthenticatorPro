// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AuthenticatorPro.Core.Backup;
using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Util;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using SimpleBase;

namespace AuthenticatorPro.Core.Converter
{
    public class TotpAuthenticatorBackupConverter : BackupConverter
    {
        private const AuthenticatorType Type = AuthenticatorType.Totp;
        private const string Algorithm = "AES/CBC/PKCS7";

        public TotpAuthenticatorBackupConverter(IIconResolver iconResolver) : base(iconResolver)
        {
        }

        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Always;

        public override async Task<ConversionResult> ConvertAsync(byte[] data, string password = null)
        {
            var accounts = await Task.Run(() => Decrypt(data, password));
            var authenticators = new List<Authenticator>();
            var failures = new List<ConversionFailure>();

            foreach (var account in accounts)
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
            }

            var backup = new Backup.Backup { Authenticators = authenticators };
            return new ConversionResult { Failures = failures, Backup = backup };
        }

        private static List<Account> Decrypt(byte[] data, string password)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password ?? throw new ArgumentNullException(nameof(password)));
            var key = SHA256.HashData(passwordBytes);

            var stringData = Encoding.UTF8.GetString(data);
            var actualBytes = Convert.FromBase64String(stringData);

            var keyParameter = new KeyParameter(key);
            var cipher = CipherUtilities.GetCipher(Algorithm);
            cipher.Init(false, keyParameter);

            byte[] raw;

            try
            {
                raw = cipher.DoFinal(actualBytes);
            }
            catch (InvalidCipherTextException e)
            {
                throw new ArgumentException("The password is incorrect", e);
            }

            var json = Encoding.UTF8.GetString(raw);

            // Deal with strange json
            json = json[2..];
            json = json[..(json.LastIndexOf(']') + 1)];
            json = json.Replace(@"\""", @"""");

            return JsonConvert.DeserializeObject<List<Account>>(json);
        }

        private sealed class Account
        {
            [JsonProperty(PropertyName = "issuer")]
            public string Issuer { get; set; }

            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "key")]
            public string Key { get; set; }

            [JsonProperty(PropertyName = "digits")]
            public string Digits { get; set; }

            [JsonProperty(PropertyName = "period")]
            public string Period { get; set; }

            [JsonProperty(PropertyName = "base")]
            public int Base { get; set; }

            public Authenticator Convert(IIconResolver iconResolver)
            {
                string issuer;
                string username;

                if (Issuer == "Unknown")
                {
                    issuer = Name;
                    username = null;
                }
                else
                {
                    issuer = Issuer;
                    username = Name;
                }

                var period = Period == ""
                    ? Type.GetDefaultPeriod()
                    : int.Parse(Period);

                var digits = Digits == ""
                    ? Type.GetDefaultDigits()
                    : int.Parse(Digits);

                if (Base != 16)
                {
                    throw new ArgumentException("Cannot parse base other than 16, given {Base}");
                }

                var secretBytes = Base16.Decode(Key);
                var secret = Base32.Rfc4648.Encode(secretBytes);

                return new Authenticator
                {
                    Issuer = issuer.Truncate(Authenticator.IssuerMaxLength),
                    Username = username.Truncate(Authenticator.UsernameMaxLength),
                    Type = Type,
                    Period = period,
                    Digits = digits,
                    Algorithm = Authenticator.DefaultAlgorithm,
                    Secret = SecretUtil.Clean(secret, Type),
                    Icon = iconResolver.FindServiceKeyByName(issuer)
                };
            }
        }
    }
}