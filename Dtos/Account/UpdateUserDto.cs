using System;

namespace api.Dtos.Account
{
    public class UpdateUserDto
    {
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Country { get; set; }

    }

}
