using System;
using System.Text;
using System.Text.RegularExpressions;

namespace QuadraticVote.Application.Service.Extensions
{
    public class CommonHelper
    {
        public static string CoverntEntityNameToDb<T>()
        {
            var typeName = typeof(T).Name;
            StringBuilder sb = new StringBuilder();
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
}