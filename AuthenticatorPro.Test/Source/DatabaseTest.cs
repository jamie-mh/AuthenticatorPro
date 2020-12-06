using System;
using System.IO;
using AuthenticatorPro.Data;
using Nito.AsyncEx;
using NUnit.Framework;
using SQLite;

namespace AuthenticatorPro.Test
{
    internal abstract class DatabaseTest
    {
        protected SQLiteAsyncConnection _connection;
        
        [SetUp]
        public void Setup()
        {
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                "test.db3"
            );
            
            _connection = new SQLiteAsyncConnection(dbPath);

            AsyncContext.Run(async delegate
            {
                await _connection.CreateTableAsync<Authenticator>();
            });
        }

        [TearDown]
        public void Teardown()
        {
            AsyncContext.Run(async delegate
            {
                await _connection.CloseAsync();
            });
            
            File.Delete(_connection.DatabasePath);
            File.Delete(_connection.DatabasePath.Replace("db3", "db3-shm"));
            File.Delete(_connection.DatabasePath.Replace("db3", "db3-wal"));
        }
    }
}