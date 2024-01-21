using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using MXAccesRestAPI.GRAccess;
using MXAccesRestAPI.MXDataHolder;
using MXAccesRestAPI.Settings;
using MXAccess_RestAPI.DBContext;
using System.Text.Json;
using MXAccesRestAPI.Classes;
using MXAccesRestAPI.Monitoring;
using System.Collections.Concurrent;

namespace MXAccesRestAPI
{
    public class Program
    {
        private static void Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);

            Console.WriteLine($"{DateTime.Now.ToString()} -> Starting the business...");

            // Settings & Config
            string serverName = builder.Configuration.GetValue<string>("ServerName") ?? "";
            builder.Services.Configure<GalaxySettings>(builder.Configuration.GetSection("GalaxySettings"));

            // Attribute Tag configuration
            string? attributeConfPath = builder.Configuration.GetValue<string>("AttributeConfigPath");
            if (string.IsNullOrEmpty(attributeConfPath))
            {
                throw new InvalidOperationException($"Attribute Config not found: {attributeConfPath}");
            }

            string basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            AttributeConfigSettings attributeConfig =
                JsonSerializer.Deserialize<AttributeConfigSettings>(File.ReadAllText(Path.Combine(basePath, attributeConfPath))) ?? throw new InvalidOperationException($"Attribute Config error: {attributeConfPath}");

            builder.Services.AddSingleton<ConcurrentDictionary<int, MXAttribute>>(new ConcurrentDictionary<int, MXAttribute>());
            builder.Services.AddSingleton<IMXDataHolderServiceFactory, MXDataHolderServiceFactory>();

            // Adding services
            //var mxDataHolderService = new MXDataHolderService(-69, serverName, attributeConfig.AllowedTagAttributes, []);
            //builder.Services.AddSingleton<IMXDataHolderService>(mxDataHolderService);
            builder.Services.AddHostedService<GRAccessReadingService>();
            builder.Services.AddDbContext<GRDBContext>(options =>
                           options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


            // Register DataStoreMonitor as a Singleton and use the same instance of MXDataHolderService
            //builder.Services.AddSingleton<AlarmDataMonitor>(new AlarmDataMonitor(mxDataHolderService));


            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
            });


            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}