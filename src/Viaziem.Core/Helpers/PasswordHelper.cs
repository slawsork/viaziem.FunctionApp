using System.Security.Cryptography;
using System.Text;

namespace Viaziem.Core.Helpers
{
    public interface IPasswordHelper
    {
        string Create(string value);

        bool Validate(string value, string hash);
    }

    public class PasswordHelper : IPasswordHelper
    {
        public string Create(string value)
        {
            using var md5Hash = MD5.Create();
            var hash = GetMd5Hash(md5Hash, value);
            return hash;
        }

        public bool Validate(string value, string hash)
        {
            return Create(value) == hash;
        }

        private static string GetMd5Hash(HashAlgorithm md5Hash, string input)
        {
            var data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sBuilder = new StringBuilder();

            foreach (var t in data) sBuilder.Append(t.ToString("x2"));

            return sBuilder.ToString();
        }
    }
}