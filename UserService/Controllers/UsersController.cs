using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace UserService.Controllers

{
    [Route("api/[controller]")]
    [ApiController]

    public class UserController : ControllerBase
    {
        private IGridFSBucket gridFS;
        private readonly ILogger<UserController> _logger;
        private readonly string _hostName;
        private readonly string _mongoDbConnectionString;

        public UserController(ILogger<UserController> logger)
        {
            _logger = logger;
            _mongoDbConnectionString = config["MongoDbConnectionString"];
            _hostName = config["HostnameRabbit"];
            _logger.LogInformation($"Connection: {_hostName}");
        }
        
        // Placeholder for the auction data storage
        private static readonly List<User> Users = new List<User>();

         // Image storage path
        private readonly string _imagePath = "Images";


        [HttpPost("create")]
        public async Task<IActionResult> CreateUser(User user)
         {
            if (user != null)
            {
                try
                {
                    // Opretter forbindelse til RabbitMQ
                    var factory = new ConnectionFactory { HostName = _hostName };

                    using var connection = factory.CreateConnection();
                    using var channel = connection.CreateModel();

                    channel.ExchangeDeclare(exchange: "topic_fleet", type: ExchangeType.Topic);

                    // Serialiseres til JSON
                    string message = JsonSerializer.Serialize(user);

                    // Konverteres til byte-array
                    var body = Encoding.UTF8.GetBytes(message);

                    // Sendes til kø
                    channel.BasicPublish(
                        exchange: "topic_fleet",
                        routingKey: "users.create",
                        basicProperties: null,
                        body: body
                    );

                    _logger.LogInformation("User created and sent to RabbitMQ");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    return StatusCode(500);
                }
                return Ok(user);
            }
            else
            {
                return BadRequest("User object is null");
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListUsers()
        {
            MongoClient dbClient = new MongoClient(_mongoDbConnectionString);
            var collection = dbClient.GetDatabase("user").GetCollection<User>("users");
            var users = await collection.Find(_ => true).ToListAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            MongoClient dbClient = new MongoClient(_mongoDbConnectionString);
            var collection = dbClient.GetDatabase("user").GetCollection<User>("users");
            User user = await collection.Find(a => a.Id == id).FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound($"User with Id {id} not found.");
            }
            return Ok(user);
        }

        [HttpGet(Name = "GetUserSerivce")]
        public IEnumerable<User> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new User
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }


}


