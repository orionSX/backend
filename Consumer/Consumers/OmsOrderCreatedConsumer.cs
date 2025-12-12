using System.Diagnostics;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Consumer.Config;
using System.Text;
using API.BLL.Models;
using API.Clients;
using API.DAL.Models;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Consumer.Consumers
{
    public class OmsOrderCreatedConsumer : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptions<RabbitMqSettings> _rabbitMqSettings;
        private readonly ILogger<OmsOrderCreatedConsumer> _logger;
        private readonly ConnectionFactory _factory;
        private IConnection _connection;
        private IChannel _channel;
        private AsyncEventingBasicConsumer _consumer;

        public OmsOrderCreatedConsumer(
            IOptions<RabbitMqSettings> rabbitMqSettings,
            IServiceProvider serviceProvider,
            ILogger<OmsOrderCreatedConsumer> logger)
        {
            _rabbitMqSettings = rabbitMqSettings;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _factory = new ConnectionFactory
            {
                HostName = rabbitMqSettings.Value.HostName,
                Port = rabbitMqSettings.Value.Port
            };
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _connection = await _factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await _channel.QueueDeclareAsync(
                queue: _rabbitMqSettings.Value.OrderCreatedQueue,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            var sw = new Stopwatch();
            
            
            
            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: cancellationToken);
            _consumer = new AsyncEventingBasicConsumer(_channel);
            _consumer.ReceivedAsync += async (sender, args) =>
            {
                sw.Restart();
                try
                {
                    var body = args.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);


                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    };

                    var order = JsonSerializer.Deserialize<OmsOrderCreatedMessage>(message, options);

                    _logger.LogInformation("Received order: {OrderId} with {ItemCount} items",
                        order.Id, order.OrderItems?.Length ?? 0);

                    // Валидация
                    if (order.OrderItems == null || order.OrderItems.Length == 0)
                    {
                        _logger.LogError("Order {OrderId} has no items", order.Id);
                        return;
                    }

                    using var scope = _serviceProvider.CreateScope();
                    var client = scope.ServiceProvider.GetRequiredService<OmsClient>();

                    await client.LogOrder(new V1AuditLogOrderRequest
                    {
                        Orders = order.OrderItems.Select(x =>
                            new V1AuditLogOrderRequest.LogOrder
                            {
                                OrderId = order.Id,
                                OrderItemId = x.Id,
                                CustomerId = order.CustomerId,
                                OrderStatus = OrderStatus.Created.ToString()
                            }).ToArray()
                    }, CancellationToken.None);

                    await _channel.BasicAckAsync(args.DeliveryTag, false,cancellationToken);
                    sw.Stop();
                    _logger.LogInformation($"Order created consumed in {sw.ElapsedMilliseconds}ms");
                    _logger.LogInformation("Successfully processed order {OrderId}", order.Id);
                }
                catch (Exception ex)
                {
                    
                    _logger.LogError(ex, $"Error processing RabbitMQ message: {ex.Message}");
                    await _channel.BasicNackAsync(args.DeliveryTag, false,true,cancellationToken);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: _rabbitMqSettings.Value.OrderCreatedQueue,
                autoAck: false,
                consumer: _consumer,
                cancellationToken: cancellationToken);

            _logger.LogInformation("OmsOrderCreatedConsumer started listening on queue: {Queue}",
                _rabbitMqSettings.Value.OrderCreatedQueue);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("OmsOrderCreatedConsumer stopping...");
            await Task.CompletedTask;
            _connection?.Dispose();
            _channel?.Dispose();
            _logger.LogInformation("OmsOrderCreatedConsumer stopped");
        }
    }

    public enum OrderStatus
    {
        Created,
        Processing,
        Completed,
        Cancelled
    }
}