using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using SupplyOrdersServiceCore.Domain.Enums;
using SupplyOrdersServiceCore.Domain.Interfaces;
using SupplyOrdersServiceCore.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PostgresNpgsqlProvider
{
    public class PostgresDatabaseService : IDatabaseService
    {
        private readonly ILogger<PostgresDatabaseService> _logger;
        private readonly IConfiguration _configuration;
        private NpgsqlConnection _connection;
        public PostgresDatabaseService(ILogger<PostgresDatabaseService> logger, IConfiguration configuration)
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

        public async Task CloseConnection()
        {
            if (_connection.State == System.Data.ConnectionState.Open)
            {
                await _connection.CloseAsync();
            }
        }

        public async Task<int> CheckOrderStatus(long id, CancellationToken stoppingToken)
        {
            try
            {
                using (NpgsqlCommand command = new NpgsqlCommand($"SELECT status from orders where id_order=:id", _connection))
                {
                    command.Parameters.Add(new NpgsqlParameter<Int64>(":id", NpgsqlDbType.Bigint) { TypedValue = id });

                    object val = await command.ExecuteScalarAsync(stoppingToken);
                    if (val != null)
                        return (int)val;
                    else
                        return -1;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during order status check: {ex.Message}");
                return -2;
            }
        }

        public async Task<List<Product>> GetConfirmedOrderPositions(long orderId, CancellationToken stoppingToken)
        {
            List<Product> products = new List<Product>();
            try
            {
                NpgsqlCommand command = new NpgsqlCommand(
                $@"SELECT id_product,
                        name,
                        central_ident_number
                        company_id,
                        quantity,
                        processed_quantity  
                   FROM products 
                        WHERE order_fk = :id and processed_quantity>0", _connection);
                command.Parameters.Add(new NpgsqlParameter<Int64>(":id", NpgsqlDbType.Bigint) { TypedValue = orderId });

                using (NpgsqlDataReader reader = await command.ExecuteReaderAsync(stoppingToken))
                {
                    while (await reader.ReadAsync(stoppingToken))
                    {
                        Product product = new Product();
                        product.Id = reader.GetInt64(0);
                        product.ProductName = reader.GetString(1);
                        product.CentralIdentNumber = reader.GetString(2);
                        product.CompanyId = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                        product.Quantity = reader.GetInt32(4);
                        product.ProcessedQuantity = reader.GetInt32(5);
                        products.Add(product);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during retrieving confirmed order positions from DB: {ex.Message}");
                products = null;
            }
            return products;
        }

        public async Task<string> GetFtpLocation(int id, CancellationToken stoppingToken)
        {
            string result = "";
            try
            {
                using (NpgsqlCommand command = new NpgsqlCommand($"SELECT ftp_directory FROM client_ftp_info WHERE client_company_id=:id", _connection))
                {
                    command.Parameters.Add(new NpgsqlParameter<int>(":id", NpgsqlDbType.Integer) { TypedValue = id });

                    object obj = await command.ExecuteScalarAsync(stoppingToken);
                    if (obj != null)
                        result = obj.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during retrieving ftp directory from DB: {ex.Message}");
                result = "error";
            }
            return result;
        }

        public async Task<List<Product>> GetOrderPositions(long orderId, CancellationToken stoppingToken)
        {
            List<Product> products = new List<Product>();
            try
            {
                NpgsqlCommand command = new NpgsqlCommand(
                $@"SELECT id_product,
                        name,
                        central_ident_number,
                        company_id,
                        quantity,
                        processed_quantity  
                   FROM products 
                        WHERE order_fk = :id and quantity>0", _connection);
                command.Parameters.Add(new NpgsqlParameter<Int64>(":id", NpgsqlDbType.Bigint) { TypedValue = orderId });

                using (NpgsqlDataReader reader = await command.ExecuteReaderAsync(stoppingToken))
                {
                    while (await reader.ReadAsync(stoppingToken))
                    {
                        Product product = new Product();
                        product.Id = reader.GetInt64(0);
                        product.ProductName = reader.GetString(1);
                        product.CentralIdentNumber = reader.GetString(2);
                        product.CompanyId = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                        product.Quantity = reader.GetInt32(4);
                        product.ProcessedQuantity = reader.GetInt32(5);
                        products.Add(product);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during retrieving order positions from DB: {ex.Message}");
                products = null;
            }
            return products;
        }

        public async Task<List<Order>> GetOrders(OrderStatus status, CancellationToken stoppingToken)
        {
            List<Order> Orders = new List<Order>();
            try
            {
                NpgsqlCommand command = new NpgsqlCommand(
                $@"SELECT id_order,
                        status,
                        order_symbol,
                        order_file,
                        response_file,
                        creation_date,
                        modification_date,
                        client_company_id,
                        ftp_status,
                        wholesaler 
                   FROM orders WHERE status=:status", _connection);
                command.Parameters.Add(new NpgsqlParameter<Int32>(":status", NpgsqlDbType.Integer) { TypedValue = (int)status });

                using (NpgsqlDataReader reader = await command.ExecuteReaderAsync(stoppingToken))
                {
                    while (await reader.ReadAsync(stoppingToken))
                    {
                        Order order = new Order();
                        order.Id = reader.GetInt64(0);
                        order.Status = (OrderStatus)reader.GetInt32(1);
                        order.OrderSymbol = reader.GetString(2);
                        order.OrderFile = reader.IsDBNull(3) ? null : reader.GetString(3);
                        order.ResponseFile = reader.IsDBNull(4) ? null : reader.GetString(4);
                        order.CreationDate = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5);
                        order.ModificationDate = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6);
                        order.ClientCompanyId = reader.GetInt32(7);
                        order.FtpStatus = (FtpStatus)reader.GetInt32(8);
                        order.Wholesaler = reader.IsDBNull(9) ? 0 : reader.GetInt32(9);
                        Orders.Add(order);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during retrieving orders from DB: {ex.Message}");
                Orders = null;
            }
            return Orders;
        }

        public async Task<List<Order>> GetOrdersToConfirm(CancellationToken stoppingToken)
        {
            List<Order> Orders = new List<Order>();
            try
            {
                NpgsqlCommand command = new NpgsqlCommand(
                $@"SELECT id_order,
                        status,
                        order_symbol,
                        order_file,
                        response_file,
                        creation_date,
                        modification_date,
                        client_company_id,
                        ftp_status,
                        wholesaler 
                   FROM orders 
                   WHERE status IN (2,3)  
                   AND ftp_status=0", _connection);

                using (NpgsqlDataReader reader = await command.ExecuteReaderAsync(stoppingToken))
                {
                    while (await reader.ReadAsync(stoppingToken))
                    {
                        Order order = new Order();
                        order.Id = reader.GetInt64(0);
                        order.Status = (OrderStatus)reader.GetInt32(1);
                        order.OrderSymbol = reader.GetString(2);
                        order.OrderFile = reader.IsDBNull(3) ? null : reader.GetString(3);
                        order.ResponseFile = reader.IsDBNull(4) ? null : reader.GetString(4);
                        order.CreationDate = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5);
                        order.ModificationDate = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6);
                        order.ClientCompanyId = reader.GetInt32(7);
                        order.FtpStatus = (FtpStatus)reader.GetInt32(8);
                        order.Wholesaler = reader.IsDBNull(9) ? 0 : reader.GetInt32(9);
                        Orders.Add(order);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during retrieving orders to confirmation from DB: {ex.Message}");
                Orders = null;
            }
            return Orders;
        }

        public async Task<bool> SetFtpConfirmationFlag(long orderId, FtpStatus status, CancellationToken stoppingToken)
        {
            try
            {
                using (NpgsqlCommand command = new NpgsqlCommand($"UPDATE orders SET ftp_status=:status where id=:id", _connection))
                {
                    command.Parameters.Add(new NpgsqlParameter<Int32>(":status", NpgsqlDbType.Integer) { TypedValue = (int)status });
                    command.Parameters.Add(new NpgsqlParameter<Int64>(":id", NpgsqlDbType.Bigint) { TypedValue = orderId });

                    await command.ExecuteNonQueryAsync(stoppingToken);
                }
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
                using (NpgsqlCommand command = new NpgsqlCommand($"UPDATE orders SET ftp_file=:file_name where id=:id", _connection))
                {
                    command.Parameters.Add(new NpgsqlParameter<string>(":file_name", NpgsqlDbType.Varchar) { TypedValue = fileName });
                    command.Parameters.Add(new NpgsqlParameter<Int64>(":id", NpgsqlDbType.Bigint) { TypedValue = orderId });

                    await command.ExecuteNonQueryAsync(stoppingToken);
                }
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
                using (NpgsqlCommand command = new NpgsqlCommand($"UPDATE orders SET comment=:comment where id=:id", _connection))
                {
                    command.Parameters.Add(new NpgsqlParameter<string>(":comment", NpgsqlDbType.Text) { TypedValue = comment });
                    command.Parameters.Add(new NpgsqlParameter<Int64>(":id", NpgsqlDbType.Bigint) { TypedValue = id });

                    await command.ExecuteNonQueryAsync(stoppingToken);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during setting order comment in DB: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateOrder(Order order, CancellationToken stoppingToken, bool fileCreation = true)
        {
            try
            {
                string commandString = $@"
                    UPDATE orders SET 
                        status=:status,
                        order_file=:order_file,
                        modification_date=:modification_date 
                        WHERE id_order=:id";
                if (fileCreation == false)
                {
                    commandString = $@"
                    UPDATE orders SET 
                        status=:status,
                        response_file=:response_file,
                        modification_date=:modification_date,
                        comment=:comment 
                        WHERE id_order=:id";
                }
                using (NpgsqlCommand command = new NpgsqlCommand(commandString, _connection))
                {
                    command.Parameters.Add(new NpgsqlParameter<Int32>(":status", NpgsqlDbType.Integer) { TypedValue = (int)order.Status });
                    if (fileCreation == true)
                        command.Parameters.Add(new NpgsqlParameter<string>(":order_file", NpgsqlDbType.Varchar) { TypedValue = order.OrderFile });
                    else
                    {
                        command.Parameters.Add(new NpgsqlParameter<string>(":response_file", NpgsqlDbType.Varchar) { TypedValue = order.ResponseFile });
                        command.Parameters.Add(new NpgsqlParameter<string>(":comment", NpgsqlDbType.Varchar) { TypedValue = order.Comment });
                    }
                    command.Parameters.Add(new NpgsqlParameter<DateTime>(":modification_date", NpgsqlDbType.Timestamp) { TypedValue = DateTime.Now });
                    command.Parameters.Add(new NpgsqlParameter<Int64>(":id", NpgsqlDbType.Bigint) { TypedValue = order.Id });

                    await command.ExecuteNonQueryAsync(stoppingToken);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during order update in DB: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateOrderPositionProcessedQuantity(Product product, CancellationToken stoppingToken)
        {
            try
            {
                using (NpgsqlCommand command = new NpgsqlCommand($@"
                    UPDATE products SET 
                        processed_quantity = :processed_quantity 
                    WHERE id_product = :id", _connection))
                {
                    command.Parameters.Add(new NpgsqlParameter<Int32>(":processed_quantity", NpgsqlDbType.Integer) { TypedValue = product.ProcessedQuantity });
                    command.Parameters.Add(new NpgsqlParameter<Int64>(":id", NpgsqlDbType.Bigint) { TypedValue = product.Id });

                    await command.ExecuteNonQueryAsync(stoppingToken);
                }
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
                using (NpgsqlCommand command = new NpgsqlCommand($"UPDATE orders SET status=:status, modification_date=:modification_date where id_order=:id", _connection))
                {
                    command.Parameters.Add(new NpgsqlParameter<Int32>(":status", NpgsqlDbType.Integer) { TypedValue = (int)status });
                    command.Parameters.Add(new NpgsqlParameter<DateTime>(":modification_date", NpgsqlDbType.Timestamp) { TypedValue = DateTime.Now });
                    command.Parameters.Add(new NpgsqlParameter<Int64>(":id", NpgsqlDbType.Bigint) { TypedValue = id });

                    await command.ExecuteNonQueryAsync(stoppingToken);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during order status update in DB: {ex.Message}");
                return false;
            }
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
