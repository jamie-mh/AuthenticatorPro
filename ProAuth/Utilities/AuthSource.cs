using System;
using System.Collections.Generic;
using System.Linq;
using Albireo.Base32;
using OtpSharp;
using ProAuth.Data;
using SQLite;

namespace ProAuth.Utilities
{
    internal class AuthSource
    {
        public enum SortType
        {
            Alphabetical, CreatedDate
        };
        public List<Authenticator> Authenticators { get; private set; }

        private string _search;
        private SortType _sort;

        private readonly SQLiteConnection _connection;

        public AuthSource(SQLiteConnection connection)
        {
            _search = "";
            _sort = SortType.Alphabetical;

            _connection = connection;

            Authenticators = new List<Authenticator>();
            Update();
        }

        public void SetSearch(string query)
        {
            _search = query;
            Update();
        }

        public void SetSort(SortType sortType)
        {
            _sort = sortType;
            Update();
        }

        public void Update()
        {
            Authenticators.Clear();

            string sql = $@"SELECT * FROM authenticator ";
            object[] args = { $@"%{_search}%" };

            if(_search.Trim() != "")
            {
                sql += "WHERE issuer LIKE ? ";
            }

            switch(_sort)
            {
                case SortType.Alphabetical:
                    sql += "ORDER BY issuer ASC, username ASC";
                    break;

                case SortType.CreatedDate:
                    sql += "ORDER BY createdDate ASC";
                    break;
            }

            Authenticators = _connection.Query<Authenticator>(sql, args);
        }

        public Authenticator Get(int position)
        {
            if(Authenticators.ElementAtOrDefault(position) == null)
            {
                return null;
            }

            Authenticator auth = Authenticators[position];

            if(auth.Type == OtpType.Totp && auth.TimeRenew <= DateTime.Now)
            {
                byte[] secret = Base32.Decode(auth.Secret);
                Totp totp = new Totp(secret, auth.Period, auth.Algorithm, auth.Digits);
                auth.Code = totp.ComputeTotp();
                auth.TimeRenew = DateTime.Now.AddSeconds(totp.RemainingSeconds());
            }

            return auth;
        }

        public void Rename(int position, string issuer, string username)
        {
            if(Authenticators.ElementAtOrDefault(position) == null)
            {
                return;
            }

            Authenticator item = Authenticators[position];
            item.Issuer = issuer.Trim().Truncate(32);
            item.Username = username.Trim().Truncate(32);
            item.Icon = Icons.FindKeyByName(item.Issuer);

            _connection.Update(item);
        }

        public void Delete(int position)
        {
            if(Authenticators.ElementAtOrDefault(position) == null)
            {
                return;
            }

            Authenticator item = Authenticators[position];

            _connection.Delete<Authenticator>(item.Secret);
            Authenticators.Remove(item);
        }

        public void IncrementHotp(int position)
        {
            if(Authenticators.ElementAtOrDefault(position) == null)
            {
                return;
            }

            Authenticator auth = Authenticators[position];

            if(auth.Type != OtpType.Hotp)
            {
                return;
            }

            byte[] secret = Base32.Decode(auth.Secret);
            Hotp hotp = new Hotp(secret, auth.Algorithm);

            auth.Counter++;
            auth.Code = hotp.ComputeHotp(auth.Counter);
            auth.TimeRenew = DateTime.Now.AddSeconds(10);

            Authenticators[position] = auth;
            _connection.Update(auth);
        }

        public bool IsDuplicate(Authenticator auth)
        {
            foreach(Authenticator iterator in Authenticators)
            {
                if(auth.Secret == iterator.Secret)
                {
                    return true;
                }
            }

            return false;
        }

        public int Count()
        {
            return Authenticators.Count;
        }
    }
}