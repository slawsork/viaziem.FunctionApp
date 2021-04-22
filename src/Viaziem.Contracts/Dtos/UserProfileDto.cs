using System;

namespace Viaziem.Contracts.Dtos
{
    public class UserProfileDto
    {
        public string Username { get; set; }
        
        public string Description { get; set; }
        
        public string City { get; set; }
        
        public int Age { get; set; }

        public Guid ProfileImageId { get; set; }
    }
}