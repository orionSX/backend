using Consumer.Config;
using Consumer.Consumers;
using API.Clients;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("consumer.appsettings.json", optional: false);
builder.Configuration.AddJsonFile($"consumer.appsettings.{builder.Environment.EnvironmentName}.json", optional: true);
builder.Services.AddLogging();

builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMqSettings"));
builder.Services.AddHostedService<OmsOrderCreatedConsumer>();
builder.Services.AddHttpClient<OmsClient>(c => c.BaseAddress = new Uri(builder.Configuration["HttpClient:Oms:BaseAddress"]));

var app = builder.Build();
await app.RunAsync();
