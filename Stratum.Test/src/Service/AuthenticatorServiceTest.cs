// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stratum.Core;
using Stratum.Core.Entity;
using Stratum.Core.Persistence;
using Stratum.Core.Persistence.Exception;
using Stratum.Core.Service;
using Stratum.Core.Service.Impl;
using Moq;
using Xunit;

namespace Stratum.Test.Service
{
    public class AuthenticatorServiceTest
    {
        private readonly Mock<IAuthenticatorRepository> _authenticatorRepository;
        private readonly Mock<IAuthenticatorCategoryRepository> _authenticatorCategoryRepository;
        private readonly Mock<ICustomIconService> _customIconService;
        private readonly Mock<IEqualityComparer<Authenticator>> _equalityComparer;

        private readonly IAuthenticatorService _authenticatorService;

        public AuthenticatorServiceTest()
        {
            _authenticatorRepository = new Mock<IAuthenticatorRepository>();
            _authenticatorCategoryRepository = new Mock<IAuthenticatorCategoryRepository>();
            _customIconService = new Mock<ICustomIconService>();
            _equalityComparer = new Mock<IEqualityComparer<Authenticator>>();

            _authenticatorService = new AuthenticatorService(
                _authenticatorRepository.Object,
                _authenticatorCategoryRepository.Object,
                _customIconService.Object,
                _equalityComparer.Object);
        }

