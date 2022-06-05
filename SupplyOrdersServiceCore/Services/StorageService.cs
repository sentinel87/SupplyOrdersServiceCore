using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SupplyOrdersServiceCore.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupplyOrdersServiceCore.Services
{
    public class StorageService: IStorageService
    {
        private readonly ILogger<StorageService> _logger;
        public StorageService(ILogger<StorageService>logger)
        {
            _logger = logger;
        }

        public bool CheckIfDirectoryExist(string path)
        {
            try
            {
                return Directory.Exists(path);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during directory check: {ex.Message}");
                return false;
            }
        }

        public bool ClearDir(string path)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(path);

                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during directory temp cleanup: {ex.Message}");
                return false;
            }
            return true;
        }

        public bool CreateTextFile(string filePath, string content)
        {
            try
            {
                using (StreamWriter sw = File.CreateText(filePath))
                {
                    sw.WriteLine(content);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during text file creation: {ex.Message}");
                return false;
            }
            return true;
        }

        public bool CreateZip(string sourcePath, string destinationPath)
        {
            try
            {
                ZipFile.CreateFromDirectory(sourcePath, destinationPath);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during zip conversion: {ex.Message}");
                return false;
            }
            return true;
        }

        public List<string> GetFiles(string sourcePath, string keyPhrase)
        {
            try
            {
                IEnumerable<string> enumerableFiles = Directory.EnumerateFiles(sourcePath, keyPhrase, SearchOption.AllDirectories).OrderBy(File.GetCreationTime);
                List<string> fileNameList = enumerableFiles.Select(Path.GetFileName).ToList();
                return fileNameList;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during acquiring directory files {sourcePath}: {ex.Message}");
                return null;
            }
        }

        public int GetFilesCount(string sourcePath, string keyPhrase)
        {
            try
            {
                string[] files = Directory.GetFiles(sourcePath, keyPhrase);
                if (files.Length > 0)
                {
                    return files.Length;
                }
                else
                    return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during file counting from {sourcePath}: {ex.Message}");
                return -1;
            }
        }

        public bool MoveFiles(string sourcePath, string destinationPath, string sourceFileName, string destinationFileName)
        {
            try
            {
                File.Move(Path.Combine(sourcePath, sourceFileName), Path.Combine(destinationPath, destinationFileName));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during file from {sourcePath}: {ex.Message}");
                return false;
            }

            return true;
        }
    }
}
