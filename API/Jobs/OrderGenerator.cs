using API.BLL.Models;
using API.BLL.Services;
using AutoFixture;

namespace API.Jobs;

public class OrderGenerator(IServiceProvider serviceProvider): BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var fixture = new Fixture();
        using var scope = serviceProvider.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {


                var orders = Enumerable.Range(1, 100)
                    .Select<int, OrderUnit>(_ =>
                    {
                        var orderItem = fixture.Build<OrderItemUnit>()
                            .With(x => x.PriceCurrency, "RUB")
                            .With(x => x.PriceCents, 1000)
                            .Create();

                        var order = fixture.Build<OrderUnit>()
                            .With(x => x.TotalPriceCurrency, "RUB")
                            .With(x => x.TotalPriceCents, 1000)
                            .With(x => x.OrderItems, [orderItem])
                            .Create();

                        return order;
                    })
                    .ToArray();

                await orderService.BatchInsert(orders, stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            await Task.Delay(250, stoppingToken);
        }
    }
}