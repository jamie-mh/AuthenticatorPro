using System.IO;
using ProAuth.Data;
using SQLite;

namespace ProAuth.Utilities
{
    internal class Database
    {
        public SQLiteConnection Connection { get; }

        public Database()
        {
            string dbPath = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                ".db3"
            );

            Connection = new SQLiteConnection(dbPath);
        }

        public void Prepare()
        {
            Connection.CreateTable<Authenticator>();
        }
    }
}