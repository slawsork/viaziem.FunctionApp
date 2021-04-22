using System.IO;
using AutoMapper;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Viaziem.Core.DataProviders;
using Viaziem.Core.EventHub;
using Viaziem.Core.Helpers;
using Viaziem.Core.ServiceBus;
using Viaziem.FunctionApp;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Viaziem.FunctionApp
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                // This gives you access to your application settings in your local development environment
                .AddJsonFile("local.settings.json", true, true)
                // This is what actually gets you the application settings in Azure
                .AddEnvironmentVariables()
                .Build();

            var connectionString = config["dbConnectionString"];
            var usersDataProvider = new UsersDataProvider(connectionString);
            var usersProfilesDataProvider = new UsersProfilesDataProvider(usersDataProvider, connectionString);

            builder.Services.AddTransient<IUsersDataProvider>(x => usersDataProvider);
            builder.Services.AddTransient<IUsersProfilesDataProvider>(x => usersProfilesDataProvider);

            builder.Services.AddSingleton<IPasswordHelper, PasswordHelper>();

            builder.Services.AddSingleton<IAuthenticationManager>(x =>
                new AuthenticationManager(config["secret"], config["expirationDays"]));

            var serviceBusConnectionString = config["serviceBusConnectionString"];
            var serviceBusQueueName = config["serviceBusQueueName"];

            var eventDispatcher = new EventDispatcher();
            var dispatcher = new ServiceBusDispatcher(eventDispatcher, usersProfilesDataProvider,
                serviceBusConnectionString, serviceBusQueueName);
            builder.Services.AddSingleton<IServiceBusDispatcher>(dispatcher);

            RegisterAutoMapper(builder);
        }

        private static void RegisterAutoMapper(IFunctionsHostBuilder builder)
        {
            var mapperConfig = new MapperConfiguration(mc => { mc.AddProfile(new MappingProfile()); });

            var mapper = mapperConfig.CreateMapper();
            builder.Services.AddSingleton(mapper);
        }
    }
}