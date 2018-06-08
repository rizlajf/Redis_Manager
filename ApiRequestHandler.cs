using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CacheManager
{
    public class ApiRequestHandler : IApiRequestHandler
    {
        public async Task<string> GetHttpResponseAsync(string uri)
        {
            string stringResult = string.Empty;
            try
            {
                if (string.IsNullOrEmpty(uri))
                    return null;
                using (HttpClient client = new HttpClient { BaseAddress = new Uri(uri) })
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    ConfiguredTaskAwaitable<HttpResponseMessage> response = client.GetAsync(uri).ConfigureAwait(false);

                    if (response.GetAwaiter().GetResult().StatusCode == HttpStatusCode.OK)
                        stringResult = await response.GetAwaiter().GetResult().Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
            catch (WebException ex) when (ex.Status == WebExceptionStatus.ProtocolError)
            {
                throw new Exception("Could not complete the job due to ProtocolError");
            }
            catch (WebException ex) when ((ex.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
            {
                throw new Exception("Please make sure the Url is correct");
            }
            catch (WebException ex) when ((ex.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.InternalServerError)
            { 
                throw new Exception("Internal Server Error");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return stringResult;
        }
    }
}
