using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MyPortal.Data;
using MyPortal.DTO;
using MyPortal.Models;

namespace MyPortal.Services
{

    public class AccountServices
    {
        private readonly DatabaseContext _context;
        private readonly UserServices _userService;
        public AccountServices(DatabaseContext context, UserServices userService)
        {
            _userService = userService;
            _context = context;
        }

        public async Task<IActionResult> AddUSer(UserDetailsDTO userDetails)
        {
            if (userDetails == null)
            {
                return new BadRequestObjectResult("Enter user details");
            }
            if (_context.Database.CanConnect())
            {
                Console.WriteLine("Connection successful!");
            }
            else
            {
                Console.WriteLine("Failed to connect to the database.");
            }
            var User = new ClientUser()
            {
                No = userDetails.No,
                Name = userDetails.FirstName + " " + userDetails.LastName,
                Email = userDetails.Email,
                PhoneNumber = userDetails.PhoneNumber,
                Password = userDetails.Password,
            };
            _context.ClientUser.Add(User);
            _context.SaveChanges();

            return new NoContentResult();
        }

        // public async Task<IActionResult> ChangePassword(string email, string password)
        // {

        // }

        public async Task<IActionResult> TemporaryPassword(string email)
        {

            bool IsUserExist = await _userService.CheckUSerExist(email);
            if (IsUserExist)
            {
                var Temp = await _userService.GetTempPassword(email);
                if (!Temp)
                {
                    return new BadRequestObjectResult(new { success = false, message = "Error generating temporary password or email not found." });
                }
                else
                {
                    return new OkObjectResult(new { success = true, message = "A temporary password has been sent to your email." });
                }
            }
            else
            {
                return new BadRequestObjectResult("Enter business central email id to change password");
            }
        }

        public async Task<IActionResult> ChangePassword(string email, string password)
        {
            var Temp = await _userService.GetChangePassword(email, password);
            if (!Temp)
            {
                return new BadRequestObjectResult(new { success = false, message = "Error generating temporary password or email not found." });
            }
            else
            {
                return new OkObjectResult(new { success = true, message = "A password has been changed." });
            }
        }
    }
}