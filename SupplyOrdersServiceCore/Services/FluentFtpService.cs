using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SupplyOrdersServiceCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace SupplyOrdersServiceCore.Services
{
    public class FluentFtpService: IFtpService
    {
        private readonly ILogger<FluentFtpService> _logger;
        private readonly IConfiguration _configuration;
        FtpClient _client;

        public FluentFtpService(ILogger<FluentFtpService>logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            setupClient();
        }

        private void setupClient()
        {
            try
            {
                string host = _configuration.GetSection("FtpConnection").GetValue<string>("Host");
                string user = Program.DecryptString(_configuration.GetSection("FtpConnection").GetValue<string>("User"));
                string pass = Program.DecryptString(_configuration.GetSection("FtpConnection").GetValue<string>("Pass"));
                _client = new FtpClient(host, user, pass);
            }
            catch(Exception ex)
            {
                _logger.LogError($"Error occured during FTP client initialization: {ex.Message}");
            }
        }

        public bool IsConnected()
        {
            if (_client.IsConnected)
                return true;
            else
                return false;
        }

        public async Task OpenConnection(CancellationToken token)
        {
            if (!_client.IsConnected)
            {
                try
                {
                    await _client.ConnectAsync(token);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Cannot open FTP connection: {ex.Message}");
                }
            }
        }

        public async Task CloseConnection()
        {
            if (_client.IsConnected)
            {
                await _client.DisconnectAsync();
            }
        }

        public async Task<bool> CheckFtpDirectoryExist(string directoryName, CancellationToken token)
        {
            try
            {
                return await _client.DirectoryExistsAsync(directoryName, token);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during ftp directory validation: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CopyFileToFtp(string filePath, string ftpDirPath, CancellationToken token)
        {
            try
            {
                if (_client.IsConnected)
                {
                    FluentFTP.FtpStatus status = await _client.UploadFileAsync(filePath, ftpDirPath, FtpRemoteExists.Overwrite);
                    _logger.LogInformation($"FTP Status: {status.ToString()}. Created file: {ftpDirPath}.");
                    if (status == FluentFTP.FtpStatus.Success)
                        return true;
                    else
                        return false;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during moving file to FTP: {ex.Message}.");
                return false;
            }
        }
    }
}
