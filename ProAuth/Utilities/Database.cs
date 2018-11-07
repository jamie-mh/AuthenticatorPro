using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ProAuth.Data;
using SQLite;
using Environment = System.Environment;

namespace ProAuth.Utilities
{
    internal static class Database
    {
        [DllImport("libProAuthKey", EntryPoint = "get_key")]
        static extern string GetDatabaseKey();

        public static async Task<SQLiteAsyncConnection> Connect()
        {
            string key = GetDatabaseKey();

            string dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                "proauth.db3"
            );

            SQLiteAsyncConnection connection = new SQLiteAsyncConnection(dbPath, true, key);
            await connection.QueryAsync<int>($@"PRAGMA key='{key}'");
            await connection.CreateTableAsync<Authenticator>();

            return connection;
        }
    }
}