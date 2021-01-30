using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SQLite;

namespace AuthenticatorPro.Shared.Source.Data.Backup.Converter
{
    public class AuthenticatorPlusBackupConverter : BackupConverter
    {
        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Always;

        public AuthenticatorPlusBackupConverter(IIconResolver iconResolver) : base(iconResolver)
        {
            
        }

        public override async Task<Backup> Convert(byte[] data, string password = null)
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                "authplus.db"
            );

            await Task.Run(delegate { File.WriteAllBytes(path, data); });
            
            var connStr = new SQLiteConnectionString(path, true, password);
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
            catch(SQLiteException)
            {
                throw new ArgumentException("Database cannot be opened");
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
            Totp = 0, Hotp = 1
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
                    _ => throw new ArgumentOutOfRangeException()
                };

                string issuer;
                string username = null;

                if(!String.IsNullOrEmpty(Issuer))
                {
                    issuer = Issuer;

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
                
                return new Authenticator
                {
                    Issuer = issuer,
                    Username = username,
                    Type = type,
                    Secret = Authenticator.CleanSecret(Secret, type),
                    Counter = Counter,
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