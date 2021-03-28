using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;
using System;
using System.IO;
using BattleshipContestFunc.Data;
using System.Text.Json;
using Azure.Core.Serialization;
using NBattleshipCodingContest.Logic;

namespace BattleshipContestFunc
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureAppConfiguration((context, builder) =>
                {
                    builder.AddJsonFile(Path.Combine(Environment.CurrentDirectory, "appsettings.json"), true, false)
                        .AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    var logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(context.Configuration)
                        .CreateLogger();
                    services.AddSingleton<ILoggerFactory>(new SerilogLoggerFactory(logger, false));
                    services.AddSingleton<IRepository, Repository>();
                    services.AddSingleton<IPlayerTable, PlayerTable>();
                    services.AddSingleton<IAuthorize, Authorize>();
                    services.AddAutoMapper(typeof(MappingProfile));
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    };
                    jsonOptions.Converters.Add(new BoardContentJsonConverter());
                    jsonOptions.Converters.Add(new BoardIndexJsonConverter());
                    services.AddSingleton(jsonOptions);
                    services.AddSingleton(_ => new JsonObjectSerializer(jsonOptions));
                })
                .Build();

            host.Run();
        }
    }
}