using System.Text;
using API.DAL.Interfaces;
using API.DAL.Models;
using Dapper;

namespace API.DAL.Repositories;

public class OrderRepository(UnitOfWork unitOfWork) : IOrderRepository
{
    public async Task<V1OrderDal[]> BulkInsert(V1OrderDal[] model, CancellationToken token)
    {
        if (model == null || model.Length == 0)
            return Array.Empty<V1OrderDal>();

        var connection = await unitOfWork.GetConnection(token);

        const string sql = @"
        INSERT INTO orders 
        (
            customer_id,
            delivery_address,
            total_price_cents,
            total_price_currency,
            created_at,
            updated_at
        )
        SELECT 
            unnest(@CustomerIds),
            unnest(@DeliveryAddresses),
            unnest(@TotalPriceCents),
            unnest(@TotalPriceCurrencies),
            unnest(@CreatedAts),
            unnest(@UpdatedAts)
        RETURNING 
            id,
            customer_id,
            delivery_address,
            total_price_cents,
            total_price_currency,
            created_at,
            updated_at";

        // Создаем массивы для каждого поля
        var customerIds = model.Select(m => m.CustomerId).ToArray();
        var deliveryAddresses = model.Select(m => m.DeliveryAddress).ToArray();
        var totalPriceCents = model.Select(m => m.TotalPriceCents).ToArray();
        var totalPriceCurrencies = model.Select(m => m.TotalPriceCurrency).ToArray();
        var createdAts = model.Select(m => m.CreatedAt).ToArray();
        var updatedAts = model.Select(m => m.UpdatedAt).ToArray();

        var result = await connection.QueryAsync<V1OrderDal>(
            new CommandDefinition(
                sql, 
                new 
                { 
                    CustomerIds = customerIds,
                    DeliveryAddresses = deliveryAddresses,
                    TotalPriceCents = totalPriceCents,
                    TotalPriceCurrencies = totalPriceCurrencies,
                    CreatedAts = createdAts,
                    UpdatedAts = updatedAts
                }, 
                cancellationToken: token));

        return result.ToArray();
    }
    public async Task<V1OrderDal[]> Query(QueryOrdersDalModel model, CancellationToken token)
    {
        var sql = new StringBuilder(@"
            select 
                id,
                customer_id,
                delivery_address,
                total_price_cents,
                total_price_currency,
                created_at,
                updated_at
            from orders
        ");

        // тот же динамический тип данных 
        var param = new DynamicParameters();

        // собираем условия для where
        var conditions = new List<string>();

        if (model.Ids?.Length > 0)
        {
            // добавляем в динамический тип данные по айдишкам
            param.Add("Ids", model.Ids);
            conditions.Add("id = ANY(@Ids)");
        }

        if (model.CustomerIds?.Length > 0)
        {
            param.Add("CustomerIds", model.CustomerIds);
            conditions.Add("customer_id = ANY(@CustomerIds)");
        }

        if (conditions.Count > 0)
            // если условия есть, то добавляем в sql
            sql.Append(" where " + string.Join(" and ", conditions));

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
        var res = await conn.QueryAsync<V1OrderDal>(new CommandDefinition(
            sql.ToString(), param, cancellationToken: token));

        return res.ToArray();
    }
}