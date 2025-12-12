using System.Text.Json;
using API.BLL.Services;
using API.Config;
using API.DAL;
using API.DAL.Interfaces;
using API.DAL.Repositories;
using API.Services;
using API.Validators;
using Dapper;
using FluentValidation;

namespace API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddJsonFile("api.appsettings.json", optional: false);
        builder.Configuration.AddJsonFile($"api.appsettings.{builder.Environment.EnvironmentName}.json", optional: true);

        DefaultTypeMap.MatchNamesWithUnderscores = true;
        builder.Services.AddScoped<UnitOfWork>();
        builder.Services.Configure<DbSettings>(
            builder.Configuration.GetSection(nameof(DbSettings)));
        builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMqSettings"));

        builder.Services.AddValidatorsFromAssemblyContaining(typeof(Program));
        builder.Services.AddScoped<ValidatorFactory>();
        builder.Services.AddScoped<IOrderRepository, OrderRepository>();
        builder.Services.AddScoped<IOrderItemRepository, OrderItemRepository>();
        builder.Services.AddScoped<OrderService>();
        builder.Services.AddScoped<IAuditLogOrderRepository, AuditLogOrderRepository>();
       
        builder.Services.AddScoped<AuditLogOrderService>();
        builder.Services.AddScoped<RabbitMqService>();

        builder.Services.AddControllers().AddJsonOptions(options => 
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        });

        builder.Services.AddSwaggerGen();


        var app = builder.Build();


        app.UseSwagger();
        app.UseSwaggerUI();


        app.MapControllers();


        Migrations.Program.Main([]);

// запускам приложение
        app.Run();
    }
}