        [Fact]
        public async Task AddAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _authenticatorService.AddAsync(null));
        }

        [Fact]
        public async Task AddAsync_ok()
        {
            var auth = new Mock<Authenticator>();
            auth.Setup(a => a.Validate()).Verifiable();

            await _authenticatorService.AddAsync(auth.Object);

            auth.Verify(a => a.Validate());
            _authenticatorRepository.Verify(r => r.CreateAsync(auth.Object));
        }

        [Fact]
        public async Task UpdateAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _authenticatorService.UpdateAsync(null));
        }

        [Fact]
        public async Task UpdateAsync_ok()
        {
            var auth = new Mock<Authenticator>();
            auth.Setup(a => a.Validate()).Verifiable();

            await _authenticatorService.UpdateAsync(auth.Object);

            auth.Verify(a => a.Validate());
            _authenticatorRepository.Verify(r => r.UpdateAsync(auth.Object));
        }

        [Fact]
        public async Task UpdateManyAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _authenticatorService.UpdateManyAsync(null));
        }

        [Fact]
        public async Task UpdateManyAsync_doesntExist()
        {
            var auth = new Mock<Authenticator>();
            auth.Setup(a => a.Validate()).Verifiable();

            _authenticatorRepository.Setup(r => r.GetAsync(It.IsAny<string>())).ReturnsAsync((Authenticator) null);

            var updated = await _authenticatorService.UpdateManyAsync(new List<Authenticator> { auth.Object });

            Assert.Equal(0, updated);

            auth.Verify(a => a.Validate());
            _authenticatorRepository.Verify(r => r.UpdateAsync(auth.Object), Times.Never());
        }

        [Fact]
        public async Task UpdateManyAsync_identical()
        {
            var auth = new Mock<Authenticator>();
            auth.Setup(a => a.Validate()).Verifiable();

            _authenticatorRepository.Setup(r => r.GetAsync(It.IsAny<string>())).ReturnsAsync(auth.Object);
            _equalityComparer.Setup(c => c.Equals(It.IsAny<Authenticator>(), It.IsAny<Authenticator>())).Returns(true);

            var updated = await _authenticatorService.UpdateManyAsync(new List<Authenticator> { auth.Object });

            Assert.Equal(0, updated);

            auth.Verify(a => a.Validate());
            _authenticatorRepository.Verify(r => r.UpdateAsync(auth.Object), Times.Never());
        }

        [Fact]
        public async Task UpdateManyAsync_ok()
        {
            var auth = new Mock<Authenticator>();
            auth.Setup(a => a.Validate()).Verifiable();

            _authenticatorRepository.Setup(r => r.GetAsync(It.IsAny<string>())).ReturnsAsync(auth.Object);
            _equalityComparer.Setup(c => c.Equals(It.IsAny<Authenticator>(), It.IsAny<Authenticator>())).Returns(false);

            var updated = await _authenticatorService.UpdateManyAsync(new List<Authenticator> { auth.Object });

            Assert.Equal(1, updated);

            auth.Verify(a => a.Validate());
            _authenticatorRepository.Verify(r => r.UpdateAsync(auth.Object));
        }

        [Fact]
        public async Task ChangeSecretAsync_nullAuth()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _authenticatorService.ChangeSecretAsync(null, "secret"));
        }

        [Fact]
        public async Task ChangeSecretAsync_nullOrEmptyNewSecret()
        {
            var auth = new Authenticator();
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _authenticatorService.ChangeSecretAsync(auth, null));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _authenticatorService.ChangeSecretAsync(auth, ""));
        }

        [Fact]
        public async Task ChangeSecretAsync_ok()
        {
            var transferInitial = new CaptureMatch<Authenticator>(a => { Assert.Equal("old", a.Secret); });
            var transferNext = new CaptureMatch<Authenticator>(a => { Assert.Equal("new", a.Secret); });

            _authenticatorCategoryRepository
                .Setup(r => r.TransferAuthenticatorAsync(Capture.With(transferInitial), Capture.With(transferNext)))
                .Verifiable();

            var oldSecretMatch = new CaptureMatch<string>(s => { Assert.Equal("old", s); });
            var newSecretMatch = new CaptureMatch<string>(s => { Assert.Equal("new", s); });

            _authenticatorRepository
                .Setup(r => r.ChangeSecretAsync(Capture.With(oldSecretMatch), Capture.With(newSecretMatch)))
                .Verifiable();

            var auth = new Authenticator { Secret = "old" };

            await _authenticatorService.ChangeSecretAsync(auth, "new");

            _authenticatorCategoryRepository
                .Verify(r => r.TransferAuthenticatorAsync(It.IsAny<Authenticator>(), It.IsAny<Authenticator>()),
                    Times.Once());

            _authenticatorRepository
                .Verify(r => r.ChangeSecretAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }

        [Fact]
        public async Task SetIconAsync_nullAuth()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _authenticatorService.SetIconAsync(null, "icon"));
        }

        [Fact]
        public async Task SetIconAsync_nullEmptyIcon()
        {
            var auth = new Authenticator();
            await Assert.ThrowsAsync<ArgumentException>(() => _authenticatorService.SetIconAsync(auth, null));
            await Assert.ThrowsAsync<ArgumentException>(() => _authenticatorService.SetIconAsync(auth, ""));
        }

        [Fact]
        public async Task SetIconAsync_ok()
        {
            var match = new CaptureMatch<Authenticator>(a => { Assert.Equal("icon", a.Icon); });

            _authenticatorRepository.Setup(r => r.UpdateAsync(Capture.With(match)));

            var auth = new Authenticator();
            await _authenticatorService.SetIconAsync(auth, "icon");

            _authenticatorRepository.Verify(r => r.UpdateAsync(auth));
            _customIconService.Verify(s => s.CullUnusedAsync());
        }

        [Fact]
        public async Task SetCustomIconAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _authenticatorService.SetCustomIconAsync(null, new CustomIcon()));
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _authenticatorService.SetCustomIconAsync(new Authenticator(), null));
        }

        [Fact]
        public async Task SetCustomIconAsync_alreadySet()
        {
            var auth = new Authenticator { Icon = $"{CustomIcon.Prefix}test" };
            var icon = new CustomIcon { Id = "test" };

            await _authenticatorService.SetCustomIconAsync(auth, icon);

            _customIconService.Verify(c => c.AddIfNotExistsAsync(icon), Times.Never());
            _customIconService.Verify(c => c.CullUnusedAsync(), Times.Never());
            _authenticatorRepository.Verify(r => r.UpdateAsync(auth), Times.Never());
        }

        [Fact]
        public async Task SetCustomIconAsync_ok()
        {
            var auth = new Authenticator();
            var icon = new CustomIcon { Id = "test" };

            var match = new CaptureMatch<Authenticator>(a =>
            {
                Assert.Equal($"{CustomIcon.Prefix}{icon.Id}", a.Icon);
            });

            _authenticatorRepository.Setup(r => r.UpdateAsync(Capture.With(match)));

            await _authenticatorService.SetCustomIconAsync(auth, icon);

            _customIconService.Verify(s => s.AddIfNotExistsAsync(icon));
            _customIconService.Verify(s => s.CullUnusedAsync());
            _authenticatorRepository.Verify(r => r.UpdateAsync(auth));
        }

        [Fact]
        public async Task AddManyAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _authenticatorService.AddManyAsync(null));
        }

        [Fact]
        public async Task AddManyAsync_exists()
        {
            var auth = new Mock<Authenticator>();
            auth.Setup(a => a.Validate()).Verifiable();

            _authenticatorRepository.Setup(r => r.CreateAsync(auth.Object)).ThrowsAsync(new EntityDuplicateException());

            var added = await _authenticatorService.AddManyAsync(new List<Authenticator> { auth.Object });

            Assert.Equal(0, added);
            auth.Verify(a => a.Validate());
        }

        [Fact]
        public async Task AddManyAsync_ok()
        {
            var auth = new Mock<Authenticator>();
            auth.Setup(a => a.Validate()).Verifiable();

            _authenticatorRepository.Setup(r => r.CreateAsync(auth.Object)).Verifiable();

            var added = await _authenticatorService.AddManyAsync(new List<Authenticator> { auth.Object });

            Assert.Equal(1, added);
            auth.Verify(a => a.Validate());
            _authenticatorRepository.Verify(r => r.CreateAsync(auth.Object));
        }

        [Fact]
        public async Task AddOrUpdateManyAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _authenticatorService.AddOrUpdateManyAsync(null));
        }

        [Fact]
        public async Task AddOrUpdateManyAsync_ok()
        {
            var auth = new Mock<Authenticator>();
            auth.Setup(a => a.Validate()).Verifiable();

            _authenticatorRepository.Setup(r => r.CreateAsync(auth.Object)).Verifiable();
            _authenticatorRepository.Setup(r => r.GetAsync(It.IsAny<string>())).ReturnsAsync(auth.Object);
            _equalityComparer.Setup(c => c.Equals(It.IsAny<Authenticator>(), It.IsAny<Authenticator>())).Returns(false);

            var (added, updated) =
                await _authenticatorService.AddOrUpdateManyAsync(new List<Authenticator> { auth.Object });

            Assert.Equal(1, added);
            Assert.Equal(1, updated);
        }

        [Fact]
        public async Task DeleteWithCategoryBindingsAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _authenticatorService.DeleteWithCategoryBindingsAsync(null));
        }

        [Fact]
        public async Task DeleteWithCategoryBindingsAsync_ok()
        {
            var auth = new Authenticator();
            await _authenticatorService.DeleteWithCategoryBindingsAsync(auth);

            _authenticatorRepository.Verify(r => r.DeleteAsync(auth));
            _authenticatorCategoryRepository.Verify(r => r.DeleteAllForAuthenticatorAsync(auth));
        }

        [Fact]
        public async Task IncrementCounterAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _authenticatorService.IncrementCounterAsync(null));
        }

        [Fact]
        public async Task IncrementCounterAsync_notHotp()
        {
            var auth = new Authenticator { Type = AuthenticatorType.Totp };
            await Assert.ThrowsAsync<ArgumentException>(() => _authenticatorService.IncrementCounterAsync(auth));
        }

        [Fact]
        public async Task IncrementCounterAsync_ok()
        {
            var match = new CaptureMatch<Authenticator>(a => { Assert.Equal(2, a.Counter); });

            _authenticatorRepository.Setup(r => r.UpdateAsync(Capture.With(match)));

            var auth = new Authenticator { Type = AuthenticatorType.Hotp, Counter = 1 };
            await _authenticatorService.IncrementCounterAsync(auth);

            _authenticatorRepository.Verify(r => r.UpdateAsync(auth));
        }

        [Fact]
        public async Task IncrementCopyCountAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _authenticatorService.IncrementCopyCountAsync(null));
        }

        [Fact]
        public async Task IncrementCopyCountAsync_ok()
        {
            var match = new CaptureMatch<Authenticator>(a => { Assert.Equal(2, a.CopyCount); });

            _authenticatorRepository.Setup(r => r.UpdateAsync(Capture.With(match)));

            var auth = new Authenticator { CopyCount = 1 };
            await _authenticatorService.IncrementCopyCountAsync(auth);

            _authenticatorRepository.Verify(r => r.UpdateAsync(auth));
        }

        [Fact]
        public async Task ResetCopyCountsAsync()
        {
            var authA = new Authenticator { CopyCount = 10 };
            var authB = new Authenticator { CopyCount = 100 };

            _authenticatorRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([authA, authB]);

            var match = new CaptureMatch<Authenticator>(a => { Assert.Equal(0, a.CopyCount); });

            _authenticatorRepository.Setup(r => r.UpdateAsync(Capture.With(match)));
            await _authenticatorService.ResetCopyCountsAsync();

            _authenticatorRepository.Verify(r => r.UpdateAsync(authA));
            _authenticatorRepository.Verify(r => r.UpdateAsync(authB));
        }
    }
}