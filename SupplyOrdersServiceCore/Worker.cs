using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SupplyOrdersServiceCore.Interfaces;
using SupplyOrdersServiceCore.Modules;
using SupplyOrdersServiceCore.Domain.Interfaces;

namespace SupplyOrdersServiceCore
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly IDatabaseService _databaseService;
        private readonly OrderCreator _orderCreator;
        private readonly OrderChecker _orderChecker;
        private readonly OrderConfirmationSender _orderConfirmationSender;

        public Worker(ILogger<Worker> logger, IDatabaseService databaseService, IConfiguration configuration, OrderCreator orderCreator, OrderChecker orderChecker, OrderConfirmationSender orderConfirmationSender)
        {
            _logger = logger;
            _databaseService = databaseService;
            _configuration = configuration;
            _orderCreator = orderCreator;
            _orderChecker = orderChecker;
            _orderConfirmationSender = orderConfirmationSender;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = _configuration.GetSection("Parameters").GetValue<int>("Interval");
            var disableCreationService = _configuration.GetSection("Parameters").GetValue<bool>("DisableCreationService");
            var disableCheckingService = _configuration.GetSection("Parameters").GetValue<bool>("DisableCheckingService");
            var disableFtpService = _configuration.GetSection("Parameters").GetValue<bool>("DisableFtpService");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Starting process...");
                await _databaseService.OpenConnection(stoppingToken);
                if(!disableCreationService)
                {
                    await _orderCreator.ProcessOrders(stoppingToken);
                }
                else
                    _logger.LogInformation("Skipped Order Creator.");
                if (!disableCheckingService)
                {
                    await _orderChecker.ProcessOrderResponse(stoppingToken);
                }
                else
                    _logger.LogInformation("Skipped Order Checker");
                if(!disableFtpService)
                {
                    await _orderConfirmationSender.ProcessPreparedOrders(stoppingToken);
                }
                else
                    _logger.LogInformation("Skipped Order Confirmation Sender.");
                await _databaseService.CloseConnection();
                _logger.LogInformation("Process completed.");
                _logger.LogInformation("---------------------------------------------------------------------------------------------");
                await Task.Delay(interval, stoppingToken);
            }
        }
    }
}
