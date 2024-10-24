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
using System.Xml.Linq;

namespace MyPortal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : Controller
    {

        private readonly UserServices _userService;
        public readonly AccountServices _accountServices;
        protected readonly DatabaseContext _context;

        public AccountController(UserServices userService, AccountServices accountServices, DatabaseContext context)
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
            return await _userService.LoginUser(loginDTO.Email, loginDTO.Password);
        }
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> AddUser([FromBody] UserDetailsDTO userDetailsDTO)
        {
            return await _accountServices.AddUSer(userDetailsDTO);
        }
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> ForgetPassword([FromBody] ForgotPassword forgotPassword)
        {
            if(string.IsNullOrEmpty(forgotPassword.Email))
            {
                return BadRequest(new { success = false, message = "Email is required" });
            }
            else 
            {
                return await _accountServices.TemporaryPassword(forgotPassword.Email);
            }
        }
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePassword changePassword)
        {
            if(string.IsNullOrEmpty(changePassword.Email) || string.IsNullOrEmpty(changePassword.Password))
            {
                return BadRequest(new { success = false, message = "Email and password is required" });
            }
            else 
            {
                return await _accountServices.ChangePassword(changePassword.Email, changePassword.Password);
            }
        }



    }
}
public class ForgotPassword
{
    public string Email { get; set; }
}
public class ChangePassword
{
    public string Email { get; set; }
    public string Password { get; set; }
}
