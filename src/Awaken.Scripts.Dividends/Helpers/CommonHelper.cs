using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Awaken.Scripts.Dividends.Helpers;

public class CommonHelper
{
    public static string ConvertEntityNameToDb<T>()
    {
        var typeName = typeof(T).Name;
        var sb = new StringBuilder();
        if (!typeName.IsNullOrEmpty())
        {
            foreach (var c in typeName.ToCharArray())
            {
                var tmp = c.ToString();
                if (Regex.IsMatch(tmp, "[A-Z]"))
                {
                    tmp = sb.Length == 0 ? tmp.ToLower() : "_" + tmp.ToLower();
                }

                sb.Append(tmp);
            }
        }

        return sb.ToString();
    }
}