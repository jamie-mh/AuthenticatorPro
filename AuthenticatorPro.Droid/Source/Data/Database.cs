using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AndroidX.Preference;
using AuthenticatorPro.Droid.Util;
using AuthenticatorPro.Shared.Data;
using Polly;
using SQLite;
using Xamarin.Essentials;
using Context = Android.Content.Context;

namespace AuthenticatorPro.Droid.Data
{
    internal static class Database
    {
        private const string FileName = "proauth.db3";
        private const SQLiteOpenFlags Flags = SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.SharedCache;
        
        private static readonly SemaphoreSlim _sharedLock = new SemaphoreSlim(1, 1);
        private static SQLiteAsyncConnection _sharedConnection;

        public static async Task<SQLiteAsyncConnection> GetSharedConnection()
        {
            await _sharedLock.WaitAsync();

            if(_sharedConnection == null)
            {
                _sharedLock.Release();
                throw new InvalidOperationException("Shared connection not open");
            }

            _sharedLock.Release();
            return _sharedConnection;
        }

        public static async Task OpenSharedConnection(string password)
        {
            await _sharedLock.WaitAsync();

            try
            {
                _sharedConnection = await GetPrivateConnection(password);
            }
            finally
            {
                _sharedLock.Release();
            }
        }

        public static async Task CloseSharedConnection()
        {
            await _sharedLock.WaitAsync();

            if(_sharedConnection == null)
            {
                _sharedLock.Release(); 
                return;
            }

            try
            {
                await _sharedConnection.CloseAsync();
                _sharedConnection = null;
            }
            finally
            {
                _sharedLock.Release();
            }
        }

        public static async Task<SQLiteAsyncConnection> GetPrivateConnection(string password)
        {
            var dbPath = GetPath();

            if(password == "")
                password = null;

            var connStr = new SQLiteConnectionString(dbPath, Flags, true, password, null, conn =>
            {
                // TODO: update to SQLCipher 4 encryption
                // Performance issue: https://github.com/praeclarum/sqlite-net/issues/978
                conn.Execute("PRAGMA cipher_compatibility = 3");
            });
            
            var connection = new SQLiteAsyncConnection(connStr);

            try
            {
                await AttemptAndRetry(() => connection.EnableWriteAheadLoggingAsync());
                await AttemptAndRetry(() => connection.CreateTableAsync<Authenticator>());
                await AttemptAndRetry(() => connection.CreateTableAsync<Category>());
                await AttemptAndRetry(() => connection.CreateTableAsync<AuthenticatorCategory>());
                await AttemptAndRetry(() => connection.CreateTableAsync<CustomIcon>());
            }
            catch
            {
                await connection.CloseAsync();
                throw;
            }

#if DEBUG
            connection.Trace = true;
            connection.Tracer = Logger.Info;
            connection.TimeExecution = true;
#endif

            return connection;
        }
        
        private static Task AttemptAndRetry(Func<Task> action, int numRetries = 4)
        {
            static TimeSpan DurationProvider(int attemptNumber) => TimeSpan.FromMilliseconds(Math.Pow(2, attemptNumber));
            return Policy.Handle<SQLiteException>().WaitAndRetryAsync(numRetries, DurationProvider).ExecuteAsync(action);
        }

        private static string GetPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                FileName
            );
        }

        public static async Task SetPassword(string currentPassword, string newPassword)
        {
            var dbPath = GetPath();
            var backupPath = dbPath + ".backup";
                
            void DeleteDatabase()
            {
                File.Delete(dbPath);
                File.Delete(dbPath.Replace("db3", "db3-shm"));
                File.Delete(dbPath.Replace("db3", "db3-wal"));
            }
            
            void RestoreBackup()
            {
                DeleteDatabase();
                File.Move(backupPath, dbPath);
            }
            
            File.Copy(dbPath, backupPath, true);
            SQLiteAsyncConnection conn;

            try
            {
                conn = await GetPrivateConnection(currentPassword);
                await conn.ExecuteScalarAsync<string>("PRAGMA wal_checkpoint(TRUNCATE)");
            }
            catch
            {
                File.Delete(backupPath);
                throw;
            }

            // Change encryption mode
            if(currentPassword == null && newPassword != null || currentPassword != null && newPassword == null)
            {
                var tempPath = dbPath + ".temp";

                try
                {
                    if(newPassword != null)
                    {
                        await conn.ExecuteAsync("ATTACH DATABASE ? AS temporary KEY ?", tempPath, newPassword);
                        await conn.ExecuteAsync("PRAGMA temporary.cipher_compatibility = 3");
                    }
                    else
                        await conn.ExecuteAsync("ATTACH DATABASE ? AS temporary KEY ''", tempPath);

                    await conn.ExecuteScalarAsync<string>("SELECT sqlcipher_export('temporary')");
                }
                catch
                {
                    File.Delete(tempPath);
                    File.Delete(backupPath);
                    throw;
                }
                finally
                {
                    await conn.ExecuteAsync("DETACH DATABASE temporary");
                    await conn.CloseAsync();
                }
                
                try
                {
                    DeleteDatabase();
                    File.Move(tempPath, dbPath);
                    conn = await GetPrivateConnection(newPassword);
                }
                catch
                {
                    // Perhaps it wasn't moved correctly
                    File.Delete(tempPath);
                    RestoreBackup();
                    throw;
                }
                finally
                {
                    await conn.CloseAsync();
                    File.Delete(backupPath);
                }
            }
            // Change password
            else
            {
                // Cannot use parameters with pragma https://github.com/ericsink/SQLitePCL.raw/issues/153
                var quoted = "'" + newPassword.Replace("'", "''") + "'";

                try
                {
                    try
                    {
                        await conn.ExecuteAsync($"PRAGMA rekey = {quoted}");
                    }
                    finally
                    {
                        await conn.CloseAsync();
                    }

                    try
                    {
                        conn = await GetPrivateConnection(newPassword);
                    }
                    finally
                    {
                        await conn.CloseAsync();
                    }
                }
                catch
                {
                    RestoreBackup();
                    throw;
                }
                finally
                {
                    File.Delete(backupPath);
                }
            }
        }

        public static async Task UpgradeLegacy(Context context)
        {
            var prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            var oldPref = prefs.GetBoolean("pref_useEncryptedDatabase", false);
            
            if(!oldPref)
                return;
                   
            string key = null;
            
            await Task.Run(async delegate
            {
                key = await SecureStorage.GetAsync("database_key");
            });

            // this shouldn't happen
            if(key == null)
                return;

            await SetPassword(key, null);
            prefs.Edit().PutBoolean("pref_useEncryptedDatabase", false).Commit();
        }
    }
}