using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SupplyOrdersServiceCore.Domain.Enums;
using SupplyOrdersServiceCore.Domain.Models;
using SupplyOrdersServiceCore.Interfaces;
using SupplyOrdersServiceCore.Modules;

namespace SupplyOrdersServiceCore_Tests
{
    public class OrderConfirmationSender_UnitTest
    {
        OrderConfirmationSender _orderConfirmationSender;
        Mock<ILogger<OrderConfirmationSender>> _loggerMock;
        Mock<IDatabaseService> _databaseServiceMock;
        Mock<IStorageService> _storageServiceMock;
        Mock<IFtpService> _ftpServiceMock;
        Mock<IExportService> _exportServiceMock;

        [SetUp]
        public void Setup()
        {
            var builder = new ConfigurationBuilder();
            buildConfig(builder);
            var configRoot = builder.Build();
            _loggerMock = new Mock<ILogger<OrderConfirmationSender>>();
            _databaseServiceMock = new Mock<IDatabaseService>();
            _storageServiceMock = new Mock<IStorageService>();
            _ftpServiceMock = new Mock<IFtpService>();
            _exportServiceMock = new Mock<IExportService>();
            _orderConfirmationSender = new OrderConfirmationSender(_loggerMock.Object, configRoot, _ftpServiceMock.Object, _exportServiceMock.Object, _storageServiceMock.Object, _databaseServiceMock.Object);
        }

        void buildConfig(IConfigurationBuilder builder)
        {
            builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "production"}.json", optional: true)
                .AddEnvironmentVariables();
        }

        [Test]
        public async Task ProcessPreparedOrders_Proper()
        {
            Order order = new Order()
            {
                Id = 1,
                OrderSymbol = "ORD_TEST",
                Status = OrderStatus.Created,
                ClientCompanyId = 12345,
                CreationDate = DateTime.Now,
                ModificationDate = DateTime.Now,
                FtpStatus = FtpStatus.NotSended,
                Comment = "Unit test order.",
                Wholesaler = 154432
            };

            var products = new List<Product>()
            {
                new Product()
                    {
                        Id = 1,
                        Name = "Test product 1",
                        CentralIdentNumber = "11223344",
                        CompanyId = 14567,
                        Quantity = 43,
                        ProcessedQuantity = 0,
                    },
                    new Product()
                    {
                        Id = 2,
                        Name = "Test product 2",
                        CentralIdentNumber = "33117799",
                        CompanyId = 1432,
                        Quantity = 4,
                        ProcessedQuantity = 0,
                    },
            };
            _databaseServiceMock.Setup(r => r.GetOrdersToConfirm(It.IsAny<CancellationToken>())).Returns(Task.FromResult(new List<Order>() { order }));
            _databaseServiceMock.Setup(r => r.GetOrderPositions(It.IsAny<long>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(products));
            _databaseServiceMock.Setup(r => r.GetFtpLocation(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult("ts123456"));
            _ftpServiceMock.Setup(r => r.CheckFtpDirectoryExist(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _storageServiceMock.Setup(r => r.CreateTextFile(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            _exportServiceMock.Setup(r => r.CreateOrderExportFiles(It.IsAny<Order>())).Returns(Task.FromResult(true));
            _ftpServiceMock.Setup(r => r.CopyFileToFtp(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _storageServiceMock.Setup(r => r.CreateZip(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            CancellationToken token = new CancellationToken();
            await _orderConfirmationSender.ProcessPreparedOrders(token);

            _loggerMock.Verify(
                m => m.Log(LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString().Contains("Order placed on the client FTP directory")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once
            );
        }

        [Test]
        public async Task ProcessPreparedOrders_NoProducts()
        {
            Order order = new Order()
            {
                Id = 1,
                OrderSymbol = "ORD_TEST",
                Status = OrderStatus.Created,
                ClientCompanyId = 12345,
                CreationDate = DateTime.Now,
                ModificationDate = DateTime.Now,
                FtpStatus = FtpStatus.NotSended,
                Comment = "Unit test order.",
                Wholesaler = 154432
            };

            _databaseServiceMock.Setup(r => r.GetOrdersToConfirm(It.IsAny<CancellationToken>())).Returns(Task.FromResult(new List<Order>() { order }));
            _databaseServiceMock.Setup(r => r.GetOrderPositions(It.IsAny<long>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new List<Product>()));

            CancellationToken token = new CancellationToken();
            await _orderConfirmationSender.ProcessPreparedOrders(token);

            _loggerMock.Verify(
                m => m.Log(LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString().Contains("All products have 0 quantity.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once
            );
        }
    }
}
