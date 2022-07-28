using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SupplyOrdersServiceCore.Domain.Enums;
using SupplyOrdersServiceCore.Domain.Interfaces;
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
    public class OrderChecker
    {
        private readonly ILogger<OrderChecker> _logger;
        private readonly IConfiguration _configuration;
        private readonly IDatabaseService _databaseService;
        private readonly IStorageService _storageService;
        private readonly IOrderService _orderService;

        public OrderChecker(ILogger<OrderChecker>logger, IConfiguration configuration, IDatabaseService databaseService, IStorageService storageService, IOrderService orderService)
        {
            _logger = logger;
            _configuration = configuration;
            _databaseService = databaseService;
            _storageService = storageService;
            _orderService = orderService;
        }

        public async Task ProcessOrderResponse(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Analysing response files...");

            var orderRespPath = _configuration.GetSection("Parameters").GetValue<string>("OrderResponsePath");
            var archivePath = _configuration.GetSection("Parameters").GetValue<string>("ArchivePath");

            int queueFiles = _storageService.GetFilesCount(orderRespPath, "SH*");
            if (queueFiles != -1)
            {
                if (queueFiles > 0)
                {
                    _logger.LogInformation($"Founded files: {queueFiles}");
                    List<string> fileNames = _storageService.GetFiles(orderRespPath, "SH*");
                    if (fileNames != null)
                    {
                        foreach (string fileName in fileNames)
                        {
                            _logger.LogInformation($"Processing file {fileName}...");
                            Order order = _orderService.ProcessOrderResponse(fileName);
                            if (order != null)
                            {
                                int checkedStatus = await _databaseService.CheckOrderStatus(order.Id, stoppingToken);
                                if (checkedStatus == (int)OrderStatus.Created || checkedStatus == (int)OrderStatus.Processing)
                                {
                                    _logger.LogInformation("Updating order...");
                                    foreach (Product product in order.Products)
                                    {
                                        await _databaseService.UpdateOrderPositionProcessedQuantity(product, stoppingToken);
                                    }
                                    order.ModificationDate = DateTime.Now;
                                    bool updated = await _databaseService.UpdateOrder(order, stoppingToken, false);
                                    if (updated)
                                    {
                                        _logger.LogInformation("Order successfuly updated.");
                                        manageFileMovement(fileName, orderRespPath, archivePath);
                                    }
                                }
                                else
                                {
                                    _logger.LogInformation("Order doesn't have proper status for update.");
                                    manageFileMovement(fileName, orderRespPath, archivePath);
                                }
                            }
                        }
                    }
                    else
                        _logger.LogError("Cannot acquire file list.");
                }
            }
            _logger.LogInformation("Analysis completed.");
        }

        private void manageFileMovement(string fileName, string orderRespPath, string archivePath)
        {
            string phrase = fileName.Replace(".csv", "");
            int queueFiles = _storageService.GetFilesCount(archivePath, $"{phrase}*");
            if (queueFiles != 0)
            {
                if (_storageService.MoveFiles(orderRespPath, archivePath, fileName, $"{phrase}_{queueFiles + 1}.csv") == true)
                {
                    _logger.LogInformation("File moved (another shortage).");
                }
            }
            else
            {
                if (_storageService.MoveFiles(orderRespPath, archivePath, fileName, fileName) == true)
                {
                    _logger.LogInformation("File moved.");
                }
            }
        }
    }
}
