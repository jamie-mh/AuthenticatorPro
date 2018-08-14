using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using ProAuth.Data;
using SQLite;

namespace ProAuth.Utilities
{
    class Database
    {
        public SQLiteConnection Connection { get; }

        public Database()
        {
            string dbPath = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                "proauth.db3"
            );

            Connection = new SQLiteConnection(dbPath);
            Connection.CreateTable<Generator>();

            Generator test = new Generator();
            test.secret = "SP6EH4UE2PVCONIYVPNVOALCRTLSLF7R";
            Connection.Insert(test);
        }
    }
}