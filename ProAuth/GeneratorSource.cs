using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Albireo.Otp;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using ProAuth.Data;
using SQLite;

namespace ProAuth
{
    class GeneratorSource
    {
        private readonly SQLiteConnection _connection;

        public GeneratorSource(SQLiteConnection connection)
        {
            _connection = connection;
        }

        public Generator Get(int id)
        {
            Generator gen = _connection.Get<Generator>(id);

            if(gen.TimeRenew < DateTime.Now)
            {
                gen.TimeRenew = DateTime.Now.AddSeconds(30);
                gen.Code = Totp.GetCode(HashAlgorithm.Sha256, gen.Secret, System.DateTime.Now);
                _connection.Update(gen);
            }

            return gen;
        }

        public int Count()
        {
            return _connection.Table<Generator>().Count();
        }
    }
}