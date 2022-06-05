using SupplyOrdersServiceCore.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupplyOrdersServiceCore.Interfaces
{
    public interface IExportService
    {
        public Task<bool> CreateOrderExportFiles(Order order);
    }
}
