using System;
using System.IO;
using Nito.AsyncEx;
using NUnit.Framework;
using SQLite;

namespace AuthenticatorPro.Test.Test
{
    internal abstract class ConnectionBasedTest
    {
        protected SQLiteAsyncConnection _connection;
        
        [SetUp]
        public void Setup()
        {
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                "test.db3"
            );
            
            try
            {
                File.Delete(dbPath);     
            }
            catch(Exception) { }

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
        }
    }
}