using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Android.Database.Sqlite;
using Android.Runtime;
using ProAuth.Data;
using SQLite;

namespace ProAuth.Utilities
{
    internal class Database
    {
        public SQLiteConnection Connection { get; }

        [DllImport("libProAuthKey", EntryPoint = "get_key")]
        static extern string GetDatabaseKey();

        public Database()
        {
            string dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                "proauth.db3"
            );

            string key = GetDatabaseKey();

            Connection = new SQLiteConnection(dbPath, true, key);
            Connection.Query<int>($@"PRAGMA key='{key}'");
            Connection.CreateTable<Authenticator>();
        }
    }
}