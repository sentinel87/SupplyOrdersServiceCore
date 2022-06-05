using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SupplyOrdersServiceCore.Domain.Enums;
using SupplyOrdersServiceCore.Domain.Models;
using SupplyOrdersServiceCore.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupplyOrdersServiceCore.Services
{
    public class CsvOrderService: IOrderService
    {
        private readonly ILogger<CsvOrderService> _logger;
        private readonly IConfiguration _configuration;
        public CsvOrderService(ILogger<CsvOrderService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public bool CreateOrder(Order order)
        {
            try
            {
                var orderQueuePath = _configuration.GetSection("Parameters").GetValue<string>("OrderQueuePath");
                string fileName = createFileName(order.Id.ToString());
                string filePath = Path.Combine(orderQueuePath, fileName);
                StringBuilder builder = new StringBuilder();
                builder.AppendLine($"{order.Id};{order.OrderSymbol};{order.ClientCompanyId};;{DateTime.Now.ToString("yyyy-MM-dd")};Supply;");
                foreach (Product product in order.Products)
                {
                    builder.AppendLine($"{product.Id};{product.CompanyId};{product.Quantity};;;");
                }
                File.WriteAllText(filePath, builder.ToString());
                order.OrderFile = fileName;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occured during csv file creation: {ex.Message}");
                return false;
            }
        }

        public Order ProcessOrderResponse(string fileName)
        {
            var orderResponsePath = _configuration.GetSection("Parameters").GetValue<string>("OrderResponsePath");
            Order order = new Order();
            order.Products = new List<Product>();
            string filePath = Path.Combine(orderResponsePath, fileName);
            try
            {
                string closeSymbol = "";
                using (StreamReader reader = new StreamReader(filePath))
                {
                    string line = "";
                    bool firstLine = false;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] parts = line.Split(';');
                        if (parts.Length > 0)
                        {
                            if (firstLine == false) // Czytamy nagłówek
                            {
                                order.Id = Int64.Parse(parts[0]);
                                order.OrderSymbol = parts[1];
                                order.ClientCompanyId = Int32.Parse(parts[2]);
                                closeSymbol = parts[3];
                                order.Comment = parts[4];
                                order.ResponseFile = fileName;
                                firstLine = true;
                                _logger.LogInformation($"Order response {order.OrderSymbol} for client {order.ClientCompanyId}");
                            }
                            else // Czytamy pozycje
                            {
                                Product product = new Product();
                                product.Id = Int64.Parse(parts[0]);
                                product.CompanyId = Int32.Parse(parts[1]);
                                if (Double.TryParse(parts[2].Replace('.', ','), out double quantity))
                                    product.Quantity = (int)quantity;
                                else
                                    product.Quantity = 0;
                                if (Double.TryParse(parts[3].Replace('.', ','), out double pQuantity))
                                    product.ProcessedQuantity = (int)pQuantity;
                                else
                                    product.ProcessedQuantity = 0;
                                order.Products.Add(product);
                            }
                        }
                    }
                    reader.Close();
                }
                if (!String.IsNullOrEmpty(closeSymbol))
                {
                    if (closeSymbol.Trim().ToLower() == "cpl")
                        order.Status = OrderStatus.Processed;
                }
                else
                    order.Status = getOrderStatus(fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occured during response file processing: {ex.Message}");
                return null;
            }
            _logger.LogInformation($"Order response acquired.");

            return order;
        }

        private string createFileName(string orderNumber)
        {
            string fileName = "ORD";
            if (orderNumber.Length < 6)
            {
                int zeroSpaces = 6 - orderNumber.Length;
                for (int i = 0; i < zeroSpaces; i++)
                    fileName += "0";
            }
            fileName += $"{orderNumber}.csv";
            return fileName;
        }

        private OrderStatus getOrderStatus(string fileName)
        {
            OrderStatus status = OrderStatus.Error;
            if (fileName.Contains("_REG.csv"))
                status = OrderStatus.Processing;
            else if (fileName.Contains("_CPL.csv"))
                status = OrderStatus.Processed;
            else
                status = OrderStatus.Error;

            return status;
        }
    }
}
