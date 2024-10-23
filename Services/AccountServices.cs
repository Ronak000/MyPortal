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
        public AccountServices(DatabaseContext context)
        {
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
                Console.WriteLine( "Connection successful!");
            }
            else
            {
                Console.WriteLine(  "Failed to connect to the database.");
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
    }
}