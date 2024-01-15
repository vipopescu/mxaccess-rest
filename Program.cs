using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using MXAccesRestAPI.GRAccess;
using MXAccesRestAPI.MXDataHolder;
using MXAccesRestAPI.Settings;
using MXAccess_RestAPI.DBContext;
using System.Text.Json;
using MXAccesRestAPI.Classes;

namespace MXAccesRestAPI
{
    public class Program
    {
        private static void Main(string[] args)
        {
            string appEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            string basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";

            var builder = WebApplication.CreateBuilder(args);
            builder.Services.Configure<GalaxySettings>(builder.Configuration.GetSection("GalaxySettings"));


            builder.Services.AddDbContext<GRDBContext>(options =>
                           options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Configuration.AddJsonFile($"appsettings.{appEnv}.json", optional: true);

            string? attributeConfPath = builder.Configuration.GetValue<string>("AttributeConfigPath");
            if (string.IsNullOrEmpty(attributeConfPath))
            {
                throw new InvalidOperationException($"Attribute Config not found: {attributeConfPath}");
            }

            AttributeConfigSettings attributeConfig =
                JsonSerializer.Deserialize<AttributeConfigSettings>(File.ReadAllText(Path.Combine(basePath, attributeConfPath))) ?? throw new InvalidOperationException($"Attribute Config error: {attributeConfPath}");


            string serverName = builder.Configuration.GetValue<string>("ServerName") ?? "";

            builder.Services.AddSingleton<IMXDataHolderService>(new MXDataHolderService(serverName, attributeConfig.AllowedTagAttributes));
            builder.Services.AddHostedService<GRAccessReadingService>();

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