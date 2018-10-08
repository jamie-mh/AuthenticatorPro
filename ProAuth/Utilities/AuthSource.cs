using System;
using System.Collections.Generic;
using System.Linq;
using Albireo.Base32;
using OtpSharp;
using ProAuth.Data;
using SQLite;

namespace ProAuth.Utilities
{
    class AuthSource
    {
        private readonly SQLiteConnection _connection;
        private List<Authenticator> _cache;

        public AuthSource(SQLiteConnection connection)
        {
            _connection = connection;
            _cache = new List<Authenticator>();
        }

        public Authenticator GetNth(int n)
        {
            string sql = $@"SELECT * FROM authenticator LIMIT 1 OFFSET {n}";
            Authenticator auth;

            if(_cache.Count <= n || _cache[n] == null)
            {
                auth = _connection.Query<Authenticator>(sql).First();
                _cache.Insert(n, auth);
            }
            else
            {
                auth = _cache[n];
            }

            if(auth.TimeRenew < DateTime.Now)
            {
                auth = _connection.Query<Authenticator>(sql).First();
                byte[] secret = Base32.Decode(auth.Secret);
                Totp totp = new Totp(secret, auth.Period, auth.Algorithm, auth.Digits);

                auth.Code = totp.ComputeTotp();
                auth.TimeRenew = DateTime.Now.AddSeconds(totp.RemainingSeconds());

                _connection.Update(auth);
                _cache[n] = auth;
            }

            return auth;
        }

        public void ClearCache()
        {
            _cache.Clear();
        }

        public void DeleteNth(int n)
        {
            string sql = $@"SELECT * FROM authenticator LIMIT 1 OFFSET {n}";
            Authenticator auth = _connection.Query<Authenticator>(sql).First();
            _cache = new List<Authenticator>();
            _connection.Delete<Authenticator>(auth.Id);
        }

        public int Count()
        {
            return _connection.Table<Authenticator>().Count();
        }
    }
}