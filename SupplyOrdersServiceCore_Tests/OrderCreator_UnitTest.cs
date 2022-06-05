using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SupplyOrdersServiceCore.Domain.Enums;
using SupplyOrdersServiceCore.Domain.Models;
using SupplyOrdersServiceCore.Interfaces;
using SupplyOrdersServiceCore.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SupplyOrdersServiceCore_Tests
{
    public class OrderCreator_UnitTest
    {
        OrderCreator _orderCreator;
        Mock<ILogger<OrderCreator>> _loggerMock;
        Mock<IDatabaseService> _databaseServiceMock;
        Mock<IOrderService> _orderServiceMock;

        [SetUp]
        public void Setup()
        {
            var builder = new ConfigurationBuilder();
            var configRoot = builder.Build();
            _loggerMock = new Mock<ILogger<OrderCreator>>();
            _databaseServiceMock = new Mock<IDatabaseService>();
            _orderServiceMock = new Mock<IOrderService>();
            _orderCreator = new OrderCreator(_loggerMock.Object, _orderServiceMock.Object, _databaseServiceMock.Object);
        }

        [Test]
        public async Task ProcessOrdersFull()
        {
            _databaseServiceMock.Setup(r => r.GetOrders(It.IsAny<OrderStatus>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new List<Order>() { new Order {
                Id = 1,
                OrderSymbol = "ZMC_TEST",
                Status = OrderStatus.Created,
                ClientCompanyId = 12345,
                CreationDate = DateTime.Now,
                ModificationDate = DateTime.Now,
                FtpStatus = FtpStatus.NotSended,
                Comment = "Unit test order.",
                Wholesaler = 154432
            }
            }));

            _databaseServiceMock.Setup(r => r.GetOrderPositions(It.IsAny<long>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new List<Product>() { 
                new Product()
                    {
                        Id = 1,
                        Name = "Test product 1",
                        CentralIdentNumber = "11223344",
                        CompanyId = 14567,
                        Quantity = 43,
                        ProcessedQuantity = 0,
                    },
                    new Product()
                    {
                        Id = 2,
                        Name = "Test product 2",
                        CentralIdentNumber = "33117799",
                        CompanyId = 1432,
                        Quantity = 4,
                        ProcessedQuantity = 0,
                    },
            }));

            _databaseServiceMock.Setup(r => r.UpdateOrder(It.IsAny<Order>(), It.IsAny<CancellationToken>(), true)).Returns(Task.FromResult(true));
            _orderServiceMock.Setup(r => r.CreateOrder(It.IsAny<Order>())).Returns(true);

            CancellationToken token = new CancellationToken();
            await _orderCreator.ProcessOrders(token);

            _loggerMock.Verify(
                m => m.Log(LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString().Contains("Error occured")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never
            );
        }

        [Test]
        public async Task ProcessOrdersWithoutProducts()
        {
            _databaseServiceMock.Setup(r => r.GetOrders(It.IsAny<OrderStatus>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new List<Order>() { new Order {
                Id = 1,
                OrderSymbol = "ZMC_TEST",
                Status = OrderStatus.Created,
                ClientCompanyId = 12345,
                CreationDate = DateTime.Now,
                ModificationDate = DateTime.Now,
                FtpStatus = FtpStatus.NotSended,
                Comment = "Unit test order.",
                Wholesaler = 154432
            }
            }));

            _databaseServiceMock.Setup(r => r.GetOrderPositions(It.IsAny<long>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new List<Product>()));
            _databaseServiceMock.Setup(r => r.UpdateOrderStatus(It.IsAny<OrderStatus>(), It.IsAny<long>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _databaseServiceMock.Setup(r => r.SetOrderComment(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));

            CancellationToken token = new CancellationToken();
            await _orderCreator.ProcessOrders(token);

            _loggerMock.Verify(
                m => m.Log(LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString().Contains("Error occured")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never
            );
        }

        [Test]
        public async Task ProcessOrdersWithoutProductIds()
        {
            _databaseServiceMock.Setup(r => r.GetOrders(It.IsAny<OrderStatus>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new List<Order>() { new Order {
                Id = 1,
                OrderSymbol = "ZMC_TEST",
                Status = OrderStatus.Created,
                ClientCompanyId = 12345,
                CreationDate = DateTime.Now,
                ModificationDate = DateTime.Now,
                FtpStatus = FtpStatus.NotSended,
                Comment = "Unit test order.",
                Wholesaler = 154432
            }
            }));

            _databaseServiceMock.Setup(r => r.GetOrderPositions(It.IsAny<long>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new List<Product>() {
                new Product()
                    {
                        Id = 1,
                        Name = "Test product 1",
                        CentralIdentNumber = "11223344",
                        CompanyId = 0,
                        Quantity = 43,
                        ProcessedQuantity = 0,
                    },
                    new Product()
                    {
                        Id = 2,
                        Name = "Test product 2",
                        CentralIdentNumber = "33117799",
                        CompanyId = 0,
                        Quantity = 4,
                        ProcessedQuantity = 0,
                    },
            }));

            _databaseServiceMock.Setup(r => r.UpdateOrderStatus(It.IsAny<OrderStatus>(), It.IsAny<long>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _databaseServiceMock.Setup(r => r.SetOrderComment(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));

            CancellationToken token = new CancellationToken();
            await _orderCreator.ProcessOrders(token);

            _loggerMock.Verify(
                m => m.Log(LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString().Contains("Error occured")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never
            );
        }

        [Test]
        public async Task ProcessOrdersWithNullableProductList()
        {
            _databaseServiceMock.Setup(r => r.GetOrders(It.IsAny<OrderStatus>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new List<Order>() { new Order {
                Id = 1,
                OrderSymbol = "ZMC_TEST",
                Status = OrderStatus.Created,
                ClientCompanyId = 12345,
                CreationDate = DateTime.Now,
                ModificationDate = DateTime.Now,
                FtpStatus = FtpStatus.NotSended,
                Comment = "Unit test order.",
                Wholesaler = 154432
            }
            }));

            _databaseServiceMock.Setup(r => r.GetOrderPositions(It.IsAny<long>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult<List<Product>>(null));
            _databaseServiceMock.Setup(r => r.UpdateOrderStatus(It.IsAny<OrderStatus>(), It.IsAny<long>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));

            CancellationToken token = new CancellationToken();
            await _orderCreator.ProcessOrders(token);

            _loggerMock.Verify(
                m => m.Log(LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString().Contains("Error occured")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never
            );
        }
    }
}
