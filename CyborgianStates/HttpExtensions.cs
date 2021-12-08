using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

namespace CyborgianStates
{
    public static class HttpExtensions
    {
        public static async Task<XmlDocument> ReadXmlAsync(this HttpResponseMessage httpResponse)
        {
            if (httpResponse is null)
                throw new ArgumentNullException(nameof(httpResponse));
            if (httpResponse.IsSuccessStatusCode)
            {
                using (Stream stream = await httpResponse.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    try
                    {
                        XmlDocument xml = new XmlDocument();
                        xml.Load(stream);
                        return xml;
                    }
                    catch (XmlException ex)
                    {
                        throw new ApplicationException($"A error while loading xml occured.", ex);
                    }
                }
            }
            else
            {
                return null;
            }
        }
    }
}