using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SimpleBase;
using SQLite;

namespace AuthenticatorPro.Shared.Data.Backup.Converter
{
    public class AuthenticatorPlusBackupConverter : BackupConverter
    {
        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Always;

        private const string TempFileName = "authplus.db";
        private const string BlizzardIssuer = "Blizzard";
        private const int BlizzardDigits = 8;

        public AuthenticatorPlusBackupConverter(IIconResolver iconResolver) : base(iconResolver)
        {
            
        }

        public override async Task<Backup> Convert(byte[] data, string password = null)
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                TempFileName 
            );

            await File.WriteAllBytesAsync(path, data);
            
            var connStr = new SQLiteConnectionString(path, true, password, null, conn =>
            {
                conn.Execute("PRAGMA cipher_compatibility = 3");
            });
            
            var connection = new SQLiteAsyncConnection(connStr);

            Backup backup;

            try
            {
                var sourceAccounts = await connection.QueryAsync<Account>("SELECT * FROM accounts");
                var sourceCategories = await connection.QueryAsync<Category>("SELECT * FROM category");

                var authenticators = sourceAccounts.Select(account => account.Convert(_iconResolver)).ToList();
                var categories = sourceCategories.Select(category => category.Convert()).ToList();
                var bindings = new List<AuthenticatorCategory>();

                for(var i = 0; i < sourceAccounts.Count; ++i)
                {
                    var sourceAccount = sourceAccounts[i];

                    if(sourceAccount.CategoryName == "All Accounts")
                        continue;

                    var category = categories.FirstOrDefault(c => c.Name == sourceAccount.CategoryName);

                    if(category == null)
                        continue;

                    var auth = authenticators[i];
                    var binding = new AuthenticatorCategory
                    {
                        AuthenticatorSecret = auth.Secret, CategoryId = category.Id
                    };

                    bindings.Add(binding);
                }

                backup = new Backup(authenticators, categories, bindings);
            }
            catch(SQLiteException e)
            {
                throw new ArgumentException("Database cannot be opened", e);
            }
            finally
            {
                await connection.CloseAsync();
                File.Delete(path);
            }

            return backup;
        }

        private enum Type
        {
            Totp = 0, Hotp = 1, Blizzard = 2
        }

        private class Account
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

            public Authenticator Convert(IIconResolver iconResolver)
            {
                var type = Type switch
                {
                    Type.Totp => AuthenticatorType.Totp,
                    Type.Hotp => AuthenticatorType.Hotp,
                    Type.Blizzard => AuthenticatorType.Totp,
                    _ => throw new ArgumentOutOfRangeException(nameof(Type))
                };

                string issuer;
                string username = null;

                if(!String.IsNullOrEmpty(Issuer))
                {
                    issuer = Type == Type.Blizzard ? BlizzardIssuer : Issuer;

                    if(!String.IsNullOrEmpty(Email))
                        username = Email;
                }
                else
                {
                    var originalNameParts = OriginalName.Split(new[] { ':' }, 2);
                    
                    if(originalNameParts.Length == 2)
                    {
                        issuer = originalNameParts[0];

                        if(issuer == "")
                            issuer = Email;
                        else
                            username = Email;
                    }
                    else
                        issuer = Email;
                }

                var digits = Type == Type.Blizzard ? BlizzardDigits : type.GetDefaultDigits();

                string secret;

                if(Type == Type.Blizzard)
                {
                    var bytes = Base16.Decode(Secret);
                    var base32Secret = Base32.Rfc4648.Encode(bytes);
                    secret = Authenticator.CleanSecret(base32Secret, type);
                }
                else
                    secret = Authenticator.CleanSecret(Secret, type);
                
                return new Authenticator
                {
                    Issuer = issuer,
                    Username = username,
                    Type = type,
                    Secret = secret,
                    Counter = Counter,
                    Digits = digits,
                    Icon = iconResolver.FindServiceKeyByName(issuer)
                };
            }
        }

        private class Category
        {
            [Column("name")]
            public string Name { get; set; }
            
            public Data.Category Convert()
            {
                return new(Name);
            }
        }
    }
}