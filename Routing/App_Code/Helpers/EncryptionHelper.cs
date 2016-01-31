using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Umbraco.Core.Logging;

namespace Routing.Helpers
{
    public class EncryptionHelper
    {

        private static byte[] salt = Encoding.ASCII.GetBytes(@"%rtal/Z_2kd*1s$(/_ªkdl¨Ñña4y9iue");
        private static string keyPadding = @"ªQa7_!1·Z$0mVt%z&S_r/5ke*b2(3)y4u=8?¿9;";

        public static string EncryptAES(string text, string key)
        {
            string result = text;
            if (!string.IsNullOrWhiteSpace(text))
            {
                Rfc2898DeriveBytes saltKey = new Rfc2898DeriveBytes(key + keyPadding, salt);
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (StreamWriter streamWriter = new StreamWriter(new CryptoStream(memoryStream, new RijndaelManaged().CreateEncryptor(saltKey.GetBytes(32), saltKey.GetBytes(16)), CryptoStreamMode.Write)))
                    {
                        streamWriter.Write(text);
                        streamWriter.Close();
                    }
                    memoryStream.Close();
                    result = Convert.ToBase64String(memoryStream.ToArray());
                }
            }
            return result;
        }

        public static string DecryptAES(string text, string key)
        {
            string result = text;
            if (!string.IsNullOrWhiteSpace(text))
            {
                try
                {
                    Rfc2898DeriveBytes saltKey = new Rfc2898DeriveBytes(key + keyPadding, salt);
                    using (MemoryStream memoryStream = new MemoryStream(Convert.FromBase64String(text)))
                    {
                        using (StreamReader streamReader = new StreamReader(new CryptoStream(memoryStream, new RijndaelManaged().CreateDecryptor(saltKey.GetBytes(32), saltKey.GetBytes(16)), CryptoStreamMode.Read)))
                        {
                            result = streamReader.ReadToEnd();
                        }
                    }
                }
                catch (Exception ex)
                {
                    result = string.Empty;
                    LogHelper.Error<EncryptionHelper>("Error in the method DecryptAES(). Wrong key.", ex);
                }
            }
            return result;
        }

    }
}