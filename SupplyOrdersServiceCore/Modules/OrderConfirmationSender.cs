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
    public class OrderConfirmationSender
    {
        private readonly ILogger<OrderConfirmationSender> _logger;
        private readonly IConfiguration _configuration;
        private readonly IFtpService _ftpService;
        private readonly IExportService _exportService;
        private readonly IStorageService _storageService;
        private readonly IDatabaseService _databaseService;
        public OrderConfirmationSender(ILogger<OrderConfirmationSender>logger, IConfiguration configuration, IFtpService ftpService, IExportService exportService, IStorageService storageService, IDatabaseService databaseService)
        {
            _logger = logger;
            _configuration = configuration;
            _ftpService = ftpService;
            _exportService = exportService;
            _storageService = storageService;
            _databaseService = databaseService;
        }

        public async Task ProcessPreparedOrders(CancellationToken stoppingToken)
        {
            var dbfTempPath = _configuration.GetSection("Parameters").GetValue<string>("DbfTempPath");
            var dbfZipTempPath = _configuration.GetSection("Parameters").GetValue<string>("DbfZipTempPath");

            _logger.LogInformation("Analysing orders for confirmation...");
            var ordersToSend = await _databaseService.GetOrdersToConfirm(stoppingToken);
            if(ordersToSend!=null)
            {
                _logger.LogInformation($"Orders prepared for confirmation: {ordersToSend.Count}.");
                foreach(Order order in ordersToSend)
                {
                    _logger.LogInformation($"Processing order {order.OrderSymbol}...");
                    order.Products = await _databaseService.GetOrderPositions(order.Id, stoppingToken);
                    if(order.Products!=null)
                    {
                        if (order.Products.Count > 0)
                        {
                            _storageService.ClearDir(dbfTempPath);
                            _storageService.ClearDir(dbfZipTempPath);

                            var ftpDir = await _databaseService.GetFtpLocation(order.ClientCompanyId, stoppingToken);
                            if (String.IsNullOrEmpty(ftpDir) || ftpDir == "error")
                            {
                                await _databaseService.SetFtpConfirmationFlag(order.Id, FtpStatus.Error, stoppingToken);
                                _logger.LogInformation($"Cannot find directory name {ftpDir} in the DB dictionary.");
                                continue;
                            }

                            if(await _ftpService.CheckFtpDirectoryExist(ftpDir, stoppingToken))
                            {
                                var fileName = createFileName(order.Id.ToString());
                                var cftFilePath = $"{dbfTempPath}/{fileName}.cft";
                                bool ptrCreated = _storageService.CreateTextFile(cftFilePath, "Startup file...");
                                bool confirmationCreated = await _exportService.CreateOrderExportFiles(order);
                                if(ptrCreated && confirmationCreated)
                                {
                                    bool ptrMoved = await _ftpService.CopyFileToFtp(cftFilePath, $"{ftpDir}/{fileName}.cft", stoppingToken);
                                    bool zipCreated = _storageService.CreateZip(dbfZipTempPath, $"{dbfTempPath}/{fileName}.zip");
                                    if (zipCreated)
                                        zipCreated = await _ftpService.CopyFileToFtp($"{dbfTempPath}/{fileName}.zip", $"{ftpDir}/{fileName}.zip", stoppingToken);

                                    if (ptrMoved && zipCreated)
                                    {
                                        if (await _databaseService.SetFtpConfirmationFlag(order.Id, FtpStatus.Sended, stoppingToken)==false)
                                            _logger.LogError("Error during confirmation flag update.");
                                        await _databaseService.SetFtpFileName(order.Id, $"{fileName}.zip", stoppingToken);
                                        _logger.LogInformation($"Order placed on the client FTP directory (files: {fileName}.cft ,{fileName}.zip).");
                                    }
                                    else
                                    {
                                        await _databaseService.SetFtpConfirmationFlag(order.Id, FtpStatus.Error, stoppingToken);
                                        _logger.LogError("Not all files were moved.");
                                    }
                                }
                                else
                                {
                                    await _databaseService.SetFtpConfirmationFlag(order.Id, FtpStatus.Error, stoppingToken);
                                    _logger.LogInformation($"Error occured during confirmation files creation.");
                                }
                            }
                            else
                            {
                                await _databaseService.SetFtpConfirmationFlag(order.Id, FtpStatus.Error, stoppingToken);
                                _logger.LogInformation($"Cannot find destination FTP directory {ftpDir}.");
                            }
                        }
                        else
                        {
                            await _databaseService.UpdateOrderStatus(OrderStatus.Canceled, order.Id, stoppingToken);
                            await _databaseService.SetOrderComment("All products have 0 quantity.", order.Id, stoppingToken);
                            _logger.LogInformation($"All products have 0 quantity.");
                        }
                    }
                    else
                    {
                        await _databaseService.SetFtpConfirmationFlag(order.Id, FtpStatus.Error, stoppingToken);
                        _logger.LogError($"Error during retriving product list.");
                    }
                    _logger.LogInformation($"Processing completed.");
                    _storageService.ClearDir(dbfTempPath);
                    _storageService.ClearDir(dbfZipTempPath);
                }
            }
            _logger.LogInformation("Analysis completed.");
        }

        private string createFileName(string orderNumber)
        {
            string fileName = "ON";
            if (orderNumber.Length < 6)
            {
                int zeroSpaces = 6 - orderNumber.Length;
                for (int i = 0; i < zeroSpaces; i++)
                    fileName += "0";
            }
            fileName += $"{orderNumber}";
            return fileName;
        }
    }
}
