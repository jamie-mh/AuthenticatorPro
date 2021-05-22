// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections;
using System.Collections.Generic;
using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Shared.Data.Generator;

namespace AuthenticatorPro.Test.ClassData
{
    internal class FromValidOtpAuthUriClassData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { "otpauth://totp/issuer:username?secret=ABCDEFG", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = "username", Secret = "ABCDEFG" }}; // Username issuer pair
            yield return new object[] { "otpauth://totp/Big%20Company:username?secret=ABCDEFG", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "Big Company", Username = "username", Secret = "ABCDEFG" }}; // Username issuer pair (encoded) 1/3
            yield return new object[] { "otpauth://totp/Big%20Company%3Ausername@test?secret=ABCDEFG", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "Big Company", Username = "username@test", Secret = "ABCDEFG" }}; // Username issuer pair (encoded) 2/3
            yield return new object[] { "otpauth://totp/Big%20Company%3Ausername%40test?secret=ABCDEFG", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "Big Company", Username = "username@test", Secret = "ABCDEFG" }}; // Username issuer pair (encoded) 3/3
            yield return new object[] { "otpauth://totp/issuer:username?secret=ABCDEFG&issuer=something", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = "username", Secret = "ABCDEFG" }}; // Redundant issuer parameter
            yield return new object[] { "otpauth://totp/username?secret=ABCDEFG&issuer=issuer", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = "username", Secret = "ABCDEFG" }}; // Issuer parameter
            yield return new object[] { "otpauth://totp/username?secret=ABCDEFG&issuer=Big%20Company", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "Big Company", Username = "username", Secret = "ABCDEFG" }}; // Issuer parameter (encoded)
            yield return new object[] { "otpauth://totp/issuer?secret=ABCDEFG", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = null, Secret = "ABCDEFG" }}; // No username
            yield return new object[] { "otpauth://hotp/issuer?secret=ABCDEFG", new Authenticator { Type = AuthenticatorType.Hotp, Issuer = "issuer", Username = null, Secret = "ABCDEFG", Counter = 0}}; // HOTP
            yield return new object[] { "otpauth://hotp/issuer?secret=ABCDEFG&counter=10", new Authenticator { Type = AuthenticatorType.Hotp, Issuer = "issuer", Username = null, Secret = "ABCDEFG", Counter = 10 }}; // HOTP with counter
            yield return new object[] { "otpauth://totp/issuer?secret=ABCDEFG&counter=10", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = null, Secret = "ABCDEFG", Counter = 0 }}; // TOTP with counter
            yield return new object[] { "otpauth://totp/issuer?secret=ABCDEFG&digits=7", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = null, Secret = "ABCDEFG", Digits = 7 }}; // Digits parameter 
            yield return new object[] { "otpauth://totp/issuer?secret=ABCDEFG&period=60", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = null, Secret = "ABCDEFG", Period = 60 }}; // Period parameter
            yield return new object[] { "otpauth://totp/issuer?secret=ABCDEFG", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = null, Secret = "ABCDEFG", Algorithm = Authenticator.DefaultAlgorithm }}; // Algorithm parameter 1/4
            yield return new object[] { "otpauth://totp/issuer?secret=ABCDEFG&algorithm=SHA1", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = null, Secret = "ABCDEFG", Algorithm = HashAlgorithm.Sha1 }}; // Algorithm parameter 2/4
            yield return new object[] { "otpauth://totp/issuer?secret=ABCDEFG&algorithm=SHA256", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = null, Secret = "ABCDEFG", Algorithm = HashAlgorithm.Sha256 }}; // Algorithm parameter 3/4
            yield return new object[] { "otpauth://totp/issuer?secret=ABCDEFG&algorithm=SHA512", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = null, Secret = "ABCDEFG", Algorithm = HashAlgorithm.Sha512 }}; // Algorithm parameter 4/4
            yield return new object[] { $"otpauth://totp/{new string('a', Authenticator.IssuerMaxLength + 1)}?secret=ABCDEFG", new Authenticator { Type = AuthenticatorType.Totp, Issuer = new string('a', Authenticator.IssuerMaxLength), Username = null, Secret = "ABCDEFG" }}; // Truncate issuer
            yield return new object[] { $"otpauth://totp/issuer:{new string('a', Authenticator.UsernameMaxLength + 1)}?secret=ABCDEFG", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = new string('a', Authenticator.UsernameMaxLength), Secret = "ABCDEFG" }}; // Truncate username
            yield return new object[] { "otpauth://totp/%F0%9F%98%80%3Ausername?secret=ABCDEFG", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "😀", Username = "username", Secret = "ABCDEFG" }}; // Multibyte characters 1/2
            yield return new object[] { "otpauth://totp/%E4%BD%A0%E5%A5%BD%E4%B8%96%E7%95%8C%3Ausername?secret=ABCDEFG", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "你好世界", Username = "username", Secret = "ABCDEFG" }}; // Multibyte characters 2/2
            yield return new object[] { "otpauth://totp/Steam?secret=ABCDEFG", new Authenticator { Type = AuthenticatorType.SteamOtp, Digits = 5, Issuer = "Steam", Username = null, Secret = "ABCDEFG" }}; // Steam issuer no username
            yield return new object[] { "otpauth://totp/Steam:username?secret=ABCDEFG", new Authenticator { Type = AuthenticatorType.SteamOtp, Digits = 5, Issuer = "Steam", Username = "username", Secret = "ABCDEFG" }}; // Steam issuer and username
            yield return new object[] { "otpauth://totp/issuer:username?secret=ABCDEFG&steam", new Authenticator { Type = AuthenticatorType.SteamOtp, Digits = 5, Issuer = "issuer", Username = "username", Secret = "ABCDEFG" }}; // Steam parameter
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}