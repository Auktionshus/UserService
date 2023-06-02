using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using UserService.Controllers;

namespace UserService.Test
{
    [TestFixture]
    public class UnitTest1
    {
        private Mock<ILogger<UserController>> _loggerMock;
        private Mock<IHttpClientFactory> _httpClientFactoryMock;
        private Mock<Environment> _environmentMock;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<UserController>>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        }

        [Test]
        public async Task CreateUser_ValidModel_ReturnsOkResult()
        {
            // Arrange
            var controller = new UserController(
                _loggerMock.Object,
                _environmentMock.Object,
                _httpClientFactoryMock.Object
            );
            var user = new Register
            {
                Email = "mateuszkubisiak@example.com",
                Password = "password",
            };

            // Act
            var result = await controller.CreateUser(user);

            // Assert
            Assert.IsInstanceOf<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        }

        // Add more test methods here for other scenarios
    }
}
