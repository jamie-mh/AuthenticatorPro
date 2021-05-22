// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthenticatorPro.Shared.Data.Generator;
using Newtonsoft.Json;
using SimpleBase;

namespace AuthenticatorPro.Shared.Data.Backup.Converter
{
    public class FreeOtpPlusBackupConverter : BackupConverter
    {
        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Never;

        public FreeOtpPlusBackupConverter(IIconResolver iconResolver) : base(iconResolver) { }
        
        public override Task<Backup> Convert(byte[] data, string password = null)
        {
            var json = Encoding.UTF8.GetString(data);
            var sourceTokens = JsonConvert.DeserializeObject<FreeOtpPlusBackup>(json).Tokens;
            var authenticators = sourceTokens.Select(account => account.Convert(_iconResolver)).ToList();

            return Task.FromResult(new Backup(authenticators));
        }

        private class FreeOtpPlusBackup
        {
            [JsonProperty(PropertyName = "tokens")]
            public List<Token> Tokens { get; set; }
        }

        private class Token
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
                    _ => throw new ArgumentOutOfRangeException(nameof(Type))
                };

                var algorithm = Algorithm switch
                {
                    "SHA1" => HashAlgorithm.Sha1,
                    "SHA256" => HashAlgorithm.Sha256,
                    "SHA512" => HashAlgorithm.Sha512,
                    _ => throw new ArgumentOutOfRangeException(nameof(Algorithm))
                };

                string issuer;
                string username;

                if(String.IsNullOrEmpty(Issuer))
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
                    Issuer = issuer,
                    Username = username,
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