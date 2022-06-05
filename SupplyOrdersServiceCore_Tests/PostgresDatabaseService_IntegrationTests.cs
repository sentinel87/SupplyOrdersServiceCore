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
using System.Threading;
using System.Threading.Tasks;

namespace SupplyOrdersServiceCore_Tests
{
    public class PostgresDatabaseService_IntegrationTests
    {
        PostgresDatabaseService _postgresDatabaseService;
        Mock<ILogger<PostgresDatabaseService>> _loggerMock;
        CancellationToken _stoppingToken;

        [SetUp]
        public void Setup()
        {
            var builder = new ConfigurationBuilder();
            buildConfig(builder);
            var configRoot = builder.Build();
            _loggerMock = new Mock<ILogger<PostgresDatabaseService>>();
            _postgresDatabaseService = new PostgresDatabaseService(_loggerMock.Object, configRoot);
            _stoppingToken = new CancellationToken();
        }

        void buildConfig(IConfigurationBuilder builder)
        {
            builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "production"}.json", optional: true)
                .AddEnvironmentVariables()
                .AddUserSecrets<PostgresDatabaseService_IntegrationTests>();
        }

        [Test]
        public async Task CheckOrderStatus()
        {
            int status = -1;
            await _postgresDatabaseService.OpenConnection(_stoppingToken);
            status = await _postgresDatabaseService.CheckOrderStatus(0,_stoppingToken);
            await _postgresDatabaseService.CloseConnection();

            Assert.IsTrue(status != -2);
        }

        [Test]
        public async Task GetOrdersToConfirm()
        {
            List<Order> orders = null;
            await _postgresDatabaseService.OpenConnection(_stoppingToken);
            orders = await _postgresDatabaseService.GetOrdersToConfirm(_stoppingToken);
            await _postgresDatabaseService.CloseConnection();

            Assert.IsTrue(orders != null);
        }

        [Test]
        public async Task GetOrders()
        {
            List<Order> orders = null;
            await _postgresDatabaseService.OpenConnection(_stoppingToken);
            orders = await _postgresDatabaseService.GetOrders(OrderStatus.Registered, _stoppingToken);
            await _postgresDatabaseService.CloseConnection();

            Assert.IsTrue(orders != null);
        }

        [Test]
        public async Task GetConfirmedOrderPositions()
        {
            List<Product> products = null;
            await _postgresDatabaseService.OpenConnection(_stoppingToken);
            products = await _postgresDatabaseService.GetConfirmedOrderPositions(0, _stoppingToken);
            await _postgresDatabaseService.CloseConnection();

            Assert.IsTrue(products != null);
        }

        [Test]
        public async Task GetFtpLocation()
        {
            string location = "";
            await _postgresDatabaseService.OpenConnection(_stoppingToken);
            location = await _postgresDatabaseService.GetFtpLocation(543888, _stoppingToken);
            await _postgresDatabaseService.CloseConnection();

            Assert.IsTrue(location != "error");
        }
    }
}
