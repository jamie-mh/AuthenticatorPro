using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AuthenticatorPro.Shared.Data;
using Newtonsoft.Json;
using OtpNet;
using PCLCrypto;
using SymmetricAlgorithm = PCLCrypto.SymmetricAlgorithm;

namespace AuthenticatorPro.Data.Backup.Converter
{
    internal class TotpAuthenticatorBackupConverter : BackupConverter
    {
        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Always;
        private const AuthenticatorType Type = AuthenticatorType.Totp;
        
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
            var authenticators = sourceAccounts.Select(entry => entry.Convert()).ToList();
            
            return Task.FromResult(new Backup(authenticators, null, null, null));
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


            private static byte[] HexToBytes(string data)
            {
                var len = data.Length;
                var output = new byte[len / 2];
                
                for(var i = 0; i < len; i += 2)
                    output[i / 2] = System.Convert.ToByte(data.Substring(i, 2), 16);
                
                return output;
            }
            
            public Authenticator Convert()
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
                
                var secret = Base32Encoding.ToString(HexToBytes(Key));

                return new Authenticator
                {
                    Issuer = issuer,
                    Username = username,
                    Type = Type,
                    Period = period,
                    Digits = digits,
                    Algorithm = Authenticator.DefaultAlgorithm,
                    Secret = Authenticator.CleanSecret(secret, Type),
                    Icon = Icon.FindServiceKeyByName(issuer)
                };
            }
        }
    }
}