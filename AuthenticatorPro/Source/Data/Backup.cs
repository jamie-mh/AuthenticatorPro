using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using PCLCrypto;
using SymmetricAlgorithm = PCLCrypto.SymmetricAlgorithm;


namespace AuthenticatorPro.Data
{
    internal class Backup
    {
        private const SymmetricAlgorithm Algorithm = SymmetricAlgorithm.AesCbcPkcs7;

        public List<Authenticator> Authenticators { get; }
        public List<Category> Categories { get; }
        public List<AuthenticatorCategory> AuthenticatorCategories { get; }


        public Backup(List<Authenticator> authenticators, List<Category> categories, List<AuthenticatorCategory> authenticatorCategories)
        {
            Authenticators = authenticators;
            Categories = categories;
            AuthenticatorCategories = authenticatorCategories;
        }

        public byte[] ToBytes(string password)
        {
            var json = JsonConvert.SerializeObject(this);

            if(String.IsNullOrEmpty(password))
                return Encoding.UTF8.GetBytes(json);

            var sha256 = SHA256.Create();
            var keyMaterial = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            var unencryptedData = Encoding.UTF8.GetBytes(json);

            var provider =
                WinRTCrypto.SymmetricKeyAlgorithmProvider.OpenAlgorithm(Algorithm);

            var key = provider.CreateSymmetricKey(keyMaterial);
            return WinRTCrypto.CryptographicEngine.Encrypt(key, unencryptedData);
        }

        public static Backup FromBytes(byte[] data, string password)
        {
            string json;

            if(String.IsNullOrEmpty(password))
                json = Encoding.UTF8.GetString(data);
            else
            {
                var sha256 = SHA256.Create();
                var passwordBytes = Encoding.UTF8.GetBytes(password);
                var keyMaterial = sha256.ComputeHash(passwordBytes);

                var provider =
                    WinRTCrypto.SymmetricKeyAlgorithmProvider.OpenAlgorithm(Algorithm);

                var key = provider.CreateSymmetricKey(keyMaterial);

                var raw = WinRTCrypto.CryptographicEngine.Decrypt(key, data);
                json = Encoding.UTF8.GetString(raw);
            }

            return JsonConvert.DeserializeObject<Backup>(json);
        }
    }
}