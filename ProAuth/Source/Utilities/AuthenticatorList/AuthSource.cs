using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Albireo.Base32;
using OtpSharp;
using ProAuth.Data;
using SQLite;

namespace ProAuth.Utilities.AuthenticatorList
{
    internal class AuthSource
    {
        public List<IAuthenticatorInfo> Authenticators { get; private set; }
        public Task UpdateTask { get; }
        public string CategoryId { get; private set; }

        private readonly SQLiteAsyncConnection _connection;

        private List<Data.Authenticator> _all;
        public List<AuthenticatorCategory> CategoryBindings { get; private set; }

        private string _search;

        public AuthSource(SQLiteAsyncConnection connection)
        {
            _search = "";
            CategoryId = null;
            _connection = connection;

            Authenticators = new List<IAuthenticatorInfo>();
            _all = new List<Data.Authenticator>();
            CategoryBindings = new List<AuthenticatorCategory>();

            UpdateTask = Update();
        }

        public void SetSearch(string query)
        {
            _search = query;
            _search = _search.ToLower();

            List<Data.Authenticator> results = 
                _all.Where(i => i.Issuer.ToLower().Contains(_search)).ToList();

            if(CategoryId != null)
            {
                List<AuthenticatorCategory> authsInCategory = 
                    CategoryBindings.Where(b => b.CategoryId == CategoryId).ToList();

                results =
                    results.Where(a => authsInCategory.Count(b => b.AuthenticatorSecret == a.Secret) == 1).ToList();
            }

            Authenticators = results.Cast<IAuthenticatorInfo>().ToList();
        }

        public void SetCategory(string categoryId)
        {
            CategoryId = categoryId;
            List<Data.Authenticator> results;

            if(CategoryId == null)
            {
                results = 
                    _all.OrderBy(a => a.Ranking)
                    .ToList();
            }
            else
            {
                List<AuthenticatorCategory> authsInCategory = 
                    CategoryBindings.Where(b => b.CategoryId == categoryId).ToList();

                results =
                    _all.Where(a => authsInCategory.Count(b => b.AuthenticatorSecret == a.Secret) == 1)
                        .OrderBy(a => authsInCategory.First(c => c.AuthenticatorSecret == a.Secret).Ranking)
                        .ToList();
            }

            Authenticators = results.Cast<IAuthenticatorInfo>().ToList();
        }

        public async Task Update()
        {
            _all.Clear();
            CategoryBindings.Clear();

            string sql = $@"SELECT * FROM authenticator ORDER BY ranking ASC";
            _all = await _connection.QueryAsync<Data.Authenticator>(sql);

            sql = $@"SELECT * FROM authenticatorcategory ORDER BY ranking ASC";
            CategoryBindings = await _connection.QueryAsync<AuthenticatorCategory>(sql);

            if(CategoryId == null)
            {
                Authenticators = _all.Cast<IAuthenticatorInfo>().ToList();
            }
            else
            {
                SetCategory(CategoryId);
            }
        }

        private Data.Authenticator GetAuthenticator(IAuthenticatorInfo info)
        {
            return _all.Find(i => i.Secret == info.Secret);
        }

        public Data.Authenticator Get(int position)
        {
            if(Authenticators.ElementAtOrDefault(position) == null)
            {
                return null;
            }

            IAuthenticatorInfo info = Authenticators[position];
            Data.Authenticator auth = GetAuthenticator(info);

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

            IAuthenticatorInfo info = Authenticators[position];
            Data.Authenticator auth = GetAuthenticator(info);

            auth.Issuer = issuer.Trim().Truncate(32);
            auth.Username = username.Trim().Truncate(32);
            auth.Icon = Icons.FindServiceKeyByName(auth.Issuer);

            _connection.UpdateAsync(auth);
        }

