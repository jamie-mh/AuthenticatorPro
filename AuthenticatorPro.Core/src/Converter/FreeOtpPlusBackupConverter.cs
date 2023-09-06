// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AuthenticatorPro.Core.Backup;
using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Generator;
using AuthenticatorPro.Core.Util;
using Newtonsoft.Json;
using SimpleBase;

namespace AuthenticatorPro.Core.Converter
{
    public class FreeOtpPlusBackupConverter : BackupConverter
    {
        public FreeOtpPlusBackupConverter(IIconResolver iconResolver) : base(iconResolver)
        {
        }

        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Never;

        public override Task<ConversionResult> ConvertAsync(byte[] data, string password = null)
        {
            var json = Encoding.UTF8.GetString(data);
            var sourceTokens = JsonConvert.DeserializeObject<FreeOtpPlusBackup>(json).Tokens;
            var authenticators = new List<Authenticator>();
            var failures = new List<ConversionFailure>();

            foreach (var token in sourceTokens)
            {
                Authenticator auth;

                try
                {
                    auth = token.Convert(IconResolver);
                    auth.Validate();
                }
                catch (Exception e)
                {
                    failures.Add(new ConversionFailure { Description = token.Issuer, Error = e.Message });
                    continue;
                }

                authenticators.Add(auth);
            }

            var backup = new Backup.Backup { Authenticators = authenticators };
            var result = new ConversionResult { Failures = failures, Backup = backup };

            return Task.FromResult(result);
        }

        private sealed class FreeOtpPlusBackup
        {
            [JsonProperty(PropertyName = "tokens")]
            public List<Token> Tokens { get; set; }
        }

        private sealed class Token
        {
            [JsonProperty(PropertyName = "algo")]
            public string Algorithm { get; set; }

            [JsonProperty(PropertyName = "counter")]
            public int Counter { get; set; }

            [JsonProperty(PropertyName = "digits")]
            public int Digits { get; set; }

            [JsonProperty(PropertyName = "issuerExt")]
            public string Issuer { get; set; }

            [JsonProperty(PropertyName = "label")]
            public string Label { get; set; }

            [JsonProperty(PropertyName = "period")]
            public int Period { get; set; }

            [JsonProperty(PropertyName = "type")]
            public string Type { get; set; }

            [JsonProperty(PropertyName = "secret")]
            public sbyte[] Secret { get; set; }

            public Authenticator Convert(IIconResolver iconResolver)
            {
                var type = Type switch
                {
                    "TOTP" => AuthenticatorType.Totp,
                    "HOTP" => AuthenticatorType.Hotp,
                    _ => throw new ArgumentException($"Type '{Type}' not supported")
                };

                var algorithm = Algorithm switch
                {
                    "SHA1" => HashAlgorithm.Sha1,
                    "SHA256" => HashAlgorithm.Sha256,
                    "SHA512" => HashAlgorithm.Sha512,
                    _ => throw new ArgumentException($"Algorithm '{Algorithm}' not supported")
                };

                string issuer;
                string username;

                if (string.IsNullOrEmpty(Issuer))
                {
                    issuer = Label;
                    username = null;
                }
                else
                {
                    issuer = Issuer;
                    username = Label;
                }

                return new Authenticator
                {
                    Issuer = issuer.Truncate(Authenticator.IssuerMaxLength),
                    Username = username.Truncate(Authenticator.UsernameMaxLength),
                    Algorithm = algorithm,
                    Type = type,
                    Counter = Counter,
                    Digits = Digits,
                    Icon = iconResolver.FindServiceKeyByName(issuer),
                    Period = Period,
                    Secret = Base32.Rfc4648.Encode((ReadOnlySpan<byte>) (Array) Secret)
                };
            }
        }
    }
}