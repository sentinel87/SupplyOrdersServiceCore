using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SupplyOrdersServiceCore.Domain.Enums;
using SupplyOrdersServiceCore.Domain.Models;
using SupplyOrdersServiceCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SupplyOrdersServiceCore.Modules
{
    public class OrderCreator
    {
        private readonly ILogger<OrderCreator> _logger;
        private readonly IOrderService _orderService;
        private readonly IDatabaseService _databaseService;
        public OrderCreator(ILogger<OrderCreator>logger, IOrderService orderService, IDatabaseService databaseService)
        {
            _logger = logger;
            _orderService = orderService;
            _databaseService = databaseService;
        }

        public async Task ProcessOrders(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Reading orders from db...");
            var ordersToProcess = await _databaseService.GetOrders(OrderStatus.Registered, stoppingToken);
            if (ordersToProcess != null)
            {
                _logger.LogInformation($"Orders to process: {ordersToProcess.Count}");
                foreach (Order order in ordersToProcess)
                {
                    _logger.LogInformation($"Processing order {order.OrderSymbol}...");
                    order.Products = await _databaseService.GetOrderPositions(order.Id, stoppingToken);
                    if (order.Products != null)
                    {
                        if (order.Products.Count > 0)
                        {
                            int excludedCount = order.Products.Where(x => x.CompanyId == 0).Count();
                            if (excludedCount != order.Products.Count)
                            {
                                bool created = _orderService.CreateOrder(order);
                                if (created)
                                {
                                    order.Status = OrderStatus.Created;
                                    if (await _databaseService.UpdateOrder(order, stoppingToken) == true)
                                        _logger.LogInformation("Order processed correctly.");
                                    else
                                        _logger.LogError("Error occured during order status db update.");
                                }
                                else
                                {
                                    if(await _databaseService.UpdateOrderStatus(OrderStatus.Error, order.Id, stoppingToken)==true)
                                        _logger.LogInformation("Order signed as 'Error'.");
                                    else
                                        _logger.LogError("Error occured during order status db update.");
                                }
                            }
                            else
                            {
                                if(await _databaseService.SetOrderComment("All positions not present in the main product list.", order.Id, stoppingToken)==false)
                                    _logger.LogError("Error occured during order comment db update.");
                                if(await _databaseService.UpdateOrderStatus(OrderStatus.Processed, order.Id, stoppingToken)==true)
                                    _logger.LogInformation("All positions not present in the main product list. Setting status for order confirmation.");
                                else
                                    _logger.LogError("Error occured during order status db update.");
                            }
                        }
                        else
                        {
                            if(await _databaseService.UpdateOrderStatus(OrderStatus.Canceled, order.Id, stoppingToken)==false)
                                _logger.LogError("Error occured during order status db update.");
                            if(await _databaseService.SetOrderComment("Pozycje w zamówieniu mają zerowe stany (ilość zatwierdzona).", order.Id, stoppingToken)==true)
                                _logger.LogInformation("Order signed as 'Canceled'.");
                            else
                                _logger.LogError("Error occured during order comment db update.");
                        }
                    }
                    else
                    {
                        if(await _databaseService.UpdateOrderStatus(OrderStatus.Stopped, order.Id, stoppingToken)==true)
                            _logger.LogInformation("Error occured during products load. Order signed as 'Error'.");
                        else
                            _logger.LogError("Error occured during order status db update.");
                    }
                }
            }
            _logger.LogInformation("Reading completed.");
        }
    }
}
