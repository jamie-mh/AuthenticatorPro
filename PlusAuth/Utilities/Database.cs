using System.IO;
using Android.Content;
using PlusAuth.Data;
using SQLite;

namespace PlusAuth.Utilities
{
    internal class Database
    {
        public SQLiteConnection Connection { get; }

        public Database(Context context)
        {
            string dbPath = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                "proauth.db3"
            );

            Connection = new SQLiteConnection(dbPath, true);
        }

        public void Prepare()
        {
            Connection.CreateTable<Authenticator>();
        }
    }
}