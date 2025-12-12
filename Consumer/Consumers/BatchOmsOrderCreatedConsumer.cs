using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using API.BLL.Models;
using API.Clients;
using API.DAL.Models;
using Consumer.Base;
using Consumer.Config;

namespace Consumer.Consumers
{
    public class BatchOmsOrderCreatedConsumer(
        IOptions<RabbitMqSettings> rabbitMqSettings,
        IServiceProvider serviceProvider)
        : BaseBatchMessageConsumer<OmsOrderCreatedMessage>(rabbitMqSettings.Value)
    {
        protected override async Task ProcessMessages(OmsOrderCreatedMessage[] messages)
        {
            using var scope = serviceProvider.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<OmsClient>();
        
            await client.LogOrder(new V1AuditLogOrderRequest
            {
                Orders = messages.SelectMany(order => order.OrderItems.Select(ol => 
                    new V1AuditLogOrderRequest.LogOrder
                    {
                        OrderId = order.Id,
                        OrderItemId = ol.Id,
                        CustomerId = order.CustomerId,
                        OrderStatus = nameof(OrderStatus.Created)
                    })).ToArray()
            }, CancellationToken.None);
        }
    }

}