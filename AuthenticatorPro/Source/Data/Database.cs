using System;
using System.IO;
using System.Threading.Tasks;
using SQLite;

namespace AuthenticatorPro.Data
{
    internal static class Database
    {
        private const string FileName = "proauth.db3";
        private static SQLiteAsyncConnection _sharedConnection;

        public static async Task<SQLiteAsyncConnection> GetSharedConnection()
        {
            if(_sharedConnection == null)
                throw new Exception("Shared connection not open");

            try
            {
                // Attempt to use the connection
                await _sharedConnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM sqlite_master");
            }
            catch
            {
                _sharedConnection.CloseAsync();
                _sharedConnection = null;
                throw;
            }

            return _sharedConnection;
        }

        public static async Task OpenSharedConnection(string password)
        {
            _sharedConnection = await GetPrivateConnection(password);
        }

        public static async Task CloseSharedConnection()
        {
            if(_sharedConnection != null)
                await _sharedConnection.CloseAsync();

            _sharedConnection = null;
        }

        public static async Task<SQLiteAsyncConnection> GetPrivateConnection(string password)
        {
            // TODO: migrate from old encryption
            
            var dbPath = GetPath();
            SQLiteAsyncConnection connection;

            if(!String.IsNullOrEmpty(password))
            {
                var connStr = new SQLiteConnectionString(dbPath, true, password);
                connection = new SQLiteAsyncConnection(connStr);
            }
            else
                connection = new SQLiteAsyncConnection(dbPath);

            try
            {
                await connection.EnableWriteAheadLoggingAsync();
                await connection.CreateTableAsync<Authenticator>();
                await connection.CreateTableAsync<Category>();
                await connection.CreateTableAsync<AuthenticatorCategory>();
                await connection.CreateTableAsync<CustomIcon>();
            }
            catch
            {
                await connection.CloseAsync();
                throw;
            }

            return connection;
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
            var conn = await GetPrivateConnection(currentPassword);
            var dbPath = GetPath();

            // Change encryption mode
            if(currentPassword == null && newPassword != null || currentPassword != null && newPassword == null)
            {
                var backupPath = dbPath + ".backup";
                var tempPath = dbPath + ".temp";
                
                await conn.BackupAsync(backupPath);

                try
                {
                    if(newPassword != null)
                        await conn.ExecuteAsync("ATTACH DATABASE ? AS temporary KEY ?", tempPath, newPassword);
                    else
                        await conn.ExecuteAsync("ATTACH DATABASE ? AS temporary KEY ''", tempPath);

                    await conn.ExecuteScalarAsync<string>("SELECT sqlcipher_export('temporary')");
                    await conn.ExecuteAsync("DETACH DATABASE temporary");
                }
                finally
                {
                    await conn.CloseAsync();
                }
                
                void DeleteDatabase()
                {
                    File.Delete(dbPath);
                    File.Delete(dbPath.Replace("db3", "db3-shm"));
                    File.Delete(dbPath.Replace("db3", "db3-wal"));
                }
                
                try
                {
                    DeleteDatabase();
                    File.Move(tempPath, dbPath);
                    File.Delete(tempPath);
                
                    conn = await GetPrivateConnection(newPassword);
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
                    File.Delete(backupPath);
                }
            }
            // Change password
            else
            {
                // Cannot use parameters with pragma https://github.com/ericsink/SQLitePCL.raw/issues/153
                var quoted = "'" + newPassword.Replace("'", "''") + "'";
                
                await conn.ExecuteAsync($"PRAGMA rekey = {quoted}");
                await conn.CloseAsync();
                
                try
                {
                    conn = await GetPrivateConnection(newPassword);
                }
                finally
                {
                    await conn.CloseAsync();
                }
            }
        }
    }
}