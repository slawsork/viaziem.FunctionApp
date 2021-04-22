using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Viaziem.Contracts.Entities
{
    public class UserProfile
    {
        [BsonId] public System.Guid UserId { get; set; }

        public Guid ProfileImageId { get; set; }
        
        public string Username { get; set; }
        
        public string Description { get; set; }
        
        public string City { get; set; }
        
        public int? Age { get; set; }
    }
}