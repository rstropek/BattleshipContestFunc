using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace BattleshipContestFunc.Data.Tests
{
    public class StorageFixture
    {
        public StorageFixture()
        {
            Configuration = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(Environment.CurrentDirectory, "appsettings.json"), true, false)
                .AddEnvironmentVariables()
                .Build();

            Repository = new Repository(Configuration);
            PlayerGameLease = new PlayerGameLeaseManager(Configuration);
        }

        public IConfiguration Configuration { get; set; }

        public IRepository Repository { get; set; }
        public PlayerGameLeaseManager PlayerGameLease { get; set; }
    }
}
