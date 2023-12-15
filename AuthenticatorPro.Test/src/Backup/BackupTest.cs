// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthenticatorPro.Core.Backup.Encryption;
using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Test.Backup.Comparer;
using AuthenticatorPro.Test.Backup.Fixture;
using Newtonsoft.Json;
using Xunit;

namespace AuthenticatorPro.Test.Backup
{
    public class BackupTest : IClassFixture<BackupFixture>
    {
        private readonly BackupComparer _backupComparer;
        private readonly BackupFixture _backupFixture;
        private readonly List<IBackupEncryption> _backupEncryptions;

        public BackupTest(BackupFixture backupFixture)
        {
            _backupComparer = new BackupComparer();
            _backupFixture = backupFixture;
            _backupEncryptions = [new StrongBackupEncryption(), new LegacyBackupEncryption(), new NoBackupEncryption()];
        }

        [Theory]
        [InlineData("t")]
        [InlineData("test")]
        [InlineData("test123")]
        [InlineData("test123!?%")]
        [InlineData("PZqE=_L]Ra;ZD8N&")]
        [InlineData("tUT.3raAGQ[f]]Q@Ft=S}.r(Vk&CM9#`")]
        [InlineData(@"MS^NqdNp&y]tLz_5:P;UU/2LDd_uF7a""x@*a't/Da]'y&b~.=&z3x'r^u{X.@?vv")]
        [InlineData(@"p[{(]2QFSYWcaYdz=;eMtrnZ<bvh8QfW;8v""4HBTtW5H!xMGQKt^\\)f]7.fJ*9dcs@pq(9GKF?7FJ3Qtj$].V!U;:N^/eUj(zG;yC9/pPRGT'DHLR>S5h$}L\\AL~X,pA")]
        [InlineData(@"#n*rF;y3zVPr,B3*qP""C,S[NFk""qswBC;k2*E_gQ&)4Rh.h[f;WG)5w3uY7MGvM/wngR]72BP9;{)_~+Vd""ukx'S]Zt[!cE='43f}J:J_TR<\:`w""{\+`dgdy%vD]Ts:xKAXzgRW!_7vWF]zg*u.)F#ZzzY&[LEHFXH(D@M7Y""'6.e7n~u""[4kyc'TGF28Q""xgNg.!5=;Hfx'e)fx,:#mLmkA(*ty]4]7#;^?*QF4xDK&Fx-)f}(ph=PL*N'#w9`")]
        [InlineData("你好世界")]
        [InlineData("😀 😃 😄 😁 😆 😅 😂 🤣")]
        public async Task EncryptDecrypt(string password)
        {
            foreach (var encryption in _backupEncryptions)
            {
                var encrypted = await encryption.EncryptAsync(_backupFixture.Backup, password);
                var decrypted = await encryption.DecryptAsync(encrypted, password);
                Assert.Equal(_backupFixture.Backup, decrypted, _backupComparer);
            }
        }

        [Fact]
        public async Task LegacyDecrypt_noPassword()
        {
            var encryption = new LegacyBackupEncryption();
            await Assert.ThrowsAsync<ArgumentException>(() => encryption.DecryptAsync(Array.Empty<byte>(), null));
            await Assert.ThrowsAsync<ArgumentException>(() => encryption.DecryptAsync(Array.Empty<byte>(), ""));
        }

        [Fact]
        public async Task LegacyEncrypt_noPassword()
        {
            var encryption = new LegacyBackupEncryption();
            await Assert.ThrowsAsync<ArgumentException>(() => encryption.EncryptAsync(_backupFixture.Backup, null));
            await Assert.ThrowsAsync<ArgumentException>(() => encryption.EncryptAsync(_backupFixture.Backup, ""));
        }

        [Fact]
        public async Task LegacyDecrypt_invalidHeader()
        {
            var encryption = new LegacyBackupEncryption();
            await Assert.ThrowsAsync<ArgumentException>(() => encryption.DecryptAsync("invalid"u8.ToArray(), "test"));
        }

        [Fact]
        public async Task LegacyDecrypt_invalidPassword()
        {
            var encryption = new LegacyBackupEncryption();
            await Assert.ThrowsAsync<ArgumentException>(() =>
                encryption.DecryptAsync(_backupFixture.LegacyData, "testing1"));
        }

        [Fact]
        public async Task LegacyDecrypt_ok()
        {
            var encryption = new LegacyBackupEncryption();
            var backup = await encryption.DecryptAsync(_backupFixture.LegacyData, "test");
            Assert.True(backup.Authenticators.Any());
        }

        [Fact]
        public async Task StrongDecrypt_noPassword()
        {
            var encryption = new StrongBackupEncryption();
            await Assert.ThrowsAsync<ArgumentException>(() => encryption.DecryptAsync(Array.Empty<byte>(), null));
            await Assert.ThrowsAsync<ArgumentException>(() => encryption.DecryptAsync(Array.Empty<byte>(), ""));
        }

        [Fact]
        public async Task StrongEncrypt_noPassword()
        {
            var encryption = new StrongBackupEncryption();
            await Assert.ThrowsAsync<ArgumentException>(() => encryption.EncryptAsync(_backupFixture.Backup, null));
            await Assert.ThrowsAsync<ArgumentException>(() => encryption.EncryptAsync(_backupFixture.Backup, ""));
        }

        [Fact]
        public async Task StrongDecrypt_invalidHeader()
        {
            var encryption = new StrongBackupEncryption();
            await Assert.ThrowsAsync<ArgumentException>(() => encryption.DecryptAsync("invalid"u8.ToArray(), "test"));
        }

        [Fact]
        public async Task StrongDecrypt_invalidPassword()
        {
            var encryption = new StrongBackupEncryption();
            await Assert.ThrowsAsync<ArgumentException>(() =>
                encryption.DecryptAsync(_backupFixture.StrongData, "testing1"));
        }

        [Fact]
        public async Task StrongDecrypt_ok()
        {
            var encryption = new StrongBackupEncryption();
            var backup = await encryption.DecryptAsync(_backupFixture.StrongData, "test");
            Assert.True(backup.Authenticators.Any());
        }

        [Fact]
        public void NoEncrypt_valid()
        {
            var backup = new Core.Backup.Backup { Authenticators = new List<Authenticator>() };
            var json = JsonConvert.SerializeObject(backup);
            var data = Encoding.UTF8.GetBytes(json);

            var encryption = new NoBackupEncryption();
            Assert.True(encryption.CanBeDecrypted(data));
        }

        [Fact]
        public void NoEncrypt_invalid_nullAuthenticators()
        {
            var backup = new Core.Backup.Backup();
            var json = JsonConvert.SerializeObject(backup);
            var data = Encoding.UTF8.GetBytes(json);

            var encryption = new NoBackupEncryption();
            Assert.False(encryption.CanBeDecrypted(data));
        }

        [Fact]
        public void NoEncrypt_invalid_badJson()
        {
            const string badJson = "hello world";
            var data = Encoding.UTF8.GetBytes(badJson);

            var encryption = new NoBackupEncryption();
            Assert.False(encryption.CanBeDecrypted(data));
        }

        [Fact]
        public void NoEncrypt_invalid_wrongFormat()
        {
            const string json = "{\"something\":\"test\"}";
            var data = Encoding.UTF8.GetBytes(json);

            var encryption = new NoBackupEncryption();
            Assert.False(encryption.CanBeDecrypted(data));
        }
    }
}