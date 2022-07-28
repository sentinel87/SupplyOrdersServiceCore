using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using SupplyOrdersServiceCore.Domain.Enums;
using SupplyOrdersServiceCore.Domain.Interfaces;
using SupplyOrdersServiceCore.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PostgresDapperProvider
{
    public class DapperDatabaseService : IDatabaseService
    {
        private readonly ILogger<DapperDatabaseService> _logger;
        private readonly IConfiguration _configuration;
        private NpgsqlConnection _connection;
        public DapperDatabaseService(ILogger<DapperDatabaseService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            setupConnection();
        }

        private void setupConnection()
        {
            try
            {
                string server = _configuration.GetSection("DatabaseConnection").GetValue<string>("Server");
                int port = _configuration.GetSection("DatabaseConnection").GetValue<int>("Port");
                string database = _configuration.GetSection("DatabaseConnection").GetValue<string>("Database");
                string dbLogin = DecryptString(_configuration.GetSection("DatabaseConnection").GetValue<string>("User"));
                string dbPass = DecryptString(_configuration.GetSection("DatabaseConnection").GetValue<string>("Pass"));
                string dbConnectionString = $"Server={server};Port={port};Database={database};User id={dbLogin};Password={dbPass}";
                _connection = new NpgsqlConnection(dbConnectionString);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during DB initialization: {ex.Message}");
            }

        }

        public async Task<int> CheckOrderStatus(long id, CancellationToken stoppingToken)
        {
            try
            {
                var parameters = new { Id = id };
                var sql = $"SELECT status from orders where id_order = @Id";
                var status = await _connection.ExecuteScalarAsync<int>(sql, parameters);
                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during order status check: {ex.Message}");
                return -2;
            }
        }

        public async Task CloseConnection()
        {
            if (_connection.State == System.Data.ConnectionState.Open)
            {
                await _connection.CloseAsync();
            }
        }

        public async Task<List<Product>> GetConfirmedOrderPositions(long orderId, CancellationToken stoppingToken)
        {
            try
            {
                var parameters = new { Id = orderId };
                var sql = $@"SELECT id_product Id,
                                    name ProductName,
                                    central_ident_number CentralIdentNumber,
                                    company_id CompanyId,
                                    quantity Quantity,
                                    processed_quantity ProcessedQuantity  
                            FROM products 
                            WHERE order_fk = @Id and processed_quantity > 0";
                var result = await _connection.QueryAsync<Product>(sql, parameters);
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during retrieving confirmed order positions from DB: {ex.Message}");
                return null;
            }
        }

        public async Task<string> GetFtpLocation(int id, CancellationToken stoppingToken)
        {
            try
            {
                var parameters = new { Id = id };
                var sql = $"SELECT ftp_directory FROM client_ftp_info WHERE client_company_id = @Id";
                var location = await _connection.ExecuteScalarAsync<string>(sql, parameters);
                return location;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during retrieving ftp directory from DB: {ex.Message}");
                return "error";
            }
        }

        public async Task<List<Product>> GetOrderPositions(long orderId, CancellationToken stoppingToken)
        {
            try
            {
                var parameters = new { Id = orderId };
                var sql = $@"SELECT id_product Id,
                                    name ProductName,
                                    central_ident_number CentralIdentNumber,
                                    company_id CompanyId,
                                    quantity Quantity,
                                    processed_quantity ProcessedQuantity  
                            FROM products 
                            WHERE order_fk = @Id and quantity > 0";
                var result = await _connection.QueryAsync<Product>(sql, parameters);
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during retrieving order positions from DB: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Order>> GetOrders(OrderStatus status, CancellationToken stoppingToken)
        {
            try
            {
                var parameters = new { Status = (int)status };
                var sql = $@"SELECT id_order Id,
                                    status Status,
                                    order_symbol OrderSymbol,
                                    order_file OrderFile,
                                    response_file ResponseFile,
                                    creation_date CreationDate,
                                    modification_date ModificationDate,
                                    client_company_id ClientCompanyId,
                                    ftp_status FtpStatus,
                                    wholesaler Wholesaler 
                            FROM orders 
                            WHERE status = @Status";
                var result = await _connection.QueryAsync<Order>(sql, parameters);
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during retrieving orders from DB: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Order>> GetOrdersToConfirm(CancellationToken stoppingToken)
        {
            try
            {
                var sql = $@"SELECT id_order Id,
                                    status Status,
                                    order_symbol OrderSymbol,
                                    order_file OrderFile,
                                    response_file ResponseFile,
                                    creation_date CreationDate,
                                    modification_date ModificationDate,
                                    client_company_id ClientCompanyId,
                                    ftp_status FtpStatus,
                                    wholesaler Wholesaler 
                            FROM orders 
                            WHERE status IN (2,3)  
                            AND ftp_status = 0";
                var result = await _connection.QueryAsync<Order>(sql);
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during retrieving orders to confirmation from DB: {ex.Message}");
                return null;
            }
        }

        public bool IsConnected()
        {
            if (_connection.State == System.Data.ConnectionState.Open)
                return true;
            else
                return false;
        }

        public async Task OpenConnection(CancellationToken stoppingToken)
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                try
                {
                    await _connection.OpenAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Cannot open DB connection: {ex.Message}");
                }
            }
        }

        public async Task<bool> SetFtpConfirmationFlag(long orderId, FtpStatus status, CancellationToken stoppingToken)
        {
            try
            {
                var parameters = new { Id = orderId, Status = (int)status };
                var sql = $"UPDATE orders SET ftp_status = @Status where id = @Id";
                await _connection.ExecuteAsync(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during setting confirmation flag in DB: {ex.Message}");
                return false;
            }
            return true;
        }

        public async Task<bool> SetFtpFileName(long orderId, string fileName, CancellationToken stoppingToken)
        {
            try
            {
                var parameters = new { FileName = fileName, Id = orderId };
                var sql = $"UPDATE orders SET ftp_file = @FileName where id = @Id";
                await _connection.ExecuteAsync(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during setting confirmation file name in DB: {ex.Message}");
                return false;
            }
            return true;
        }

        public async Task<bool> SetOrderComment(string comment, long id, CancellationToken stoppingToken)
        {
            try
            {
                var parameters = new { Comment = comment, Id = id };
                var sql = $"UPDATE orders SET comment = @Comment where id = @Id";
                await _connection.ExecuteAsync(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during setting order comment in DB: {ex.Message}");
                return false;
            }
            return true;
        }

        public async Task<bool> UpdateOrder(Order order, CancellationToken stoppingToken, bool fileCreation = true)
        {
            try
            {
                if (fileCreation == true)
                {
                    var parameters = new { Status = (int)order.Status, OrderFile = order.OrderFile, ModificationDate = order.ModificationDate, Id = order.Id };
                    var sql = $@"
                    UPDATE orders SET 
                        status=@Status,
                        order_file=@OrderFile,
                        modification_date=@ModificationDate 
                        WHERE id_order=@Id";
                    await _connection.ExecuteAsync(sql, parameters);
                }
                else
                {
                    var parameters = new { Status = (int)order.Status, ResponseFile = order.ResponseFile, ModificationDate = order.ModificationDate, Comment = order.Comment, Id = order.Id };
                    var sql = $@"
                    UPDATE orders SET 
                        status=@Status,
                        response_file=@ResponseFile,
                        modification_date=@ModificationDate,
                        comment=@Comment 
                        WHERE id_order=@Id";
                    await _connection.ExecuteAsync(sql, parameters);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during order update in DB: {ex.Message}");
                return false;
            }
            return true;
        }

        public async Task<bool> UpdateOrderPositionProcessedQuantity(Product product, CancellationToken stoppingToken)
        {
            try
            {
                var parameters = new { ProcessedQuantity = product.ProcessedQuantity, Id = product.Id };
                var sql = $@"UPDATE products SET processed_quantity = @ProcessedQuantity WHERE id_product = @Id";
                await _connection.ExecuteAsync(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during order position update in DB: {ex.Message}");
                return false;
            }
            return true;
        }

        public async Task<bool> UpdateOrderStatus(OrderStatus status, long id, CancellationToken stoppingToken)
        {
            try
            {
                var parameters = new { Status = (int)status, ModificationDate = DateTime.Now, Id = id };
                var sql = $@"UPDATE orders SET status = @Status, modification_date = @ModificationDate where id_order = @Id";
                await _connection.ExecuteAsync(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during order status update in DB: {ex.Message}");
                return false;
            }
            return true;
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
    }
}
