using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AuthenticatorPro.Data;
using SQLite;

namespace AuthenticatorPro.Utilities
{
    internal static class Database
    {
        [DllImport("libProAuthKey", EntryPoint = "get_key")]
        private static extern string GetDatabaseKey();

        public static async Task<SQLiteAsyncConnection> Connect()
        {
            var key = GetDatabaseKey();

            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                "proauth.db3"
            );

            var connection = new SQLiteAsyncConnection(dbPath, true, key);
            await connection.QueryAsync<int>($@"PRAGMA key='{key}'");

            await connection.CreateTableAsync<Authenticator>();
            await connection.CreateTableAsync<Category>();
            await connection.CreateTableAsync<AuthenticatorCategory>();

            return connection;
        }
    }
}