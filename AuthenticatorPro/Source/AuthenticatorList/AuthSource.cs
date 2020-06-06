using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthenticatorPro.Data;
using AuthenticatorPro.Shared;
using OtpNet;
using SQLite;

namespace AuthenticatorPro.AuthenticatorList
{
    internal class AuthSource
    {
        private readonly SQLiteAsyncConnection _connection;

        private List<Authenticator> _all;

        private string _search;

        public List<IAuthenticatorInfo> Authenticators { get; private set; }

        public Task UpdateTask { get; }
        public string CategoryId { get; private set; }
        public List<AuthenticatorCategory> CategoryBindings { get; private set; }


        public AuthSource(SQLiteAsyncConnection connection)
        {
            _search = "";
            CategoryId = null;
            _connection = connection;

            Authenticators = new List<IAuthenticatorInfo>();
            _all = new List<Authenticator>();
            CategoryBindings = new List<AuthenticatorCategory>();

            UpdateTask = UpdateSource();
        }

        public void SetSearch(string query)
        {
            _search = query;
            _search = _search.ToLower();

            var results =
                _all.Where(i => i.Issuer.ToLower().Contains(_search)).ToList();

            if(CategoryId != null)
            {
                var authsInCategory =
                    CategoryBindings.Where(b => b.CategoryId == CategoryId).ToList();

                results =
                    results.Where(a => authsInCategory.Count(b => b.AuthenticatorSecret == a.Secret) == 1).ToList();
            }

            Authenticators = results.Cast<IAuthenticatorInfo>().ToList();
        }

        public void SetCategory(string categoryId)
        {
            CategoryId = categoryId;
            UpdateView();
        }

        public void UpdateView()
        {
            List<Authenticator> results;

            if(CategoryId == null)
            {
                results =
                    _all.OrderBy(a => a.Ranking)
                        .ToList();
            }
            else
            {
                var authsInCategory =
                    CategoryBindings.Where(b => b.CategoryId == CategoryId).ToList();

                results =
                    _all.Where(a => authsInCategory.Count(b => b.AuthenticatorSecret == a.Secret) == 1)
                        .OrderBy(a => authsInCategory.First(c => c.AuthenticatorSecret == a.Secret).Ranking)
                        .ToList();
            }

            Authenticators = results.Cast<IAuthenticatorInfo>().ToList();
        }

        public async Task UpdateSource()
        {
            _all.Clear();
            CategoryBindings.Clear();

            var sql = @"SELECT * FROM authenticator ORDER BY ranking, issuer, username ASC";
            _all = await _connection.QueryAsync<Authenticator>(sql);

            sql = @"SELECT * FROM authenticatorcategory ORDER BY ranking ASC";
            CategoryBindings = await _connection.QueryAsync<AuthenticatorCategory>(sql);

            if(CategoryId == null)
                Authenticators = _all.Cast<IAuthenticatorInfo>().ToList();
            else
                UpdateView();
        }

        private Authenticator GetAuthenticator(IAuthenticatorInfo info)
        {
            return _all.Find(i => i.Secret == info.Secret);
        }

        public Authenticator Get(int position)
        {
            if(Authenticators.ElementAtOrDefault(position) == null) return null;

            var info = Authenticators[position];
            var auth = GetAuthenticator(info);

            if(auth.Type == AuthenticatorType.Totp && auth.TimeRenew <= DateTime.Now)
            {
                var secret = Base32Encoding.ToBytes(auth.Secret);
                var totp = new Totp(secret, auth.Period, auth.Algorithm, auth.Digits);
                auth.Code = totp.ComputeTotp();
                auth.TimeRenew = DateTime.Now.AddSeconds(totp.RemainingSeconds());
            }

            return auth;
        }

        public int GetPosition(string secret)
        {
            return _all.FindIndex(a => a.Secret == secret);
        }

