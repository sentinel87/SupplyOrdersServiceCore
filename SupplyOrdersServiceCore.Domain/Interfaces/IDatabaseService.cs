using SupplyOrdersServiceCore.Domain.Enums;
using SupplyOrdersServiceCore.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SupplyOrdersServiceCore.Domain.Interfaces
{
    public interface IDatabaseService
    {
        bool IsConnected();
        Task OpenConnection(CancellationToken stoppingToken);
        Task CloseConnection();
        Task<int> CheckOrderStatus(long id, CancellationToken stoppingToken);
        Task<bool> SetOrderComment(string comment, long id, CancellationToken stoppingToken);
        Task<List<Order>> GetOrdersToConfirm(CancellationToken stoppingToken);
        Task<List<Order>> GetOrders(OrderStatus status, CancellationToken stoppingToken);
        Task<bool> UpdateOrderStatus(OrderStatus status, long id, CancellationToken stoppingToken);
        Task<bool> UpdateOrder(Order order, CancellationToken stoppingToken, bool fileCreation = true);
        Task<List<Product>> GetOrderPositions(long orderId, CancellationToken stoppingToken);
        Task<List<Product>> GetConfirmedOrderPositions(long orderId, CancellationToken stoppingToken);
        Task<bool> UpdateOrderPositionProcessedQuantity(Product product, CancellationToken stoppingToken);
        Task<bool> SetFtpConfirmationFlag(long orderId, FtpStatus status, CancellationToken stoppingToken);
        Task<bool> SetFtpFileName(long orderId, string fileName, CancellationToken stoppingToken);
        Task<string> GetFtpLocation(int id, CancellationToken stoppingToken);
    }
}
