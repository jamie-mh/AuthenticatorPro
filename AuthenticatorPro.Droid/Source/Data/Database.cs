// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AndroidX.Preference;
using AuthenticatorPro.Droid.Util;
using AuthenticatorPro.Shared.Data;
using SQLite;
using Xamarin.Essentials;
using Context = Android.Content.Context;

namespace AuthenticatorPro.Droid.Data
{
    internal static class Database
    {
        private const string FileName = "proauth.db3";
        private const SQLiteOpenFlags Flags = SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.SharedCache;
       
        private static readonly SemaphoreSlim Lock = new SemaphoreSlim(1, 1);
        private static SQLiteAsyncConnection _connection;
        public static bool IsOpen => _connection != null;

        public static async Task<SQLiteAsyncConnection> GetConnection()
        {
            await Lock.WaitAsync();

            if(_connection == null)
            {
                Lock.Release();
                throw new InvalidOperationException("Connection not open");
            }

            Lock.Release();
            return _connection;
        }

        public static async Task Close()
        {
            await Lock.WaitAsync();

            if(_connection == null)
            {
                Lock.Release(); 
                return;
            }

            try
            {
                await _connection.CloseAsync();
                _connection = null;
            }
            finally
            {
                Lock.Release();
            }
        }

        public static async Task<SQLiteAsyncConnection> Open(string password)
        {
            if(_connection != null)
                await Close();

            await Lock.WaitAsync();
            
            var path = GetPath();
            var firstLaunch = !File.Exists(path);

            if(password == "")
                password = null;

            var connStr = new SQLiteConnectionString(path, Flags, true, password, null, conn =>
            {
                // TODO: update to SQLCipher 4 encryption
                // Performance issue: https://github.com/praeclarum/sqlite-net/issues/978
                if(password != null)
                    conn.ExecuteScalar<string>("PRAGMA cipher_compatibility = 3");
            });
            
            _connection = new SQLiteAsyncConnection(connStr);

            try
            {
                if(firstLaunch)
                    await _connection.EnableWriteAheadLoggingAsync();
                
                await _connection.CreateTableAsync<Authenticator>();
                await _connection.CreateTableAsync<Category>();
                await _connection.CreateTableAsync<AuthenticatorCategory>();
                await _connection.CreateTableAsync<CustomIcon>();
            }
            catch
            {
                Lock.Release();
                await Close();
                throw;
            }

#if DEBUG
            _connection.Trace = true;
            _connection.Tracer = Logger.Info;
            _connection.TimeExecution = true;
#endif

            Lock.Release();
            return _connection;
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
                conn = await GetConnection();
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
                }
                
                try
                {
                    await Close();
                    DeleteDatabase();
                    File.Move(tempPath, dbPath);
                    conn = await Open(newPassword);
                }
                catch
                {
                    // Perhaps it wasn't moved correctly
                    File.Delete(tempPath);
                    RestoreBackup();
                    await Open(currentPassword);
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
                    await conn.ExecuteAsync($"PRAGMA rekey = {quoted}");

                    await Close();
                    conn = await Open(newPassword);
                }
                catch
                {
                    RestoreBackup();
                    await Open(currentPassword);
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