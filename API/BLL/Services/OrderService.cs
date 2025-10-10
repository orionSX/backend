using API.BLL.Models;
using API.DAL;
using API.DAL.Interfaces;
using API.DAL.Models;

namespace API.BLL.Services;

public class OrderService(
    UnitOfWork unitOfWork,
    IOrderRepository orderRepository,
    IOrderItemRepository orderItemRepository)
{
    /// <summary>
    ///     Метод создания заказов
    /// </summary>
    public async Task<OrderUnit[]> BatchInsert(OrderUnit[] orderUnits, CancellationToken token)
    {
        var now = DateTimeOffset.UtcNow;
        await using var transaction = await unitOfWork.BeginTransactionAsync(token);

        try
        {
            var resultOrders = new List<OrderUnit>();


            var ordersDal = orderUnits.Select(orderUnit => new V1OrderDal
            {
                CustomerId = orderUnit.CustomerId,
                DeliveryAddress = orderUnit.DeliveryAddress,
                TotalPriceCents = orderUnit.TotalPriceCents,
                TotalPriceCurrency = orderUnit.TotalPriceCurrency,
                CreatedAt = now,
                UpdatedAt = now
            }).ToArray();

            var savedOrders = await orderRepository.BulkInsert(ordersDal, token);

            // 2. Подготавливаем и сохраняем позиции заказов пакетно
            var allOrderItems = new List<V1OrderItemDal>();

            for (var i = 0; i < savedOrders.Length; i++)
                if (orderUnits[i].OrderItems?.Length > 0)
                {
                    var orderItems = orderUnits[i].OrderItems.Select(item => new V1OrderItemDal
                    {
                        OrderId = savedOrders[i].Id, // Связываем с сохраненным заказом
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        ProductTitle = item.ProductTitle,
                        ProductUrl = item.ProductUrl,
                        PriceCents = item.PriceCents,
                        PriceCurrency = item.PriceCurrency,
                        CreatedAt = now,
                        UpdatedAt = now
                    });

                    allOrderItems.AddRange(orderItems);
                }

            var savedOrderItems = allOrderItems.Count > 0
                ? await orderItemRepository.BulkInsert(allOrderItems.ToArray(), token)
                : [];

            // 3. Группируем позиции по OrderId для удобства
            var orderItemsByOrderId = savedOrderItems.GroupBy(x => x.OrderId)
                .ToDictionary(g => g.Key, g => g.ToArray());

            // 4. Собираем результат
            for (var i = 0; i < savedOrders.Length; i++)
            {
                var savedOrder = savedOrders[i];
                var orderItems = orderItemsByOrderId.GetValueOrDefault(savedOrder.Id) ?? [];

                resultOrders.Add(new OrderUnit
                {
                    Id = savedOrder.Id,
                    CustomerId = savedOrder.CustomerId,
                    DeliveryAddress = savedOrder.DeliveryAddress,
                    TotalPriceCents = savedOrder.TotalPriceCents,
                    TotalPriceCurrency = savedOrder.TotalPriceCurrency,
                    CreatedAt = savedOrder.CreatedAt,
                    UpdatedAt = savedOrder.UpdatedAt,
                    OrderItems = orderItems.Select(item => new OrderItemUnit
                    {
                        Id = item.Id,
                        OrderId = item.OrderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        ProductTitle = item.ProductTitle,
                        ProductUrl = item.ProductUrl,
                        PriceCents = item.PriceCents,
                        PriceCurrency = item.PriceCurrency,
                        CreatedAt = item.CreatedAt,
                        UpdatedAt = item.UpdatedAt
                    }).ToArray()
                });
            }

            await transaction.CommitAsync(token);
            return resultOrders.ToArray();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync(token);
            throw;
        }
    }

    /// <summary>
    ///     Метод получения заказов
    /// </summary>
    public async Task<OrderUnit[]> GetOrders(QueryOrderItemsModel model, CancellationToken token)
    {
        var orders = await orderRepository.Query(new QueryOrdersDalModel
        {
            Ids = model.Ids,
            CustomerIds = model.CustomerIds,
            Limit = model.PageSize,
            Offset = (model.Page - 1) * model.PageSize
        }, token);

        if (orders.Length is 0) return [];

        ILookup<long, V1OrderItemDal> orderItemLookup = null;
        if (model.IncludeOrderItems)
        {
            var orderItems = await orderItemRepository.Query(new QueryOrderItemsDalModel
            {
                OrderIds = orders.Select(x => x.Id).ToArray()
            }, token);

            orderItemLookup = orderItems.ToLookup(x => x.OrderId);
        }

        return Map(orders, orderItemLookup);
    }

    private OrderUnit[] Map(V1OrderDal[] orders,
        ILookup<long, V1OrderItemDal> orderItemLookup = null)
    {
        return orders.Select(x => new OrderUnit
        {
            Id = x.Id,
            CustomerId = x.CustomerId,
            DeliveryAddress = x.DeliveryAddress,
            TotalPriceCents = x.TotalPriceCents,
            TotalPriceCurrency = x.TotalPriceCurrency,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            OrderItems = orderItemLookup?[x.Id].Select(o => new OrderItemUnit
            {
                Id = o.Id,
                OrderId = o.OrderId,
                ProductId = o.ProductId,
                Quantity = o.Quantity,
                ProductTitle = o.ProductTitle,
                ProductUrl = o.ProductUrl,
                PriceCents = o.PriceCents,
                PriceCurrency = o.PriceCurrency,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt
            }).ToArray() ?? []
        }).ToArray();
    }
}