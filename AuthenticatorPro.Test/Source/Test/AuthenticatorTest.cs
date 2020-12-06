using AuthenticatorPro.Data;
using Nito.AsyncEx;
using NUnit.Framework;

namespace AuthenticatorPro.Test.Test
{
    [TestFixture]
    internal class AuthenticatorTest : ConnectionBasedTest
    {
        [Test]
        public void Pass()
        {
            var auth = new Authenticator {Secret = "a", Issuer = "test"};

            AsyncContext.Run(async delegate
            {
                await _connection.InsertAsync(auth);
                var res = await _connection.GetAsync<Authenticator>("a");
                Assert.True(res.Issuer == "test");
            });
        }
    }
}