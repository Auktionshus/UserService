using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;

namespace UserService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly IMongoCollection<User> _users;

        public UserController(ILogger<UserController> logger)
        {
            _logger = logger;
            var client = new MongoClient("mongodb+srv://GroenOlsen:BhvQmiihJWiurl2V@auktionshusgo.yzctdhc.mongodb.net/?retryWrites=true&w=majority");
            var database = client.GetDatabase("User");
            _users = database.GetCollection<User>("Users");
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateUser(User user)
        {
            if (user != null)
            {
                try
                {
                    await _users.InsertOneAsync(user);
                    _logger.LogInformation("User created");
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
            var users = await _users.Find(_ => true).ToListAsync();
            return Ok(users);
        }

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
    }
}
