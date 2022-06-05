using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SupplyOrdersServiceCore.Domain.Enums;
using SupplyOrdersServiceCore.Domain.Models;
using SupplyOrdersServiceCore.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupplyOrdersServiceCore_Tests
{
    public class CsvOrderService_IntegrationTests
    {
        CsvOrderService _csvOrderCreator;
        Mock<ILogger<CsvOrderService>> _loggerMock;

        [SetUp]
        public void Setup()
        {
            var builder = new ConfigurationBuilder();
            buildConfig(builder);
            var configRoot = builder.Build();
            _loggerMock = new Mock<ILogger<CsvOrderService>>();
            _csvOrderCreator = new CsvOrderService(_loggerMock.Object, configRoot);
            if (!Directory.Exists("TestObjects"))
            {
                Directory.CreateDirectory("TestObjects");
            }
        }

        void buildConfig(IConfigurationBuilder builder)
        {
            builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "production"}.json", optional: true)
                .AddEnvironmentVariables()
                .AddUserSecrets<CsvOrderService_IntegrationTests>();
        }

        [Test]
        public void CreateOrder()
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
                Wholesaler = 154432,
                Products = new List<Product>()
                {
                    new Product()
                    {
                        Id = 1,
                        Name = "Test product 1",
                        CentralIdentNumber = "11223344",
                        CompanyId = 14567,
                        Quantity = 43,
                        ProcessedQuantity = 0
                    },
                    new Product()
                    {
                        Id = 2,
                        Name = "Test product 2",
                        CentralIdentNumber = "33117799",
                        CompanyId = 1432,
                        Quantity = 4,
                        ProcessedQuantity = 0
                    },
                }
            };

            bool created = _csvOrderCreator.CreateOrder(order);
            Assert.IsTrue(created);
        }

        [Test]
        public void ProcessOrderResponse()
        {
            Order order = _csvOrderCreator.ProcessOrderResponse("SH000001.csv");
            Assert.IsTrue(order != null);
        }
    }
}
