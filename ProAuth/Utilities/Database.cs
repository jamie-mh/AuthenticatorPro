using System.IO;
using Android.Content;
using ProAuth.Data;
using SQLite;

namespace ProAuth.Utilities
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
            Connection.CreateTable<Authenticator>();
        }
    }
}