using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using Viaziem.Contracts;
using Viaziem.Contracts.Dtos;
using Viaziem.Contracts.Entities;

namespace Viaziem.Core.DataProviders
{
    public interface IUsersDataProvider
    {
        Task<bool> UserExists(string email);
        Task<User> GetUser(string email);
        Task<User> GetUser(Guid id);
        Task CreateUser(string email, string password);
        Task<IReadOnlyCollection<User>> GetAllUsers();
        Task SetUserStatus(UserInfo userInfo);
    }

    public class UsersDataProvider : IUsersDataProvider
    {
        private readonly IMongoDatabase _database;
        private IMongoCollection<User> _users;

        public UsersDataProvider(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentException();

            var connection = new MongoUrlBuilder(connectionString);
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(connection.DatabaseName);
        }

        private IMongoCollection<User> Users
        {
            get
            {
                _users ??= _database.GetCollection<User>("Users");
                return _users;
            }
        }

        public async Task<bool> UserExists(string email)
        {
            var result = await TryGetUser(email);

            return result != null;
        }

        public async Task<User> GetUser(string email)
        {
            var result = await TryGetUser(email);
            return result;
        }

        public async Task<User> GetUser(Guid id)
        {
            var filter = Builders<User>.Filter.Eq(x => x.Active, true);
            filter &= Builders<User>.Filter.Eq(x => x.Id, id);

            var result = await Users.Find(filter).FirstOrDefaultAsync();
            return result;
        }

        public async Task CreateUser(string email, string password)
        {
            var user = new User
            {
                Email = email,
                Password = password,
                Active = true,
                Role = string.Empty
            };

            await Users.InsertOneAsync(user);
        }

        public async Task<IReadOnlyCollection<User>> GetAllUsers()
        {
            var filter = Builders<User>.Filter.Ne(x => x.Role, Constants.Admin);

            var result = await Users.Find(filter).ToListAsync();
            return result;
        }

        public async Task SetUserStatus(UserInfo userInfo)
        {
            var filter = Builders<User>.Filter.Eq(x => x.Email, userInfo.Email);
            var update = Builders<User>.Update.Set("Active", userInfo.Active);

            await Users.UpdateOneAsync(filter, update);
        }

        private async Task<User> TryGetUser(string email)
        {
            var filter = Builders<User>.Filter.Eq(x => x.Active, true);
            filter &= Builders<User>.Filter.Eq(x => x.Email, email);

            var result = await Users.Find(filter).FirstOrDefaultAsync();
            return result;
        }
    }
}