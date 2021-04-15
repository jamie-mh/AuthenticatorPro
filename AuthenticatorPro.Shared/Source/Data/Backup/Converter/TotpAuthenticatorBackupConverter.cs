using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AuthenticatorPro.Shared.Source.Util;
using Newtonsoft.Json;
using PCLCrypto;
using SimpleBase;
using SymmetricAlgorithm = PCLCrypto.SymmetricAlgorithm;

namespace AuthenticatorPro.Shared.Source.Data.Backup.Converter
{
    public class TotpAuthenticatorBackupConverter : BackupConverter
    {
        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Always;
        private const AuthenticatorType Type = AuthenticatorType.Totp;

        public TotpAuthenticatorBackupConverter(IIconResolver iconResolver) : base(iconResolver)
        {
            
        }
        
        public override Task<Backup> Convert(byte[] data, string password = null)
        {
            var sha256 = SHA256.Create();
            var passwordBytes = Encoding.UTF8.GetBytes(password ?? throw new ArgumentNullException(nameof(password)));
            var keyMaterial = sha256.ComputeHash(passwordBytes);

            var provider = WinRTCrypto.SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithm.AesCbc);
            var key = provider.CreateSymmetricKey(keyMaterial);

            var stringData = Encoding.UTF8.GetString(data);
            var actualBytes = System.Convert.FromBase64String(stringData);
            
            var raw = WinRTCrypto.CryptographicEngine.Decrypt(key, actualBytes);
            var json = Encoding.UTF8.GetString(raw);

            // Deal with strange json
            json = json.Substring(2);
            json = json.Substring(0, json.LastIndexOf(']') + 1);
            json = json.Replace(@"\""", @"""");

            var sourceAccounts = JsonConvert.DeserializeObject<List<Account>>(json);
            var authenticators = sourceAccounts.Select(entry => entry.Convert(_iconResolver)).ToList();
            
            return Task.FromResult(new Backup(authenticators));
        }

        private class Account
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

                if(Issuer == "Unknown")
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

                // TODO: figure out if this value ever changes
                if(Base != 16)
                    throw new ArgumentException("Cannot parse base other than 16");
                
                var secretBytes = Base16.Decode(Key);
                var secret = Base32.Rfc4648.Encode(secretBytes);

                return new Authenticator
                {
                    Issuer = issuer,
                    Username = username,
                    Type = Type,
                    Period = period,
                    Digits = digits,
                    Algorithm = Authenticator.DefaultAlgorithm,
                    Secret = Authenticator.CleanSecret(secret, Type),
                    Icon = iconResolver.FindServiceKeyByName(issuer)
                };
            }
        }
    }
}