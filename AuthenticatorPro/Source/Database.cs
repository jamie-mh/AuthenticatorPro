using System;
using System.IO;
using System.Threading.Tasks;
using Android.Content;
using AndroidX.Preference;
using AuthenticatorPro.Data;
using AuthenticatorPro.Shared.Util;
using SQLite;
using Xamarin.Essentials;

namespace AuthenticatorPro
{
    internal static class Database
    {
        public static async Task<SQLiteAsyncConnection> Connect(Context context)
        {
            var prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            var isEncrypted = prefs.GetBoolean("pref_useEncryptedDatabase", false);

            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                "proauth.db3"
            );

            SQLiteAsyncConnection connection = null;
            
            async Task TryGetConnection(bool encrypted)
            {
                if(encrypted)
                {
                    var key = await GetKey();
                    var connStr = new SQLiteConnectionString(dbPath, true, key);
                    connection = new SQLiteAsyncConnection(connStr);
                }
                else
                    connection = new SQLiteAsyncConnection(dbPath);

                if(connection == null)
                    throw new Exception("Connection could not be established.");
               
                // Attempt to use the connection
                await connection.CreateTableAsync<Authenticator>();
            }

            if(isEncrypted)
                await TryGetConnection(true);
            else
            {
                try
                {
                    await TryGetConnection(false);
                }
                // The preference might not be initialised and the previous default value was true
                // Attempt an encrypted connection instead
                catch(SQLiteException)
                {
                    connection = null;
                    await TryGetConnection(true);
                    prefs.Edit().PutBoolean("pref_useEncryptedDatabase", true).Commit();
                }
            }

            await connection.CreateTableAsync<Category>();
            await connection.CreateTableAsync<AuthenticatorCategory>();
            await connection.CreateTableAsync<CustomIcon>();

            return connection;
        }

        private static async Task<string> GetKey()
        {
            var key = await SecureStorage.GetAsync("database_key");

            if(key != null)
                return key;
            
            key = Hash.Sha1(Guid.NewGuid().ToString());
            await SecureStorage.SetAsync("database_key", key);
            return key;
        }

        public static async Task SetEncryptionEnabled(Context context, bool encrypt)
        {
            var conn = await Connect(context);
            var tempPath = conn.DatabasePath.Replace("proauth", "temp");

            if(encrypt)
            {
                var key = await GetKey();
                await conn.ExecuteAsync("ATTACH DATABASE ? AS temporary KEY ?", tempPath, key);
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