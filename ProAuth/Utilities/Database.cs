using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Android.Content;
using Android.Content.Res;
using Newtonsoft.Json;
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
            Connection.CreateTable<Implementation>();

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