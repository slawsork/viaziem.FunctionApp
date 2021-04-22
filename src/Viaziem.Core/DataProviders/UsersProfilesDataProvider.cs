using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using Viaziem.Contracts.Dtos;
using Viaziem.Contracts.Entities;
using Viaziem.Contracts.Exceptions;

namespace Viaziem.Core.DataProviders
{
    public interface IUsersProfilesDataProvider
    {
        Task UpdateUserProfile(Guid userId, UserProfileDto userProfileDto);
        Task<UserProfile> GetUserProfile(Guid userId);
        Task AddPictureId(Guid profileImageId, Guid userId);
    }

    public class UsersProfilesDataProvider : IUsersProfilesDataProvider
    {
        private readonly IMongoDatabase _database;
        private readonly IUsersDataProvider _usersDataProvider;
        private IMongoCollection<UserProfile> _users;

        public UsersProfilesDataProvider(IUsersDataProvider usersDataProvider, string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentException();

            _usersDataProvider = usersDataProvider;

            var connection = new MongoUrlBuilder(connectionString);
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(connection.DatabaseName);
        }

        private IMongoCollection<UserProfile> UserProfiles
        {
            get
            {
                _users ??= _database.GetCollection<UserProfile>("UserProfiles");
                return _users;
            }
        }

        public async Task UpdateUserProfile(Guid userId, UserProfileDto userProfileDto)
        {
            var user = await _usersDataProvider.GetUser(userId);

            if (user == null) throw new NotFoundException("User not found");

            var userProfile = await TryGetUserProfile(userId);

            if (userProfile == null) await CreateUserProfile(userId);

            var filter = GetUserProfileFilter(userId);
            var update = Builders<UserProfile>.Update
                .Set(nameof(UserProfile.Username), userProfileDto.Username)
                .Set(nameof(UserProfile.Description), userProfileDto.Description)
                .Set(nameof(UserProfile.City), userProfileDto.City)
                .Set(nameof(UserProfile.Age), userProfileDto.Age);

            await UserProfiles.UpdateOneAsync(filter, update);
        }

        public async Task<UserProfile> GetUserProfile(Guid userId)
        {
            var userProfile = await TryGetUserProfile(userId);

            return userProfile;
        }

        public async Task AddPictureId(Guid profileImageId, Guid userId)
        {
            var user = await _usersDataProvider.GetUser(userId);

            if (user == null) throw new NotFoundException("User not found");

            var userProfile = await TryGetUserProfile(userId);

            if (userProfile == null) await CreateUserProfile(userId);

            var filter = GetUserProfileFilter(userId);
            var update = Builders<UserProfile>.Update
                .Set(nameof(UserProfile.ProfileImageId), profileImageId);

            await UserProfiles.UpdateOneAsync(filter, update);
        }

        private async Task CreateUserProfile(Guid userId)
        {
            var userProfile = new UserProfile
            {
                UserId = userId
            };

            await UserProfiles.InsertOneAsync(userProfile);
        }

        private async Task<UserProfile> TryGetUserProfile(Guid userId)
        {
            var filter = GetUserProfileFilter(userId);

            var result = await UserProfiles.Find(filter).FirstOrDefaultAsync();

            return result;
        }

        private static FilterDefinition<UserProfile> GetUserProfileFilter(Guid userId)
        {
            return Builders<UserProfile>.Filter.Eq(x => x.UserId, userId);
        }
    }
}