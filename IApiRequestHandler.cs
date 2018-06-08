using System.Threading.Tasks;

namespace CacheManager
{
    public interface IApiRequestHandler
    {
        Task<string> GetHttpResponseAsync(string uri);
    }
}
