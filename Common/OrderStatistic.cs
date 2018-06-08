using System;
using System.Collections.Generic;
using System.Text;

namespace CacheManager.Common
{
    public class OrderStatistic
    {
        public string RestaurantCode { get; set; }
        public string RestaurantLongName { get; set; }
        public int? OrderCount { get; set; }
        public int? DeliveryTime { get; set; }
    }
}
