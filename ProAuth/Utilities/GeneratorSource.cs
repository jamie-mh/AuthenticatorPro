using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Albireo.Base32;
using OtpSharp;
using ProAuth.Data;
using SQLite;

namespace ProAuth.Utilities
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

            //if(gen.TimeRenew < DateTime.Now)
            //{
            //    byte[] secret = Base32.Decode(gen.Secret);
            //    Totp totp = new Totp(secret, gen.Period, gen.Algorithm, gen.Digits);

            //    gen.Code = totp.ComputeTotp();
            //    gen.TimeRenew = DateTime.Now.AddSeconds(totp.RemainingSeconds());

            //    _connection.Update(gen);
            //}

            return gen;
        }

        public Generator GetNth(int n)
        {
            string sql = $@"SELECT * FROM generator LIMIT 1 OFFSET {n}";
            Generator gen = _connection.Query<Generator>(sql).First();

            if(gen.TimeRenew < DateTime.Now)
            {
                byte[] secret = Base32.Decode(gen.Secret);
                Totp totp = new Totp(secret, gen.Period, gen.Algorithm, gen.Digits);

                gen.Code = totp.ComputeTotp();
                gen.TimeRenew = DateTime.Now.AddSeconds(totp.RemainingSeconds());

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