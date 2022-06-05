using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
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
    public class FluentFtpService_IntegrationTests
    {
        Mock<ILogger<FluentFtpService>> _loggerMock;
        FluentFtpService _fluentFtpService;
        CancellationToken _stoppingToken;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<FluentFtpService>>();
            var builder = new ConfigurationBuilder();
            buildConfig(builder);
            var configRoot = builder.Build();
            _fluentFtpService = new FluentFtpService(_loggerMock.Object, configRoot);
            _stoppingToken = new CancellationToken();
        }

        private void buildConfig(IConfigurationBuilder builder)
        {
            builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "production"}.json", optional: true)
                .AddEnvironmentVariables();
        }

        [Test]
        public async Task OpenFtpConnection()
        {
            bool connected = false;
            bool disconnected = false;
            await _fluentFtpService.OpenConnection(_stoppingToken);
            connected = _fluentFtpService.IsConnected();
            await _fluentFtpService.CloseConnection();
            disconnected = !_fluentFtpService.IsConnected();

            Assert.IsTrue(connected);
            Assert.IsTrue(disconnected);
        }

        [Test]
        public async Task CheckFtpDirectoryExist()
        {
            bool connected = false;
            bool disconnected = false;
            await _fluentFtpService.OpenConnection(_stoppingToken);
            connected = _fluentFtpService.IsConnected();
            await _fluentFtpService.CheckFtpDirectoryExist("test_test", _stoppingToken);
            await _fluentFtpService.CloseConnection();
            disconnected = !_fluentFtpService.IsConnected();

            Assert.IsTrue(connected);
            Assert.IsTrue(disconnected);
            _loggerMock.Verify(m => m.Log(LogLevel.Error, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString().Contains("Error during ftp directory validation")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never);
        }
    }
}
