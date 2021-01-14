using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuthenticatorPro.Shared.Data;
using Newtonsoft.Json;
using OtpNet;

namespace AuthenticatorPro.Data.Backup.Converter
{
    internal class FreeOtpPlusBackupConverter : BackupConverter
    {
        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Never;
        
        public override Task<Backup> Convert(byte[] data, string password = null)
        {
            var json = Encoding.UTF8.GetString(data);
            var sourceTokens = JsonConvert.DeserializeObject<FreeOtpPlusBackup>(json).Tokens;
            var authenticators = sourceTokens.Select(account => account.Convert()).ToList();

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


            public Authenticator Convert()
            {
                var type = Type switch
                {
                    "TOTP" => AuthenticatorType.Totp,
                    "HOTP" => AuthenticatorType.Hotp,
                    _ => throw new ArgumentException("Unknown type")
                };

                var algorithm = Algorithm switch
                {
                    "SHA1" => OtpHashMode.Sha1,
                    "SHA256" => OtpHashMode.Sha256,
                    "SHA512" => OtpHashMode.Sha512,
                    _ => throw new ArgumentException("Unknown algorithm")
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
                    Icon = Icon.FindServiceKeyByName(issuer),
                    Period = Period,
                    Secret = Base32Encoding.ToString((byte[]) (Array) Secret)
                };
            }
        }
    }
}