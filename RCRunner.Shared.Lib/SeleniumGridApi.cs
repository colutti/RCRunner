using System.Configuration;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace RCRunner.Shared.Lib
{
    class SeleniumGridApi
    {
        private static string HttpGet(string url)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                var response = (HttpWebResponse)request.GetResponse();
                var stream = response.GetResponseStream();
                if (stream == null) return string.Empty;
                var reader = new StreamReader(stream);
                var data = reader.ReadToEnd();
                reader.Close();
                stream.Close();
                return data;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static int GetNumberAvailableBrownsers()
        {
            var seleniumGriUrl = ConfigurationManager.AppSettings["SeleniumHubURL"];

            var data = HttpGet(seleniumGriUrl);

            var count = Regex.Matches(data, "seleniumProtocol=WebDriver").Count +
                        Regex.Matches(data, "/session/").Count;

            return count;
            //return 2;
        }
    }
}
