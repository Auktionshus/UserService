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
        private readonly IMongoCollection<User> _users;
        private readonly HttpClient _httpClient;

        public UserController(ILogger<UserController> logger, IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _httpClient = clientFactory.CreateClient();
            var client = new MongoClient(
                "mongodb+srv://GroenOlsen:BhvQmiihJWiurl2V@auktionshusgo.yzctdhc.mongodb.net/?retryWrites=true&w=majority"
            );
            var database = client.GetDatabase("User");
            _users = database.GetCollection<User>("Users");
        }

        /// <summary>
        /// Creates a new user
        /// </summary>
        /// <param name="model">Register</param>
        /// <returns>The created user</returns>
        [HttpPost("create")]
        public async Task<IActionResult> CreateUser([FromBody] Register model)
        {
            try
            {
                _logger.LogInformation($"User with email: {model.Email} recieved");
                if (string.IsNullOrWhiteSpace(model.Password))
                    return BadRequest("Password is required");

                if (
                    await _users.Find<User>(x => x.Email == model.Email).FirstOrDefaultAsync()
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

                await _users.InsertOneAsync(user);

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

        /// <summary>
        /// Gets a list of all users
        /// </summary>
        /// <returns>A list of all users</returns>
        [HttpGet("list")]
        public async Task<IActionResult> ListUsers()
        {
            _logger.LogInformation("Geting UserList");
            var users = await _users.Find(_ => true).ToListAsync();
            return Ok(users);
        }

        /// <summary>
        /// Gets a user by id
        /// </summary>
        /// <param name="id">User id</param>
        /// <returns>The user</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            User user = await _users.Find(u => u.Id == id).FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound($"User with Id {id} not found.");
            }
            return Ok(user);
        }

        /// <summary>
        /// Gets a user by id
        /// </summary>
        /// <param name="id">User id</param>
        /// <returns>The user</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var result = await _users.DeleteOneAsync(u => u.Id == id);

            if (result.DeletedCount == 0)
            {
                return NotFound($"User with Id {id} not found.");
            }

            return Ok($"User with Id {id} has been deleted.");
        }

        /// <summary>
        /// Creates hash and salt for password
        /// </summary>
        /// <param name="password">Password</param>
        /// <param name="passwordHash">Password hash</param>
        /// <param name="passwordSalt">Password salt</param>
        /// <returns></returns>
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

        /// <summary>
        /// Gets the version information of the service
        /// </summary>
        /// <returns>A list of version information</returns>
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
