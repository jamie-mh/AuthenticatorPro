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
            Connection.CreateTable<Generator>();

            //AssetManager assetManager = context.Assets;
            //string json = new StreamReader(assetManager.Open("implementations.json")).ReadToEnd();
            //List<Implementation> impl = JsonConvert.DeserializeObject<List<Implementation>>(json);

            //Connection.InsertAll(impl);

            //Generator gen = new Generator()
            //{
            //    Secret = "7AOCIJYZNAUM57HM",
            //    ImplementationId = 2
            //};
            //Connection.Insert(gen);
        }

        /*
         *  Service
         */

        /*
         *  Generator
         */
    }
}