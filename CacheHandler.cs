using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Caching.Distributed;
using System.Threading.Tasks;
using CacheManager.Common;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace CacheManager
{
    public sealed class CacheHandler
    {
        #region Private Variables
        private static IDistributedCache _cache;
        private static IApiRequestHandler _apiRequestHandler;
        private static string _dataApikey;
        #endregion

        public static CacheHandler Instance => _instance ?? (_instance = new CacheHandler());
        private static CacheHandler _instance;

        #region Constructors

        private CacheHandler()
        {
        }
        #endregion


        internal void InitCacheHandler(IConfiguration configuration, IApiRequestHandler apiRequestHandler, IDistributedCache cache)
        {
            _dataApikey = configuration.GetSection("Appsettings").GetSection("DataApikey").Value;
            _cache = cache;
            _apiRequestHandler = apiRequestHandler;
        }

        #region Public Methods

        public void PrepareCacheEntry()
        {
            DateTime endTime = DateTime.Now;
            try
            {
                string[] timeFrameArray = Enum.GetNames(typeof(TimeFrames));
                foreach (string timeFrame in timeFrameArray)
                {
                    Uri dataApikey = new Uri($"{_dataApikey}/OrderStatistics?timeFrame={timeFrame}&endTime={endTime}");
                    string stringResult = _apiRequestHandler.GetHttpResponseAsync(dataApikey.ToString()).Result;
                    if (!string.IsNullOrEmpty(stringResult))
                    {
                        IList<OrderStatistic> orderStatisticList = JsonConvert.DeserializeObject<IList<OrderStatistic>>(stringResult);
                        var orderStatisticListByCode = orderStatisticList.GroupBy(item => item.RestaurantCode,
                                (key, group) => new { RestaurantCode = key, Items = @group.ToList() })
                            .ToList();
                        foreach (var item in orderStatisticListByCode)
                        {
                            string key = $"{timeFrame}/{item.RestaurantCode}";
                            string cacheString = JsonConvert.SerializeObject(item.Items);
                            Task.Run(() => UpdateCacheAsync(cacheString, key));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        #endregion

        #region Private Methods
        private static async Task UpdateCacheAsync(string stringResult, string key)
        {
            try
            {
                Console.WriteLine("Updating the values for the filters");

                await _cache.SetStringAsync(key, stringResult, new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured during the process");
                Console.WriteLine(ex.Message);
            }
        } 
        #endregion
    }
}
