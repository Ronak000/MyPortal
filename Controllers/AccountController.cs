using Microsoft.AspNetCore.Mvc;
using MyPortal.Models;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.OData.Buffers;
using Microsoft.Identity.Client;
using NAV;
using MyPortal.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using MyPortal.DTO;
using MyPortal.Data;

namespace MyPortal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : Controller
    {
        private static string[] Scopes_one = new[] { "https://api.businesscentral.dynamics.com/.default" };
        private readonly UserServices _userService;
        public readonly AccountServices _accountServices;
        protected readonly DatabaseContext _context;

        public AccountController( UserServices userService, AccountServices accountServices, DatabaseContext context)
        {
            _accountServices = accountServices;
            _userService = userService;
            _context = context;
        }

        [HttpGet]
        [Route("[action]")]
        public IActionResult Get()
        {
            return Ok(new { message = "Hello, Swagger!" });
        }
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
        {
            // Validate the user against the cached data
            return await _userService.ValidateUser(loginDTO.Email, loginDTO.Password);
        }
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> AddUser([FromBody] UserDetailsDTO userDetailsDTO)
        {
            try
            {
                if (_context.Database.CanConnect())
                {
                    Console.WriteLine("Database connection successful.");
                }
                else
                {
                    Console.WriteLine("Failed to connect to the database.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
            }
            // Validate the user against the cached data
            return await _accountServices.AddUSer(userDetailsDTO);
        }



    }
}
