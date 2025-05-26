using System.Security.Cryptography;
using System.Text;

namespace Net.Utilities.Helper.Object.String;

// ReSharper disable once InconsistentNaming
public static class MD5Encrypt
{
    public static string Encrypt32(string password = "", bool lowerCase = false)
    {
#if NET
        var s = MD5.HashData(Encoding.UTF8.GetBytes(password));
#else
        using var md5 = MD5.Create();

        var s = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
#endif

        return s.Aggregate(string.Empty, (current, item) => string.Concat(current, item.ToString(lowerCase ? "x2" : "X2")));
    }
}