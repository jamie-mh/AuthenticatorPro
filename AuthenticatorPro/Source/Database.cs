using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AuthenticatorPro.Data;
using SQLite;
using Xamarin.Essentials;

namespace AuthenticatorPro
{
    internal static class Database
    {
        public static async Task<SQLiteAsyncConnection> Connect()
        {
            var databaseKey = await SecureStorage.GetAsync("database_key");

            if(databaseKey == null)
            {
                databaseKey = Hash.SHA1(Guid.NewGuid().ToString());
                await SecureStorage.SetAsync("database_key", databaseKey);
            }

            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                "proauth.db3"
            );

            var connection = new SQLiteAsyncConnection(dbPath, true, databaseKey);
            await connection.QueryAsync<int>($@"PRAGMA key='{databaseKey}'");

            await connection.CreateTableAsync<Authenticator>();
            await connection.CreateTableAsync<Category>();
            await connection.CreateTableAsync<AuthenticatorCategory>();

            return connection;
        }
    }
}