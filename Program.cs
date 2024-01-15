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

namespace MXAccesRestAPI
{
    public class Program
    {
        private static void Main(string[] args)
        {
            string appEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            string basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            var builder = WebApplication.CreateBuilder(args);

            // Settings & Config
            string serverName = builder.Configuration.GetValue<string>("ServerName") ?? "";
            builder.Services.Configure<GalaxySettings>(builder.Configuration.GetSection("GalaxySettings"));
            builder.Configuration.AddJsonFile($"appsettings.{appEnv}.json", optional: true);

            // Attribute Tag configuration
            string? attributeConfPath = builder.Configuration.GetValue<string>("AttributeConfigPath");
            if (string.IsNullOrEmpty(attributeConfPath))
            {
                throw new InvalidOperationException($"Attribute Config not found: {attributeConfPath}");
            }

            AttributeConfigSettings attributeConfig =
                JsonSerializer.Deserialize<AttributeConfigSettings>(File.ReadAllText(Path.Combine(basePath, attributeConfPath))) ?? throw new InvalidOperationException($"Attribute Config error: {attributeConfPath}");


            // Adding services
            var mxDataHolderService = new MXDataHolderService(serverName, attributeConfig.AllowedTagAttributes);
            builder.Services.AddSingleton<IMXDataHolderService>(mxDataHolderService);
            builder.Services.AddHostedService<GRAccessReadingService>();
            builder.Services.AddDbContext<GRDBContext>(options =>
                           options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


            // Register DataStoreMonitor as a Singleton and use the same instance of MXDataHolderService
            builder.Services.AddSingleton<AlarmMonitor>(new AlarmMonitor(mxDataHolderService));


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