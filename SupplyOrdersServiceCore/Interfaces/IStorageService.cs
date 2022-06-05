using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupplyOrdersServiceCore.Interfaces
{
    public interface IStorageService
    {
        int GetFilesCount(string sourcePath, string keyPhrase);
        List<string> GetFiles(string sourcePath, string keyPhrase);
        bool MoveFiles(string sourcePath, string destinationPath, string sourceFileName, string destinationFileName);
        bool ClearDir(string path);
        bool CreateZip(string sourcePath, string destinationPath);
        bool CheckIfDirectoryExist(string path);
        bool CreateTextFile(string filePath, string content);
    }
}
