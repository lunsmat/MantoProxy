using System.Text;

namespace MantoProxy.Handlers
{
    class AuthHandler
    {
        public static bool HasPermission(string authHeader)
        {
            if (string.IsNullOrEmpty(authHeader)) return false;

            if (!authHeader.StartsWith("Proxy-Authorization:", StringComparison.OrdinalIgnoreCase))
                return false;

            string encoded = authHeader.Substring(authHeader.IndexOf(' ') + 1).Trim();

            if (encoded.StartsWith("Basic", StringComparison.OrdinalIgnoreCase))
                encoded = encoded.Substring(5).Trim();

            try
            {
                string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                var parts = decoded.Split(':');
                if (parts.Length != 2) return false;

                var user = parts[0];
                var pass = parts[1];

                if (user == "john.doe" && pass == "banana") return true;
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}