        public async Task Delete(int position)
        {
            if(Authenticators.ElementAtOrDefault(position) == null)
            {
                return;
            }

            IAuthenticatorInfo info = Authenticators[position];
            Data.Authenticator auth = GetAuthenticator(info);

            _connection.DeleteAsync<Data.Authenticator>(auth.Secret);
            Authenticators.Remove(info);
            _all.Remove(auth);

            string sql = "DELETE FROM authenticatorcategory WHERE authenticatorSecret = ?";
            object[] args = {auth.Secret};
            _connection.ExecuteAsync(sql, args);
        }

        public async void Move(int oldPosition, int newPosition)
        {
            IAuthenticatorInfo old = Authenticators[newPosition];
            Authenticators[newPosition] = Authenticators[oldPosition];
            Authenticators[oldPosition] = old;

            if(oldPosition > newPosition)
            {
                for(int i = newPosition; i < Authenticators.Count; ++i)
                {
                    if(CategoryId == null)
                    {
                        Data.Authenticator auth = GetAuthenticator(Authenticators[i]);
                        auth.Ranking++;
                        _connection.UpdateAsync(auth);
                    }
                    else
                    {
                        AuthenticatorCategory binding = 
                            GetAuthenticatorCategory(Authenticators[i]);
                        binding.Ranking++;
                        _connection.UpdateAsync(binding);
                    }
                }
            }
            else
            {
                for(int i = oldPosition; i < newPosition; ++i)
                {
                    if(CategoryId == null)
                    {
                        Data.Authenticator auth = GetAuthenticator(Authenticators[i]);
                        auth.Ranking--;
                        _connection.UpdateAsync(auth);
                    }
                    else
                    {
                        AuthenticatorCategory binding = 
                            GetAuthenticatorCategory(Authenticators[i]);
                        binding.Ranking--;
                        _connection.UpdateAsync(binding);
                    }
                }
            }

            if(CategoryId == null)
            {
                Data.Authenticator temp = GetAuthenticator(Authenticators[newPosition]); 
                temp.Ranking = newPosition;
                _connection.UpdateAsync(temp);
            }
            else
            {
                AuthenticatorCategory temp = GetAuthenticatorCategory(Authenticators[newPosition]);
                temp.Ranking = newPosition;
                _connection.UpdateAsync(temp);
            }
        }

        public async Task IncrementHotp(int position)
        {
            if(Authenticators.ElementAtOrDefault(position) == null)
            {
                return;
            }

            IAuthenticatorInfo info = Authenticators[position];
            Data.Authenticator auth = GetAuthenticator(info);

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

        public bool IsDuplicate(Data.Authenticator auth)
        {
            foreach(Data.Authenticator iterator in _all)
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

        public List<string> GetCategories(int position)
        {
            List<string> ids = new List<string>();
            string secret = Authenticators[position].Secret;

            List<AuthenticatorCategory> authCategories = 
                CategoryBindings.Where(b => b.AuthenticatorSecret == secret).ToList();

            foreach(AuthenticatorCategory binding in authCategories)
            {
                ids.Add(binding.CategoryId);
            }

            return ids;
        }

        public AuthenticatorCategory GetAuthenticatorCategory(IAuthenticatorInfo info)
        {
            return CategoryBindings.First(b => b.AuthenticatorSecret == info.Secret && b.CategoryId == CategoryId);
        }

        public void AddToCategory(int position, string categoryId)
        {
            string sql = "INSERT INTO authenticatorcategory (categoryId, authenticatorSecret)" +
                         "VALUES (?, ?)";
            string secret = Authenticators[position].Secret;
            object[] args = {categoryId, secret};
            _connection.ExecuteAsync(sql, args);

            CategoryBindings.Add(new AuthenticatorCategory(categoryId, secret));
        }

        public void RemoveFromCategory(int position, string categoryId)
        {
            string sql = "DELETE FROM authenticatorcategory WHERE categoryId = ? AND authenticatorSecret = ?";
            string secret = Authenticators[position].Secret;
            object[] args = {categoryId, secret};
            _connection.ExecuteAsync(sql, args);

            AuthenticatorCategory binding =
                CategoryBindings.Find(b => b.CategoryId == categoryId && b.AuthenticatorSecret == secret);
            CategoryBindings.Remove(binding);
        }
    }
}