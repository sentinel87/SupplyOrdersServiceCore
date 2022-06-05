using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SupplyOrdersServiceCore.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SupplyOrdersServiceCore_Tests
{
    public class StorageService_IntegrationTests
    {
        Mock<ILogger<StorageService>> _loggerMock;
        StorageService _storageService;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<StorageService>>();
            _storageService = new StorageService(_loggerMock.Object);
            if (!Directory.Exists("TestDir"))
            {
                Directory.CreateDirectory("TestDir");
            }
            if(!File.Exists("TestDir/Test.txt"))
                File.Create("TestDir/Test.txt");
            if (!File.Exists("TestDir/TextFile.ptr"))
                File.Create("TestDir/TextFile.ptr");
            if (!Directory.Exists("TestDirZip"))
            {
                Directory.CreateDirectory("TestDirZip");
            }
        }

        [Test]
        public void CheckIfDirectoryExist()
        {
            _storageService.CheckIfDirectoryExist("TestDir");
            _loggerMock.Verify(
                m => m.Log(LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString().Contains("Error during directory check")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never
            );
        }

        [Test]
        public void ClearDir()
        {
            bool result = _storageService.ClearDir("TestDirZip");
            Assert.IsTrue(result);
        }

        [Test]
        public void CreateZip()
        {
            bool result = _storageService.CreateZip("TestDir", "TestDirZip/Test.zip");
            Assert.IsTrue(result);
        }

        [Test]
        public void GetFiles()
        {
            var coll = _storageService.GetFiles("TestDir", "Test");
            Assert.IsTrue(coll != null);
        }

        [Test]
        public void GetFilesCount()
        {
            int count = _storageService.GetFilesCount("TestDir", "Test");
            Assert.IsTrue(count != -1);
        }

        [Test]
        public void CreateTextFile()
        {
            bool result = _storageService.CreateTextFile("TestDir/TextFile.ptr", "Start file...");
            Assert.IsTrue(result);
        }
    }
}
