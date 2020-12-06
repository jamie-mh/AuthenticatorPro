using AuthenticatorPro.Shared.Data;
using NUnit.Framework;

namespace AuthenticatorPro.Test.Test
{
    [TestFixture]
    public class IconTest
    {
        public static readonly object[] GetServiceTestCases =
        {
            new object[] { "google", Resource.Drawable.auth_google, Resource.Drawable.auth_google }, // Icon without specific dark variant
            new object[] { "github", Resource.Drawable.auth_github, Resource.Drawable.auth_github_dark }, // Icon with specific dark variant
            new object[] { "abcdefg", Resource.Drawable.auth_default, Resource.Drawable.auth_default_dark }, // Icon that doesn't exist
        };
        
        [Test]
        [TestCaseSource(nameof(GetServiceTestCases))]
        public void GetServiceTest(string key, int expectedResourceLight, int expectedResourceDark)
        {
            var iconLight = Icon.GetService(key, false);
            Assert.That(iconLight == expectedResourceLight);
            
            var iconDark = Icon.GetService(key, true);
            Assert.That(iconDark == expectedResourceDark);
        }

        public static readonly object[] FindServiceKeyByNameTestCases =
        {
            new object[] { "Google", "google" }, // Simple one word match
            new object[] { "Electronic Arts", "electronicarts" }, // Multiple word match 1/2
            new object[] { "Rockstar Games", "rockstargames" }, // Multiple word match 2/2
            new object[] { "LogMeIn Accounts", "logmein" }, // Match first word 1/2
            new object[] { "Nintendo Account", "nintendo" }, // Match first word 2/2
            new object[] { "ABCDEFG", Icon.Default }, // No match
        };

        [Test]
        [TestCaseSource(nameof(FindServiceKeyByNameTestCases))]
        public void FindServiceKeyByNameTest(string name, string expectedKey)
        {
            Assert.That(Icon.FindServiceKeyByName(name) == expectedKey); 
        }
    }
}