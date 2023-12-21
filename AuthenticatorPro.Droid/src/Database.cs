// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AuthenticatorPro.Core.Entity;
using Serilog;
using SQLite;

namespace AuthenticatorPro.Droid
{
    public class Database
    {
        public enum Origin
        {
            Application,
            Activity,
            Wear,
            AutoBackup,
            Other
        }

        private const string FileName = "proauth.db3";

        private const SQLiteOpenFlags Flags = SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite |
                                              SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.SharedCache;

        private readonly ILogger _log = Log.ForContext<Database>();
        private readonly SemaphoreSlim _lock = new(1, 1);
        private SQLiteAsyncConnection _connection;

        public async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            await _lock.WaitAsync();

            try
            {
                if (_connection == null)
                {
                    throw new InvalidOperationException("Connection not open");
                }

                return _connection;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<bool> IsOpenAsync(Origin origin)
        {
            await _lock.WaitAsync();

            try
            {
                var isOpen = _connection != null;
                _log.Debug("Is database open from {Origin}? {IsOpen}", origin, isOpen);
                return isOpen;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task CloseAsync(Origin origin)
        {
            await _lock.WaitAsync();

            try
            {
                if (_connection == null)
                {
                    return;
                }

                _log.Debug("Closing database from {Origin}", origin);

                await _connection.CloseAsync();
                _connection = null;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task OpenAsync(string password, Origin origin)
        {
            _log.Debug("Opening database from {Origin}", origin);

            var path = GetPath();
            var firstLaunch = !File.Exists(path);

            if (password == "")
            {
                password = null;
            }

            var connStr = new SQLiteConnectionString(path, Flags, true, password, null, conn =>
            {
                // TODO: update to SQLCipher 4 encryption
                // Performance issue: https://github.com/praeclarum/sqlite-net/issues/978
                if (password != null)
                {
                    conn.ExecuteScalar<string>("PRAGMA cipher_compatibility = 3");
                }
            });

            await _lock.WaitAsync();

            try
            {
                if (_connection != null)
                {
                    await _connection.CloseAsync();
                }

                _connection = new SQLiteAsyncConnection(connStr);

                try
                {
                    await MigrateAsync(firstLaunch);
                }
                catch
                {
                    await _connection.CloseAsync();
                    _connection = null;
                    throw;
                }

#if DEBUG
                _connection.Trace = true;
                _connection.Tracer = _log.Debug;
                _connection.TimeExecution = true;
#endif
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task MigrateAsync(bool firstLaunch)
        {
            if (firstLaunch)
            {
                await _connection.EnableWriteAheadLoggingAsync();
            }

            await _connection.CreateTableAsync<Authenticator>();
            await _connection.CreateTableAsync<Category>();
            await _connection.CreateTableAsync<AuthenticatorCategory>();
            await _connection.CreateTableAsync<CustomIcon>();
            await _connection.CreateTableAsync<IconPack>();
            await _connection.CreateTableAsync<IconPackEntry>();
        }

        private static string GetPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                FileName
            );
        }

        public async Task SetPasswordAsync(string currentPassword, string newPassword)
        {
            if (currentPassword == newPassword)
            {
                return;
            }

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
                conn = await GetConnectionAsync();
                await conn.ExecuteScalarAsync<string>("PRAGMA wal_checkpoint(TRUNCATE)");
            }
            catch
            {
                File.Delete(backupPath);
                throw;
            }

            // Change encryption mode
            if (currentPassword == null || newPassword == null)
            {
                var tempPath = dbPath + ".temp";

                try
                {
                    if (newPassword != null)
                    {
                        await conn.ExecuteAsync("ATTACH DATABASE ? AS temporary KEY ?", tempPath, newPassword);
                        await conn.ExecuteAsync("PRAGMA temporary.cipher_compatibility = 3");
                    }
                    else
                    {
                        await conn.ExecuteAsync("ATTACH DATABASE ? AS temporary KEY ''", tempPath);
                    }

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
                }

                try
                {
                    await CloseAsync(Origin.Other);
                    DeleteDatabase();
                    File.Move(tempPath, dbPath);
                    await OpenAsync(newPassword, Origin.Other);
                }
                catch
                {
                    // Perhaps it wasn't moved correctly
                    File.Delete(tempPath);
                    RestoreBackup();
                    await OpenAsync(currentPassword, Origin.Other);
                    throw;
                }
                finally
                {
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
                    await conn.ExecuteScalarAsync<string>($"PRAGMA rekey = {quoted}");

                    await CloseAsync(Origin.Other);
                    await OpenAsync(newPassword, Origin.Other);
                }
                catch
                {
                    RestoreBackup();
                    await OpenAsync(currentPassword, Origin.Other);
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