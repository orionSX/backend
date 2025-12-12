using System.Text;
using API.DAL.Interfaces;
using API.DAL.Models;
using Dapper;

namespace API.DAL.Repositories;

public class OrderItemRepository(UnitOfWork unitOfWork) : IOrderItemRepository
{
    public async Task<V1OrderItemDal[]> BulkInsert(V1OrderItemDal[] model, CancellationToken token)
{
    if (model == null || model.Length == 0)
        return Array.Empty<V1OrderItemDal>();

    var connection = await unitOfWork.GetConnection(token);

    const string sql = @"
        INSERT INTO order_items 
        (
            order_id,
            product_id,
            quantity,
            product_title,
            product_url,
            price_cents,
            price_currency,
            created_at,
            updated_at
        )
        SELECT 
            unnest(@OrderIds),
            unnest(@ProductIds),
            unnest(@Quantities),
            unnest(@ProductTitles),
            unnest(@ProductUrls),
            unnest(@PriceCents),
            unnest(@PriceCurrencies),
            unnest(@CreatedAts),
            unnest(@UpdatedAts)
        RETURNING 
            id,
            order_id,
            product_id,
            quantity,
            product_title,
            product_url,
            price_cents,
            price_currency,
            created_at,
            updated_at";

    // Создаем массивы для каждого поля
    var orderIds = model.Select(m => m.OrderId).ToArray();
    var productIds = model.Select(m => m.ProductId).ToArray();
    var quantities = model.Select(m => m.Quantity).ToArray();
    var productTitles = model.Select(m => m.ProductTitle).ToArray();
    var productUrls = model.Select(m => m.ProductUrl).ToArray();
    var priceCents = model.Select(m => m.PriceCents).ToArray();
    var priceCurrencies = model.Select(m => m.PriceCurrency).ToArray();
    var createdAts = model.Select(m => m.CreatedAt).ToArray();
    var updatedAts = model.Select(m => m.UpdatedAt).ToArray();

    var result = await connection.QueryAsync<V1OrderItemDal>(
        new CommandDefinition(
            sql, 
            new 
            { 
                OrderIds = orderIds,
                ProductIds = productIds,
                Quantities = quantities,
                ProductTitles = productTitles,
                ProductUrls = productUrls,
                PriceCents = priceCents,
                PriceCurrencies = priceCurrencies,
                CreatedAts = createdAts,
                UpdatedAts = updatedAts
            }, 
            cancellationToken: token));

    return result.ToArray();
}
    public async Task<V1OrderItemDal[]> Query(QueryOrderItemsDalModel model,
        CancellationToken token)
    {
        var sql = new StringBuilder(@"
            select 
                id,
                order_id,
                product_id,
                quantity,
                product_title,
                product_url,
                price_cents,
                price_currency,
                created_at,
                updated_at
            from order_items
        ");

        var param = new DynamicParameters();
        var conditions = new List<string>();

      
        if (model.Ids?.Length > 0)
        {
            param.Add("Ids", model.Ids);
            conditions.Add("id = ANY(@Ids)");
        }

        
        if (model.OrderIds?.Length > 0)
        {
            param.Add("OrderIds", model.OrderIds);
            conditions.Add("order_id = ANY(@OrderIds)");
        }


        
        if (conditions.Count > 0) sql.Append(" where " + string.Join(" and ", conditions));


        
        if (model.Limit > 0)
        {
            sql.Append(" limit @Limit");
            param.Add("Limit", model.Limit);
        }

        if (model.Offset > 0)
        {
            sql.Append(" offset @Offset");
            param.Add("Offset", model.Offset);
        }

        var conn = await unitOfWork.GetConnection(token);
        var res = await conn.QueryAsync<V1OrderItemDal>(new CommandDefinition(
            sql.ToString(), param, cancellationToken: token));

        return res.ToArray();
    }
}