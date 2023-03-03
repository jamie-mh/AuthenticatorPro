// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Test.Backup.Comparer;
using AuthenticatorPro.Test.Backup.Fixture;
using System;
using Xunit;

namespace AuthenticatorPro.Test.Backup
{
    public class BackupTest : IClassFixture<BackupFixture>
    {
        private readonly BackupComparer _backupComparer;
        private readonly BackupFixture _backupFixture;

        public BackupTest(BackupFixture backupFixture)
        {
            _backupComparer = new BackupComparer();
            _backupFixture = backupFixture;
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("t")]
        [InlineData("test")]
        [InlineData("test123")]
        [InlineData("test123!?%")]
        [InlineData("PZqE=_L]Ra;ZD8N&")]
        [InlineData("tUT.3raAGQ[f]]Q@Ft=S}.r(Vk&CM9#`")]
        [InlineData(@"MS^NqdNp&y]tLz_5:P;UU/2LDd_uF7a""x@*a't/Da]'y&b~.=&z3x'r^u{X.@?vv")]
        [InlineData(
            @"p[{(]2QFSYWcaYdz=;eMtrnZ<bvh8QfW;8v""4HBTtW5H!xMGQKt^\\)f]7.fJ*9dcs@pq(9GKF?7FJ3Qtj$].V!U;:N^/eUj(zG;yC9/pPRGT'DHLR>S5h$}L\\AL~X,pA")]
        [InlineData(
            @"#n*rF;y3zVPr,B3*qP""C,S[NFk""qswBC;k2*E_gQ&)4Rh.h[f;WG)5w3uY7MGvM/wngR]72BP9;{)_~+Vd""ukx'S]Zt[!cE='43f}J:J_TR<\:`w""{\+`dgdy%vD]Ts:xKAXzgRW!_7vWF]zg*u.)F#ZzzY&[LEHFXH(D@M7Y""'6.e7n~u""[4kyc'TGF28Q""xgNg.!5=;Hfx'e)fx,:#mLmkA(*ty]4]7#;^?*QF4xDK&Fx-)f}(ph=PL*N'#w9`")]
        [InlineData("你好世界")]
        [InlineData("😀 😃 😄 😁 😆 😅 😂 🤣")]
        public void ToBytesFromBytes(string password)
        {
            var transformed = Core.Backup.Backup.FromBytes(_backupFixture.Backup.ToBytes(password), password);
            Assert.Equal(_backupFixture.Backup, transformed, _backupComparer);
        }

        [Fact]
        public void FromBytes_invalidHeader()
        {
            Assert.Throws<ArgumentException>(delegate { Core.Backup.Backup.FromBytes("invalid"u8.ToArray(), "test"); });
        }

        [Fact]
        public void FromBytes_invalidPassword()
        {
            Assert.Throws<ArgumentException>(delegate { Core.Backup.Backup.FromBytes(_backupFixture.EncryptedData, "testing1"); });
        }

        [Fact]
        public void FromBytes_invalidJson()
        {
            var bytes = "abcdef"u8.ToArray();
            Assert.Throws<ArgumentException>(delegate { Core.Backup.Backup.FromBytes(bytes, null); });
        }

        [Fact]
        public void IsReadableWithoutPassword_ok()
        {
            var bytes = "{\"field\": 1}"u8.ToArray();
            Assert.True(Core.Backup.Backup.IsReadableWithoutPassword(bytes));
        }

        [Fact]
        public void IsReadableWithoutPassword_invalidString()
        {
            var bytes = "testing"u8.ToArray();
            Assert.False(Core.Backup.Backup.IsReadableWithoutPassword(bytes));
        }

        [Fact]
        public void IsReadableWithoutPassword_invalidJson()
        {
            var bytes = "{!!!}"u8.ToArray();
            Assert.False(Core.Backup.Backup.IsReadableWithoutPassword(bytes));
        }

        [Fact]
        public void HasValidEncryptionHeader_ok()
        {
            var bytes = "AuthenticatorPro"u8.ToArray();
            Assert.True(Core.Backup.Backup.HasValidEncryptionHeader(bytes));
        }

        [Fact]
        public void HasValidEncryptionHeader_notOk()
        {
            var bytes = "SomethingElse123"u8.ToArray();
            Assert.False(Core.Backup.Backup.IsReadableWithoutPassword(bytes));
        }

        [Fact]
        public void HasValidEncryptionHeader_empty()
        {
            Assert.False(Core.Backup.Backup.HasValidEncryptionHeader(Array.Empty<byte>()));
        }
    }
}