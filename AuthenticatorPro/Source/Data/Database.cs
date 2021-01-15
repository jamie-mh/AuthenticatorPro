using System;
using System.IO;
using System.Threading.Tasks;
using Android.Content;
using AndroidX.Preference;
using AuthenticatorPro.Shared.Util;
using SQLite;
using Xamarin.Essentials;

namespace AuthenticatorPro.Data
{
    internal static class Database
    {
        private const string FileName = "proauth.db3";
        
        public static async Task<SQLiteAsyncConnection> Connect(Context context, bool? forcedEncryptionMode = null)
        {
            var prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            var isEncrypted = forcedEncryptionMode ?? prefs.GetBoolean("pref_useEncryptedDatabase", false);

            var dbPath = GetPath();
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
            
            try
            {
                await TryGetConnection(isEncrypted);
            }
            // The preference might not be initialised to the correct value
            // Attempt an encrypted or unencrypted connection instead
            catch(SQLiteException)
            {
                if(forcedEncryptionMode != null)
                    throw;
                
                connection = null;
                await TryGetConnection(!isEncrypted);
                prefs.Edit().PutBoolean("pref_useEncryptedDatabase", !isEncrypted).Commit();
            }

            await connection.EnableWriteAheadLoggingAsync();
            await connection.CreateTableAsync<Category>();
            await connection.CreateTableAsync<AuthenticatorCategory>();
            await connection.CreateTableAsync<CustomIcon>();

            return connection;
        }

        private static string GetPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                FileName
            );
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

        public static async Task SetEncryptionEnabled(Context context, bool shouldEncrypt)
        {
            var conn = await Connect(context);

            var dbPath = GetPath();
            var backupPath = dbPath + ".backup";
            var tempPath = dbPath + ".temp";
            
            await conn.BackupAsync(backupPath);
            
            if(shouldEncrypt)
            {
                var key = await GetKey();
                await conn.ExecuteAsync("ATTACH DATABASE ? AS temporary KEY ?", tempPath, key);
            }
            else
                await conn.ExecuteAsync("ATTACH DATABASE ? AS temporary KEY ''", tempPath);

            await conn.ExecuteScalarAsync<string>("SELECT sqlcipher_export('temporary')");
            await conn.ExecuteAsync("DETACH DATABASE temporary");
            await conn.CloseAsync();

            void DeleteDatabase()
            {
                File.Delete(dbPath);
                File.Delete(dbPath.Replace("db3", "db3-shm"));
                File.Delete(dbPath.Replace("db3", "db3-wal"));
            }

            DeleteDatabase();
            File.Move(tempPath, dbPath);

            try
            {
                conn = await Connect(context, shouldEncrypt);
            }
            catch
            {
                // Restore backup
                DeleteDatabase();
                File.Move(backupPath, dbPath);
                throw;
            }
            finally
            {
                await conn.CloseAsync();
            }
            
            File.Delete(backupPath);
        }
    }
}