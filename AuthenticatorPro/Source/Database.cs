using System;
using System.IO;
using System.Threading.Tasks;
using Android.Content;
using AndroidX.Preference;
using AuthenticatorPro.Data;
using AuthenticatorPro.Util;
using SQLite;
using Xamarin.Essentials;

namespace AuthenticatorPro
{
    internal static class Database
    {
        public static async Task<SQLiteAsyncConnection> Connect(Context context)
        {
            var prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            var isEncrypted = prefs.GetBoolean("pref_useEncryptedDatabase", true);

            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                "proauth.db3"
            );

            SQLiteAsyncConnection connection;

            if(isEncrypted)
            {
                var databaseKey = await SecureStorage.GetAsync("database_key");

                if(databaseKey == null)
                {
                    databaseKey = Hash.SHA1(Guid.NewGuid().ToString());
                    await SecureStorage.SetAsync("database_key", databaseKey);
                }

                var connStr = new SQLiteConnectionString(dbPath, true, databaseKey);
                connection = new SQLiteAsyncConnection(connStr);
            }
            else
                connection = new SQLiteAsyncConnection(dbPath);

            await connection.CreateTableAsync<Authenticator>();
            await connection.CreateTableAsync<Category>();
            await connection.CreateTableAsync<AuthenticatorCategory>();

            return connection;
        }

        public static async Task UpdateEncryption(Context context, bool useKey)
        {
            var conn = await Connect(context);
            var tempPath = conn.DatabasePath.Replace("proauth", "temp");

            if(useKey)
            {
                var databaseKey = await SecureStorage.GetAsync("database_key");
                await conn.ExecuteAsync("ATTACH DATABASE ? AS temporary KEY ?", tempPath, databaseKey);
            }
            else
                await conn.ExecuteAsync("ATTACH DATABASE ? AS temporary KEY ''", tempPath);

            await conn.ExecuteScalarAsync<string>("SELECT sqlcipher_export('temporary')");
            await conn.ExecuteAsync("DETACH DATABASE temporary");

            await conn.CloseAsync();

            File.Delete(conn.DatabasePath);
            File.Delete(conn.DatabasePath.Replace("db3", "db3-shm"));
            File.Delete(conn.DatabasePath.Replace("db3", "db3-wal"));

            File.Move(tempPath, conn.DatabasePath);
        }
    }
}