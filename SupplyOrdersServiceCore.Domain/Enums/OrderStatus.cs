using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupplyOrdersServiceCore.Domain.Enums
{
    public enum OrderStatus
    {
        Registered = 0,
        Created = 1,
        Processing = 2,
        Processed = 3,
        Stopped = 4,
        Error = 5,
        Canceled = 6
    }
}
