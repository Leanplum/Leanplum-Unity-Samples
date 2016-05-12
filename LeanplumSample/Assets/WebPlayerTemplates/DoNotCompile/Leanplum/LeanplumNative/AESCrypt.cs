﻿// Copyright 2013, Leanplum, Inc.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace LeanplumSDK
{
    /// <summary>
    ///     Class AESCrypt. Adapted from
    ///     http://stackoverflow.com/questions/273452/using-aes-encryption-in-c-sharp/14286740#14286740
    /// </summary>
    internal class AESCrypt
    {
        // Aliasing constants here to make minimal changes to the original code.
        private const int iterations = Constants.Crypt.ITER_COUNT;
        private const int keySize = Constants.Crypt.KEY_LENGTH;
        private const string salt = Constants.Crypt.SALT;
        private const string vector = Constants.Crypt.VECTOR;

        /// <summary>
        ///     Encrypts the specified value using password with AES.
        /// </summary>
        /// <param name="plaintext">The plaintext.</param>
        /// <param name="key">The key.</param>
        /// <returns>The encrypted value</returns>
        public static string Encrypt(string plaintext, string key)
        {
            return Encrypt<AesManaged>(plaintext, key);
        }

        private static string Encrypt<T>(string plaintext, string key)
            where T : SymmetricAlgorithm, new()
        {
            byte[] vectorBytes = Encoding.ASCII.GetBytes(vector);
            byte[] saltBytes = Encoding.ASCII.GetBytes(salt);
            byte[] valueBytes = Encoding.ASCII.GetBytes(plaintext);

            byte[] encrypted;
            using (T cipher = new T())
            {
                try
                {
                    Rfc2898DeriveBytes passwordBytes = new Rfc2898DeriveBytes(key, saltBytes, iterations);
                    byte[] keyBytes = passwordBytes.GetBytes(keySize / 8);

                    cipher.Mode = CipherMode.CBC;

                    using (ICryptoTransform encryptor = cipher.CreateEncryptor(keyBytes, vectorBytes))
                    {
                        using (MemoryStream to = new MemoryStream())
                        {
                            using (CryptoStream writer = new CryptoStream(to, encryptor, CryptoStreamMode.Write))
                            {
                                writer.Write(valueBytes, 0, valueBytes.Length);
                                writer.FlushFinalBlock();
                                encrypted = to.ToArray();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LeanplumNative.CompatibilityLayer.LogError("Error performing encryption. " + ex.ToString());
                    return String.Empty;
                }
                cipher.Clear();
            }
            return Convert.ToBase64String(encrypted);
        }

        /// <summary>
        ///     Decrypts the specified ciphertext. Value must be a valid output from Encrypt.
        /// </summary>
        /// <param name="ciphertext">The ciphertext.</param>
        /// <param name="key">The key.</param>
        /// <returns>The decrypted value</returns>
        public static string Decrypt(string ciphertext, string key)
        {
            return Decrypt<AesManaged>(ciphertext, key);
        }

        private static string Decrypt<T>(string ciphertext, string key) where T : SymmetricAlgorithm, new()
        {
            byte[] vectorBytes = Encoding.ASCII.GetBytes(vector);
            byte[] saltBytes = Encoding.ASCII.GetBytes(salt);
            byte[] valueBytes = Convert.FromBase64String(ciphertext);

            byte[] decrypted;
            int decryptedByteCount = 0;

            using (T cipher = new T())
            {
                try
                {
                    Rfc2898DeriveBytes passwordBytes = new Rfc2898DeriveBytes(key, saltBytes, iterations);
                    byte[] keyBytes = passwordBytes.GetBytes(keySize / 8);

                    cipher.Mode = CipherMode.CBC;

                    using (ICryptoTransform decryptor = cipher.CreateDecryptor(keyBytes, vectorBytes))
                    {
                        using (MemoryStream from = new MemoryStream(valueBytes))
                        {
                            using (CryptoStream reader = new CryptoStream(from, decryptor, CryptoStreamMode.Read))
                            {
                                decrypted = new byte[valueBytes.Length];
                                decryptedByteCount = reader.Read(decrypted, 0, decrypted.Length);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LeanplumNative.CompatibilityLayer.LogError("Error performing decryption. " + ex.ToString());
                    return String.Empty;
                }

                cipher.Clear();
            }
            return Encoding.UTF8.GetString(decrypted, 0, decryptedByteCount);
        }
    }
}
