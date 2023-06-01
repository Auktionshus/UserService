using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.Net.Http;
using Newtonsoft.Json;

namespace UserService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly string _mongoDbConnectionString;

        public UserController(ILogger<UserController> logger, Environment secrets)
        {
            try
            {
                _mongoDbConnectionString = secrets.dictionary["ConnectionString"];

                _logger = logger;
                _logger.LogInformation($"MongoDbConnectionString: {_mongoDbConnectionString}");
            }
            catch (Exception e)
            {
                _logger.LogError($"Error getting environment variables{e.Message}");
            }
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateUser([FromBody] Register model)
        {
            try
            {
                MongoClient dbClient = new MongoClient(_mongoDbConnectionString);
                var collection = dbClient.GetDatabase("User").GetCollection<User>("Users");

                _logger.LogInformation($"User with email: {model.Email} recieved");
                if (string.IsNullOrWhiteSpace(model.Password))
                    return BadRequest("Password is required");

                if (
                    await collection.Find<User>(x => x.Email == model.Email).FirstOrDefaultAsync()
                    != null
                )
                    return BadRequest("Email \"" + model.Email + "\" is already taken");

                byte[] passwordHash,
                    passwordSalt;
                CreatePasswordHash(model.Password, out passwordHash, out passwordSalt);

                User user = new User
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt
                };

                await collection.InsertOneAsync(user);

                return Ok(user);
            }
            catch
            {
                _logger.LogInformation(
                    $"An error occurred while trying to create user with email: {model.Email}"
                );
                return BadRequest();
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListUsers()
        {
            MongoClient dbClient = new MongoClient(_mongoDbConnectionString);
            var collection = dbClient.GetDatabase("User").GetCollection<User>("Users");
            _logger.LogInformation("Geting UserList");
            var users = await collection.Find(_ => true).ToListAsync();
            return Ok(users);
        }

        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            MongoClient dbClient = new MongoClient(_mongoDbConnectionString);
            var collection = dbClient.GetDatabase("User").GetCollection<User>("Users");
            User user = await collection.Find(u => u.Id == id).FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound($"User with Id {id} not found.");
            }
            return Ok(user);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            MongoClient dbClient = new MongoClient(_mongoDbConnectionString);
            var collection = dbClient.GetDatabase("User").GetCollection<User>("Users");
            var result = await collection.DeleteOneAsync(u => u.Id == id);

            if (result.DeletedCount == 0)
            {
                return NotFound($"User with Id {id} not found.");
            }

            return Ok($"User with Id {id} has been deleted.");
        }

        private void CreatePasswordHash(
            string password,
            out byte[] passwordHash,
            out byte[] passwordSalt
        )
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        [HttpGet("version")]
        public IEnumerable<string> Get()
        {
            var properties = new List<string>();
            var assembly = typeof(Program).Assembly;
            foreach (var attribute in assembly.GetCustomAttributesData())
            {
                _logger.LogInformation("Tilføjer " + attribute.AttributeType.Name);
                properties.Add($"{attribute.AttributeType.Name} - {attribute.ToString()}");
            }
            return properties;
        }
    }
}
