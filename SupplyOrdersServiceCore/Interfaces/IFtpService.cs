using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SupplyOrdersServiceCore.Interfaces
{
    public interface IFtpService
    {
        bool IsConnected();
        public Task OpenConnection(CancellationToken token);
        public Task CloseConnection();
        public Task<bool> CheckFtpDirectoryExist(string directoryName, CancellationToken token);
        public Task<bool> CopyFileToFtp(string filePath, string ftpDirPath, CancellationToken token);
    }
}
