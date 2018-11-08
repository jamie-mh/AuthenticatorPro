using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Albireo.Base32;
using OtpSharp;
using ProAuth.Data;
using SQLite;

namespace ProAuth.Utilities
{
    internal class AuthSource
    {
        public List<Authenticator> Authenticators { get; private set; }
        public bool IsSearching => _search.Trim() != "";

        private string _search;
        private readonly SQLiteAsyncConnection _connection;

        public AuthSource(SQLiteAsyncConnection connection)
        {
            _search = "";
            _connection = connection;

            Authenticators = new List<Authenticator>();
            Update();
        }

        public void SetSearch(string query)
        {
            _search = query;
            Update();
        }

        public async Task Update()
        {
            Authenticators.Clear();

            string sql = $@"SELECT * FROM authenticator ";
            object[] args = { $@"%{_search}%" };

            if(IsSearching)
            {
                sql += "WHERE issuer LIKE ? ";
            }

            sql += "ORDER BY ranking ASC, issuer ASC, username ASC";
            Authenticators = _connection.QueryAsync<Authenticator>(sql, args).Result;
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

        public async Task Rename(int position, string issuer, string username)
        {
            if(Authenticators.ElementAtOrDefault(position) == null)
            {
                return;
            }

            Authenticator item = Authenticators[position];
            item.Issuer = issuer.Trim().Truncate(32);
            item.Username = username.Trim().Truncate(32);
            item.Icon = Icons.FindServiceKeyByName(item.Issuer);

            _connection.UpdateAsync(item);
        }

        public async Task Delete(int position)
        {
            if(Authenticators.ElementAtOrDefault(position) == null)
            {
                return;
            }

            Authenticator item = Authenticators[position];

            _connection.DeleteAsync<Authenticator>(item.Secret);
            Authenticators.Remove(item);
        }

        public async void Move(int oldPosition, int newPosition)
        {
            Authenticator old = Authenticators[newPosition];
            Authenticators[newPosition] = Authenticators[oldPosition];
            Authenticators[oldPosition] = old;

            if(oldPosition > newPosition)
            {
                for(int i = newPosition; i < Authenticators.Count; ++i)
                {
                    Authenticators[i].Ranking++;
                    _connection.UpdateAsync(Authenticators[i]);
                }
            }
            else
            {
                for(int i = oldPosition; i < newPosition; ++i)
                {
                    Authenticators[i].Ranking--;
                    _connection.UpdateAsync(Authenticators[i]);
                }
            }

            Authenticators[newPosition].Ranking = newPosition;
            _connection.UpdateAsync(Authenticators[newPosition]);
        }

        public async Task IncrementHotp(int position)
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
            _connection.UpdateAsync(auth);
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