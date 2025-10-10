using API.BLL.Services;
using API.DAL;
using API.DAL.Interfaces;
using API.DAL.Repositories;
using API.Validators;
using Dapper;
using FluentValidation;

namespace API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);


        DefaultTypeMap.MatchNamesWithUnderscores = true;
        builder.Services.AddScoped<UnitOfWork>();
        builder.Services.Configure<DbSettings>(
            builder.Configuration.GetSection(nameof(DbSettings)));


        builder.Services.AddValidatorsFromAssemblyContaining(typeof(Program));
        builder.Services.AddScoped<ValidatorFactory>();
        builder.Services.AddScoped<IOrderRepository, OrderRepository>();
        builder.Services.AddScoped<IOrderItemRepository, OrderItemRepository>();
        builder.Services.AddScoped<OrderService>();
        builder.Services.AddControllers();

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