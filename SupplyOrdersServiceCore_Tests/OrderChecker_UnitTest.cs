using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SupplyOrdersServiceCore.Domain.Enums;
using SupplyOrdersServiceCore.Domain.Interfaces;
using SupplyOrdersServiceCore.Domain.Models;
using SupplyOrdersServiceCore.Interfaces;
using SupplyOrdersServiceCore.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SupplyOrdersServiceCore_Tests
{
    public class OrderChecker_UnitTest
    {
        OrderChecker _orderChecker;
        Mock<ILogger<OrderChecker>> _loggerMock;
        Mock<IDatabaseService> _databaseServiceMock;
        Mock<IStorageService> _storageServiceMock;
        Mock<IOrderService> _orderServiceMock;

        [SetUp]
        public void Setup()
        {
            var builder = new ConfigurationBuilder();
            buildConfig(builder);
            var configRoot = builder.Build();
            _loggerMock = new Mock<ILogger<OrderChecker>>();
            _databaseServiceMock = new Mock<IDatabaseService>();
            _storageServiceMock = new Mock<IStorageService>();
            _orderServiceMock = new Mock<IOrderService>();
            _orderChecker = new OrderChecker(_loggerMock.Object, configRoot, _databaseServiceMock.Object, _storageServiceMock.Object, _orderServiceMock.Object);
        }

        void buildConfig(IConfigurationBuilder builder)
        {
            builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "production"}.json", optional: true)
                .AddEnvironmentVariables();
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        [TestCase(8)]
        public async Task ProcessOrderResponse_ProperUpdate(int orderCount)
        {
            _storageServiceMock.Setup(r => r.GetFilesCount(It.IsAny<string>(), "SH*")).Returns(orderCount);
            var fileNames = new List<string>();
            for(int i =0; i<orderCount;i++)
            {
                string fileName = $"SH0000{i}.csv";
                fileNames.Add(fileName);
            }
            _storageServiceMock.Setup(r => r.GetFiles(It.IsAny<string>(), "SH*")).Returns(fileNames);

            Order order = new Order()
            {
                Id = 1,
                OrderSymbol = "ZMC_TEST",
                Status = OrderStatus.Created,
                ClientCompanyId = 12345,
                CreationDate = DateTime.Now,
                ModificationDate = DateTime.Now,
                FtpStatus = FtpStatus.NotSended,
                Comment = "Unit test order.",
                Wholesaler = 154432,
                Products = new List<Product>()
                {
                    new Product()
                    {
                        Id = 1,
                        ProductName = "Test product 1",
                        CentralIdentNumber = "11223344",
                        CompanyId = 14567,
                        Quantity = 43,
                        ProcessedQuantity = 0,
                    },
                    new Product()
                    {
                        Id = 2,
                        ProductName = "Test product 2",
                        CentralIdentNumber = "33117799",
                        CompanyId = 1432,
                        Quantity = 4,
                        ProcessedQuantity = 0,
                    },
                }
            };

            _orderServiceMock.Setup(r => r.ProcessOrderResponse(It.IsAny<string>())).Returns(order);
            _databaseServiceMock.Setup(r => r.CheckOrderStatus(It.IsAny<long>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(1));
            _databaseServiceMock.Setup(r => r.UpdateOrder(It.IsAny<Order>(), It.IsAny<CancellationToken>(), false)).Returns(Task.FromResult(true));

            CancellationToken token = new CancellationToken();
            await _orderChecker.ProcessOrderResponse(token);

            _loggerMock.Verify(
                m => m.Log(LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString().Contains("Order successfuly updated.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Exactly(orderCount)
            );
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        [TestCase(8)]
        public async Task ProcessOrderResponse_NoUpdateStatus(int orderCount)
        {
            _storageServiceMock.Setup(r => r.GetFilesCount(It.IsAny<string>(), "SH*")).Returns(orderCount);
            var fileNames = new List<string>();
            for (int i = 0; i < orderCount; i++)
            {
                string fileName = $"SH0000{i}.csv";
                fileNames.Add(fileName);
            }
            _storageServiceMock.Setup(r => r.GetFiles(It.IsAny<string>(), "SH*")).Returns(fileNames);

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
                Wholesaler = 154432,
                Products = new List<Product>()
                {
                    new Product()
                    {
                        Id = 1,
                        ProductName = "Test product 1",
                        CentralIdentNumber = "11223344",
                        CompanyId = 14567,
                        Quantity = 43,
                        ProcessedQuantity = 0,
                    },
                    new Product()
                    {
                        Id = 2,
                        ProductName = "Test product 2",
                        CentralIdentNumber = "33117799",
                        CompanyId = 1432,
                        Quantity = 4,
                        ProcessedQuantity = 0,
                    },
                }
            };

            _orderServiceMock.Setup(r => r.ProcessOrderResponse(It.IsAny<string>())).Returns(order);
            _databaseServiceMock.Setup(r => r.CheckOrderStatus(It.IsAny<long>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(3));
            _databaseServiceMock.Setup(r => r.UpdateOrder(It.IsAny<Order>(), It.IsAny<CancellationToken>(), false)).Returns(Task.FromResult(true));

            CancellationToken token = new CancellationToken();
            await _orderChecker.ProcessOrderResponse(token);

            _loggerMock.Verify(
                m => m.Log(LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString().Contains("Order doesn't have proper status for update.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Exactly(orderCount)
            ); ;
        }
    }
}
