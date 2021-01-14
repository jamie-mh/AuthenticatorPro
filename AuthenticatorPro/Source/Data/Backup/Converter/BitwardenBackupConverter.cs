using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuthenticatorPro.Shared.Data;
using Newtonsoft.Json;

namespace AuthenticatorPro.Data.Backup.Converter
{
    internal class BitwardenBackupConverter : BackupConverter
    {
        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Never;
        private const AuthenticatorType Type = AuthenticatorType.Totp;
        
        public override Task<Backup> Convert(byte[] data, string password = null)
        {
            var json = Encoding.UTF8.GetString(data);
            var export = JsonConvert.DeserializeObject<Export>(json);
            var authenticators = export.Items.Where(item => !String.IsNullOrEmpty(item.Login.Totp)).Select(item => item.Convert()).ToList();

            return Task.FromResult(new Backup(authenticators));
        }

        private class Export
        {
            [JsonProperty(PropertyName = "items")]
            public List<Item> Items { get; set; }
        }

        private class Item
        {
            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }
            
            [JsonProperty(PropertyName = "login")]
            public Login Login { get; set; }

            
            public Authenticator Convert()
            {
                if(Login.Totp.StartsWith("otpauth"))
                    return Authenticator.FromOtpAuthUri(Login.Totp);
                
                return new Authenticator()
                {
                    Issuer = Name,
                    Username = Login.Username,
                    Type = Type,
                    Algorithm = Authenticator.DefaultAlgorithm,
                    Digits = Type.GetDefaultDigits(),
                    Period = Type.GetDefaultPeriod(),
                    Icon = Icon.FindServiceKeyByName(Name),
                    Secret = Authenticator.CleanSecret(Login.Totp, Type)
                };
            }
        }

        private class Login
        {
            [JsonProperty(PropertyName = "username")]
            public string Username { get; set; }
            
            [JsonProperty(PropertyName = "totp")]
            public string Totp { get; set; }
        }
    }
}