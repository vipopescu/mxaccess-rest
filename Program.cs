using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using MXAccesRestAPI.Classes;
using MXAccesRestAPI.GRAccess;
using MXAccesRestAPI.MXDataHolder;
using MXAccesRestAPI.Settings;
using MXAccess_RestAPI.DBContext;

namespace MXAccesRestAPI
{
    public class Program
    {
        private static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.Configure<GalaxySettings>(builder.Configuration.GetSection("GalaxySettings"));

            builder.Services.AddDbContext<GRDBContext>(options =>
                           options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddSingleton<IMXDataHolderService>(new MXDataHolderService("RESTAPI-AVEVA"));

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