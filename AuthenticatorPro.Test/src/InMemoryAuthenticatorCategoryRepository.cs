using AuthenticatorPro.Shared.Entity;
using AuthenticatorPro.Shared.Persistence;
using AuthenticatorPro.Shared.Persistence.Exception;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthenticatorPro.Test
{
    public class InMemoryAuthenticatorCategoryRepository : IAuthenticatorCategoryRepository
    {
        private readonly List<AuthenticatorCategory> _authenticatorCategories = new List<AuthenticatorCategory>();

        public Task CreateAsync(AuthenticatorCategory item)
        {
            if (_authenticatorCategories.Any(a =>
                a.AuthenticatorSecret == item.AuthenticatorSecret && a.CategoryId == item.CategoryId))
            {
                throw new EntityDuplicateException();
            }

            _authenticatorCategories.Add(item.Clone());
            return Task.CompletedTask;
        }

        public Task<AuthenticatorCategory> GetAsync((string, string) id)
        {
            var (authenticatorSecret, categoryId) = id;
            return Task.FromResult(_authenticatorCategories.SingleOrDefault(a =>
                a.AuthenticatorSecret == authenticatorSecret && a.CategoryId == categoryId));
        }

        public Task<List<AuthenticatorCategory>> GetAllAsync()
        {
            return Task.FromResult(_authenticatorCategories);
        }

        public Task UpdateAsync(AuthenticatorCategory item)
        {
            var index = _authenticatorCategories.FindIndex(a =>
                a.AuthenticatorSecret == item.AuthenticatorSecret && a.CategoryId == item.CategoryId);

            if (index >= 0)
            {
                _authenticatorCategories[index] = item.Clone();
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(AuthenticatorCategory item)
        {
            var index = _authenticatorCategories.FindIndex(a =>
                a.AuthenticatorSecret == item.AuthenticatorSecret && a.CategoryId == item.CategoryId);

            if (index >= 0)
            {
                _authenticatorCategories.RemoveAt(index);
            }

            return Task.CompletedTask;
        }

        public Task<List<AuthenticatorCategory>> GetAllForAuthenticatorAsync(Authenticator authenticator)
        {
            return Task.FromResult(_authenticatorCategories.Where(a => a.AuthenticatorSecret == authenticator.Secret)
                .ToList());
        }

        public Task<List<AuthenticatorCategory>> GetAllForCategoryAsync(Category category)
        {
            return Task.FromResult(_authenticatorCategories.Where(a => a.CategoryId == category.Id).ToList());
        }

        public Task DeleteAllForAuthenticatorAsync(Authenticator authenticator)
        {
            foreach (var ac in _authenticatorCategories.Where(a => a.AuthenticatorSecret == authenticator.Secret))
            {
                var index = _authenticatorCategories.FindIndex(a =>
                    a.AuthenticatorSecret == ac.AuthenticatorSecret &&
                    a.CategoryId == ac.CategoryId);

                _authenticatorCategories.RemoveAt(index);
            }

            return Task.CompletedTask;
        }

        public Task DeleteAllForCategoryAsync(Category category)
        {
            foreach (var ac in _authenticatorCategories.Where(a => a.CategoryId == category.Id))
            {
                var index = _authenticatorCategories.FindIndex(a =>
                    a.AuthenticatorSecret == ac.AuthenticatorSecret &&
                    a.CategoryId == ac.CategoryId);

                _authenticatorCategories.RemoveAt(index);
            }

            return Task.CompletedTask;
        }

        public Task TransferAsync(Category initial, Category next)
        {
            foreach (var ac in _authenticatorCategories.Where(a => a.CategoryId == initial.Id))
            {
                ac.CategoryId = next.Id;
            }

            return Task.CompletedTask;
        }
    }
}