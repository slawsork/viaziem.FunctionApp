using MongoDB.Bson.Serialization.Attributes;

namespace Viaziem.Contracts.Entities
{
    public class User
    {
        [BsonId] public System.Guid Id { get; set; }

        [BsonRequired] public string Email { get; set; }

        [BsonRequired] public string Password { get; set; }

        public string Role { get; set; }

        public bool Active { get; set; }
    }
}