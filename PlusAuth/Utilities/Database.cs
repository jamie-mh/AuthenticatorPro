using System.IO;
using PlusAuth.Data;
using SQLite;

namespace PlusAuth.Utilities
{
    internal class Database
    {
        public SQLiteConnection Connection { get; }

        public Database()
        {
            string dbPath = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                "plusauth.db3"
            );

            Connection = new SQLiteConnection(dbPath);
        }

        public void Prepare()
        {
            Connection.CreateTable<Authenticator>();
        }
    }
}