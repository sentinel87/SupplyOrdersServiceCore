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
    public class DbfExportService_IntegrationTests
    {
        DbfExportService _dbfExportService;
        Mock<ILogger<DbfExportService>> _loggerMock;

        [SetUp]
        public void Setup()
        {
            var builder = new ConfigurationBuilder();
            buildConfig(builder);
            var configRoot = builder.Build();
            _loggerMock = new Mock<ILogger<DbfExportService>>();
            _dbfExportService = new DbfExportService(_loggerMock.Object, configRoot);
        }

        void buildConfig(IConfigurationBuilder builder)
        {
            builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "production"}.json", optional: true)
                .AddEnvironmentVariables();
        }

        [Test]
        public void CreateOrderHeaderFile()
        {
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

            bool created = _dbfExportService.CreateOrderHeaderFile(order);
            Assert.IsTrue(created);
        }

        [Test]
        public void CreateOrderProductsFile()
        {
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
                        ProcessedQuantity = 0
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

            bool created = _dbfExportService.CreateOrderProductsFile(order);
            Assert.IsTrue(created);
        }

        [Test]
        public async Task CreateOrderExportFiles()
        {
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
                        ProcessedQuantity = 0
                    },
                    new Product()
                    {
                        Id = 2,
                        ProductName = "Test product 2",
                        CentralIdentNumber = "33117799",
                        CompanyId = 1432,
                        Quantity = 4,
                        ProcessedQuantity = 0
                    },
                }
            };

            bool created = await _dbfExportService.CreateOrderExportFiles(order);
            Assert.IsTrue(created);
        }
    }
}
