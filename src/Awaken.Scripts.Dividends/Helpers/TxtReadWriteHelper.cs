using System.IO;

namespace Awaken.Scripts.Dividends.Helpers
{
    public class TxtReadWriteHelper
    {
        public static string Read(string path)
        {
            var streamReader = new StreamReader(path);
            var context = streamReader.ReadToEnd();
            streamReader.Close();
            return context;
        }

        public static void Write(string path, string txt)
        {
            File.WriteAllText(path, txt);
        }
    }
}