using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using AuthenticatorPro.Shared.Source.Data;
using Polly;
using SQLite;

namespace AuthenticatorPro.UWP.Data
{
    internal static class Database
    {
        private const string FileName = "proauth.db3";
        private const SQLiteOpenFlags Flags = SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.SharedCache;
        
        public static async Task<SQLiteAsyncConnection> GetConnection(string password)
        {
            var dbPath = GetPath();

            if(password == "")
                password = null;

            var connStr = new SQLiteConnectionString(dbPath, Flags, true, password);
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
            connection.Tracer = s => Debug.WriteLine(s);
            connection.TimeExecution = true;
#endif

            return connection;
        }
        
        private static Task AttemptAndRetry(Func<Task> action, int numRetries = 4)
        {
            static TimeSpan durationProvider(int attemptNumber) => TimeSpan.FromMilliseconds(Math.Pow(2, attemptNumber));
            return Policy.Handle<SQLiteException>().WaitAndRetryAsync(numRetries, durationProvider).ExecuteAsync(action);
        }

        private static string GetPath()
        {
            return Path.Combine(
                Windows.Storage.ApplicationData.Current.LocalFolder.Path,
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
            var conn = await GetConnection(currentPassword);

            // Change encryption mode
            if(currentPassword == null && newPassword != null || currentPassword != null && newPassword == null)
            {
                var tempPath = dbPath + ".temp";

                try
                {
                    if(newPassword != null)
                        await conn.ExecuteAsync("ATTACH DATABASE ? AS temporary KEY ?", tempPath, newPassword);
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
                    conn = await GetConnection(newPassword);
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
                        conn = await GetConnection(newPassword);
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
    }
}