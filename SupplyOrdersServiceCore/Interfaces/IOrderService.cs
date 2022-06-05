using SupplyOrdersServiceCore.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupplyOrdersServiceCore.Interfaces
{
    public interface IOrderService
    {
        bool CreateOrder(Order order);
        Order ProcessOrderResponse(string fileName);
    }
}
