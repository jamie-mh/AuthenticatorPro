using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OtpNet;

namespace AuthenticatorPro.Shared.Source.Data.Backup.Converter
{
    public class AegisBackupConverter : BackupConverter
    {
        // Encrypted backups not yet supported, see andOTP converter
        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Never;

        public AegisBackupConverter(IIconResolver iconResolver) : base(iconResolver)
        {
        
        }
        
        public override async Task<Backup> Convert(byte[] data, string password = null)
        {
            var json = Encoding.UTF8.GetString(data);
            var backup = JsonConvert.DeserializeObject<AegisBackup>(json);

            if(backup.Version != 1)
                throw new NotSupportedException("Unsupported backup version");
            
            var authenticators = backup.Database.Entries.Select(entry => entry.Convert(_iconResolver)).ToList();
            var categories = new List<Category>();
            var bindings = new List<AuthenticatorCategory>();
            var icons = new List<CustomIcon>();

            for(var i = 0; i < backup.Database.Entries.Count; i++)
            {
                var entry = backup.Database.Entries[i];
                var auth = authenticators[i];

                if(!String.IsNullOrEmpty(entry.Group))
                {
                    var category = categories.FirstOrDefault(c => c.Name == entry.Group);

                    if(category == null)
                    {
                        category = new Category(entry.Group);
                        categories.Add(category);
                    }

                    var binding = new AuthenticatorCategory(auth.Secret, category.Id);
                    bindings.Add(binding);
                }

                // TODO: custom icon generator for platform?

                // if(entry.Icon != null)
                // {
                //     var newIcon = await CustomIcon.FromBytes(entry.Icon);
                //     var icon = icons.FirstOrDefault(ic => ic.Id == newIcon.Id);
                //
                //     if(icon == null)
                //     {
                //         icon = newIcon;
                //         icons.Add(newIcon);
                //     }
                //
                //     auth.Icon = CustomIcon.Prefix + icon.Id;
                // }
            }

            return new Backup(authenticators, categories, bindings, icons);
        }

        private class AegisBackup
        {
            [JsonProperty(PropertyName = "version")]
            public int Version { get; set; }
            
            [JsonProperty(PropertyName = "db")]
            public Database Database { get; set; }
        }

        private class Database
        {
            [JsonProperty(PropertyName = "entries")]
            public List<Entry> Entries { get; set; }
        }

        private class Entry
        {
            [JsonProperty(PropertyName = "type")]
            public string Type { get; set; }
            
            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }
            
            [JsonProperty(PropertyName = "issuer")]
            public string Issuer { get; set; }
            
            [JsonProperty(PropertyName = "group")]
            public string Group { get; set; }
            
            [JsonProperty(PropertyName = "icon")]
            [JsonConverter(typeof(ByteArrayConverter))]
            public byte[] Icon { get; set; }
            
            [JsonProperty(PropertyName = "info")]
            public EntryInfo Info { get; set; }


            public Authenticator Convert(IIconResolver iconResolver)
            {
                var type = Type switch
                {
                    "totp" => AuthenticatorType.Totp,
                    "hotp" => AuthenticatorType.Hotp,
                    "steam" => AuthenticatorType.SteamOtp,
                    _ => throw new ArgumentException("Unknown type")
                };

                var algorithm = Info.Algorithm switch
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
                    issuer = Name;
                    username = null;
                }
                else
                {
                    issuer = Issuer;
                    username = Name;
                }

                return new Authenticator
                {
                    Type = type,
                    Algorithm = algorithm,
                    Secret = Authenticator.CleanSecret(Info.Secret, type),
                    Digits = Info.Digits,
                    Period = Info.Period,
                    Issuer = issuer,
                    Username = username,
                    Counter = Info.Counter,
                    Icon = iconResolver.FindServiceKeyByName(issuer)
                };
            }
        }

        private class EntryInfo
        {
            [JsonProperty(PropertyName = "secret")]
            public string Secret { get; set; } 
            
            [JsonProperty(PropertyName = "algo")]
            public string Algorithm { get; set; }
            
            [JsonProperty(PropertyName = "digits")]
            public int Digits { get; set; } 
            
            [JsonProperty(PropertyName = "period")]
            public int Period { get; set; } 
            
            [JsonProperty(PropertyName = "counter")]
            public int Counter { get; set; } 
        }
    }
}