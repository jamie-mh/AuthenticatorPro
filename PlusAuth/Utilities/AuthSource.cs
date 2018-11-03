using System;
using System.Collections.Generic;
using System.Linq;
using Albireo.Base32;
using OtpSharp;
using PlusAuth.Data;
using SQLite;

namespace PlusAuth.Utilities
{
    internal class AuthSource
    {
        public enum SortType
        {
            Alphabetical, CreatedDate
        };

        private string _search;
        private SortType _sort;

        private readonly SQLiteConnection _connection;
        private List<Authenticator> _authenticators;

        public AuthSource(SQLiteConnection connection)
        {
            _search = "";
            _sort = SortType.Alphabetical;

            _connection = connection;

            _authenticators = new List<Authenticator>();
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
            _authenticators.Clear();

            string sql = $@"SELECT * FROM authenticator ";
            object[] args = { $@"%{_search}%" };

            bool searching = _search.Trim() != "";

            if(searching)
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

            _authenticators = _connection.Query<Authenticator>(sql, args);
        }

        public Authenticator Get(int position)
        {
            if(_authenticators.ElementAtOrDefault(position) == null)
            {
                return null;
            }

            Authenticator auth = _authenticators[position];

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
            if(_authenticators.ElementAtOrDefault(position) == null)
            {
                return;
            }

            Authenticator item = _authenticators[position];
            item.Issuer = issuer.Trim().Truncate(32);
            item.Username = username.Trim().Truncate(32);

            _connection.Update(item);
        }

        public void Delete(int position)
        {
            if(_authenticators.ElementAtOrDefault(position) == null)
            {
                return;
            }

            Authenticator item = _authenticators[position];

            _connection.Delete<Authenticator>(item.Secret);
            _authenticators.Remove(item);
        }

        public void IncrementHotp(int position)
        {
            if(_authenticators.ElementAtOrDefault(position) == null)
            {
                return;
            }

            Authenticator auth = _authenticators[position];

            if(auth.Type != OtpType.Hotp)
            {
                return;
            }

            byte[] secret = Base32.Decode(auth.Secret);
            Hotp hotp = new Hotp(secret, auth.Algorithm);

            auth.Counter++;
            auth.Code = hotp.ComputeHotp(auth.Counter);
            auth.TimeRenew = DateTime.Now.AddSeconds(10);

            _authenticators[position] = auth;
            _connection.Update(auth);
        }

        public bool IsDuplicate(Authenticator auth)
        {
            foreach(Authenticator iterator in _authenticators)
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
            return _authenticators.Count;
        }
    }
}