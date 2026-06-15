using System;
using System.Security.Cryptography;
using System.Text;

namespace GXLightBrowser
{
    internal sealed class PasswordVaultEntry
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string Username { get; set; }
        public string Note { get; set; }
        public DateTime ImportedUtc { get; set; }
        private byte[] _protectedPassword;

        public void SetPassword(string password)
        {
            byte[] clear = Encoding.UTF8.GetBytes(password ?? string.Empty);
            try
            {
                _protectedPassword = ProtectedData.Protect(clear, null, DataProtectionScope.CurrentUser);
            }
            finally
            {
                Array.Clear(clear, 0, clear.Length);
            }
        }

        public string RevealPassword()
        {
            if (_protectedPassword == null || _protectedPassword.Length == 0)
            {
                return string.Empty;
            }

            byte[] clear = ProtectedData.Unprotect(_protectedPassword, null, DataProtectionScope.CurrentUser);
            try
            {
                return Encoding.UTF8.GetString(clear);
            }
            finally
            {
                Array.Clear(clear, 0, clear.Length);
            }
        }
    }
}