        public async Task Rename(int position, string issuer, string username)
        {
            if(Authenticators.ElementAtOrDefault(position) == null) return;

            var info = Authenticators[position];
            var auth = GetAuthenticator(info);

            auth.Issuer = issuer.Trim().Truncate(32);
            auth.Username = username.Trim().Truncate(32);

            await _connection.UpdateAsync(auth);
        }

        public async Task Delete(int position)
        {
            if(Authenticators.ElementAtOrDefault(position) == null) return;

            var info = Authenticators[position];
            var auth = GetAuthenticator(info);

            await _connection.DeleteAsync<Authenticator>(auth.Secret);
            Authenticators.Remove(info);
            _all.Remove(auth);

            var sql = "DELETE FROM authenticatorcategory WHERE authenticatorSecret = ?";
            object[] args = {auth.Secret};
            await _connection.ExecuteAsync(sql, args);
        }

        public async Task Move(int oldPosition, int newPosition)
        {
            var old = Authenticators[newPosition];
            Authenticators[newPosition] = Authenticators[oldPosition];
            Authenticators[oldPosition] = old;

            for(var i = 0; i < Authenticators.Count; ++i)
                if(CategoryId == null)
                {
                    var auth = GetAuthenticator(Authenticators[i]);
                    auth.Ranking = i;
                    await _connection.UpdateAsync(auth);
                }
                else
                {
                    var binding =
                        GetAuthenticatorCategory(Authenticators[i]);
                    binding.Ranking = i;
                    await _connection.UpdateAsync(binding);
                }
        }

        public async Task IncrementHotp(int position)
        {
            if(Authenticators.ElementAtOrDefault(position) == null) return;

            var info = Authenticators[position];
            var auth = GetAuthenticator(info);

            if(auth.Type != AuthenticatorType.Hotp) return;

            var secret = Base32Encoding.ToBytes(auth.Secret);
            var hotp = new Hotp(secret, auth.Algorithm);

            auth.Counter++;
            auth.Code = hotp.ComputeHOTP(auth.Counter);
            auth.TimeRenew = DateTime.Now.AddSeconds(10);

            Authenticators[position] = auth;
            await _connection.UpdateAsync(auth);
        }

        public bool IsDuplicate(Authenticator auth)
        {
            return _all.Any(iterator => auth.Secret == iterator.Secret);
        }

        public bool IsDuplicateCategoryBinding(AuthenticatorCategory binding)
        {
            return CategoryBindings.Any(
                iterator => binding.AuthenticatorSecret == iterator.AuthenticatorSecret &&
                         binding.CategoryId == iterator.CategoryId);
        }

        public int Count()
        {
            return Authenticators.Count;
        }

        public List<string> GetCategories(int position)
        {
            var ids = new List<string>();
            var secret = Authenticators[position].Secret;

            var authCategories =
                CategoryBindings.Where(b => b.AuthenticatorSecret == secret).ToList();

            foreach(var binding in authCategories) ids.Add(binding.CategoryId);

            return ids;
        }

        public AuthenticatorCategory GetAuthenticatorCategory(IAuthenticatorInfo info)
        {
            return CategoryBindings.First(b => b.AuthenticatorSecret == info.Secret && b.CategoryId == CategoryId);
        }

        public void AddToCategory(int position, string categoryId)
        {
            var sql = "INSERT INTO authenticatorcategory (categoryId, authenticatorSecret)" +
                      "VALUES (?, ?)";
            var secret = Authenticators[position].Secret;
            object[] args = {categoryId, secret};
            _connection.ExecuteAsync(sql, args);

            CategoryBindings.Add(new AuthenticatorCategory(categoryId, secret));
        }

        public void RemoveFromCategory(int position, string categoryId)
        {
            const string sql = "DELETE FROM authenticatorcategory WHERE categoryId = ? AND authenticatorSecret = ?";
            var secret = Authenticators[position].Secret;
            object[] args = {categoryId, secret};
            _connection.ExecuteAsync(sql, args);

            var binding =
                CategoryBindings.Find(b => b.CategoryId == categoryId && b.AuthenticatorSecret == secret);
            CategoryBindings.Remove(binding);
        }
    }
}