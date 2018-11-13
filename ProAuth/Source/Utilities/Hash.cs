using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ProAuth.Utilities
{
    internal static class Hash
    {
        public static string SHA1(string input)
        {
            var hash = (new SHA1Managed()).ComputeHash(Encoding.UTF8.GetBytes(input));
            return string.Join("", hash.Select(b => b.ToString("x2")).ToArray());
        }
    }
}