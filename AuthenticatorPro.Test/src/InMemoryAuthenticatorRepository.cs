// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Entity;
using AuthenticatorPro.Shared.Persistence;
using AuthenticatorPro.Shared.Persistence.Exception;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthenticatorPro.Test
{
    public class InMemoryAuthenticatorRepository : IAuthenticatorRepository
    {
        private readonly List<Authenticator> _authenticators = new List<Authenticator>();

        public Task CreateAsync(Authenticator item)
        {
            if (_authenticators.Any(a => a.Secret == item.Secret))
            {
                throw new EntityDuplicateException();
            }

            _authenticators.Add(item.Clone());
            return Task.CompletedTask;
        }

        public Task<Authenticator> GetAsync(string id)
        {
            return Task.FromResult(_authenticators.SingleOrDefault(a => a.Secret == id));
        }

        public Task<List<Authenticator>> GetAllAsync()
        {
            return Task.FromResult(_authenticators);
        }

        public Task UpdateAsync(Authenticator item)
        {
            var index = _authenticators.FindIndex(a => a.Secret == item.Secret);

            if (index >= 0)
            {
                _authenticators[index] = item.Clone();
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(Authenticator item)
        {
            var index = _authenticators.FindIndex(a => a.Secret == item.Secret);

            if (index >= 0)
            {
                _authenticators.RemoveAt(index);
            }

            return Task.CompletedTask;
        }
    }
}