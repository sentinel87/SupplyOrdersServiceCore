using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PostgresNpgsqlProvider;
using PostgresDapperProvider;
using Serilog;
using SupplyOrdersServiceCore.Interfaces;
using SupplyOrdersServiceCore.Modules;
using SupplyOrdersServiceCore.Services;
using System;
using System.IO;
using System.Text;

namespace SupplyOrdersServiceCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder();
            buildConfig(builder);
            createLogger(builder);
            CreateHostBuilder(args).Build().Run();
        }

        private static void buildConfig(IConfigurationBuilder builder)
        {
            builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json"), optional: false, reloadOnChange: true)
                .AddJsonFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "production"}.json"), optional: true)
                .AddEnvironmentVariables();
        }

        private static void createLogger(IConfigurationBuilder builder)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Build())
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs/Log_.txt"), rollingInterval: RollingInterval.Day, shared: true, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }

        public static string DecryptString(string encoded)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i <= encoded.Length - 2; i += 2)
            {
                sb.Append(Convert.ToString(Convert.ToChar(Int32.Parse(encoded.Substring(i, 2),
                System.Globalization.NumberStyles.HexNumber))));
            }
            return sb.ToString();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                    services.AddSingleton<OrderCreator>();
                    services.AddSingleton<OrderChecker>();
                    services.AddSingleton<OrderConfirmationSender>();
                    services.AddSingleton<IOrderService, CsvOrderService>();
                    //services.AddNpgsqlDatabaseProvider();
                    services.AddDapperDatabaseProvider();
                    services.AddSingleton<IStorageService, StorageService>();
                    services.AddSingleton<IFtpService, FluentFtpService>();
                    services.AddSingleton<IExportService, DbfExportService>();
                    services.AddSingleton<IFtpService, FluentFtpService>();
                })
            .UseSerilog()
            .UseWindowsService();
    }
}
