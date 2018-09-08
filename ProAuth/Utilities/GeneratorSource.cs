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
        private List<Generator> _cache;

        public GeneratorSource(SQLiteConnection connection)
        {
            _connection = connection;
            _cache = new List<Generator>();
        }

        public Generator GetNth(int n)
        {
            string sql = $@"SELECT * FROM generator LIMIT 1 OFFSET {n}";
            Generator gen;

            if(_cache.Count <= n || _cache[n] == null)
            {
                gen = _connection.Query<Generator>(sql).First();
                _cache.Insert(n, gen);
            }
            else
            {
                gen = _cache[n];
            }

            if(gen.TimeRenew < DateTime.Now)
            {
                gen = _connection.Query<Generator>(sql).First();
                byte[] secret = Base32.Decode(gen.Secret);
                Totp totp = new Totp(secret, gen.Period, gen.Algorithm, gen.Digits);

                gen.Code = totp.ComputeTotp();
                gen.TimeRenew = DateTime.Now.AddSeconds(totp.RemainingSeconds());

                _connection.Update(gen);
                _cache[n] = gen;
            }

            return gen;
        }

        public void DeleteNth(int n)
        {
            string sql = $@"SELECT * FROM generator LIMIT 1 OFFSET {n}";
            Generator gen = _connection.Query<Generator>(sql).First();
            _cache = new List<Generator>();
            _connection.Delete<Generator>(gen.Id);
        }

        public int Count()
        {
            return _connection.Table<Generator>().Count();
        }
    }
}