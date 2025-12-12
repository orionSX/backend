using Dapper;
using API.DAL.Interfaces;
using API.DAL.Models;

namespace API.DAL.Repositories
{
    public class AuditLogOrderRepository : IAuditLogOrderRepository
    {
        private readonly UnitOfWork _unitOfWork;

        public AuditLogOrderRepository(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<V1AuditLogOrderDal[]> BulkInsert(V1AuditLogOrderDal[] auditLogs, CancellationToken token)
        {
            if (auditLogs == null || auditLogs.Length == 0)
                return Array.Empty<V1AuditLogOrderDal>();

            var connection = await _unitOfWork.GetConnection(token);

            const string sql = @"
        INSERT INTO audit_log_order 
        (
            order_id,
            order_item_id,
            customer_id,
            order_status,
            created_at,
            updated_at
        )
        SELECT 
            unnest(@OrderIds),
            unnest(@OrderItemIds),
            unnest(@CustomerIds),
            unnest(@OrderStatuses),
            unnest(@CreatedAts),
            unnest(@UpdatedAts)
        RETURNING 
            id,
            order_id,
            order_item_id,
            customer_id,
            order_status,
            created_at,
            updated_at";

            // Создаем массивы для каждого поля
            var orderIds = auditLogs.Select(a => a.OrderId).ToArray();
            var orderItemIds = auditLogs.Select(a => a.OrderItemId).ToArray();
            var customerIds = auditLogs.Select(a => a.CustomerId).ToArray();
            var orderStatuses = auditLogs.Select(a => a.OrderStatus).ToArray();
            var createdAts = auditLogs.Select(a => a.CreatedAt).ToArray();
            var updatedAts = auditLogs.Select(a => a.UpdatedAt).ToArray();

            var result = await connection.QueryAsync<V1AuditLogOrderDal>(
                new CommandDefinition(
                    sql, 
                    new 
                    { 
                        OrderIds = orderIds,
                        OrderItemIds = orderItemIds,
                        CustomerIds = customerIds,
                        OrderStatuses = orderStatuses,
                        CreatedAts = createdAts,
                        UpdatedAts = updatedAts
                    }, 
                    cancellationToken: token));

            return result.ToArray();
        }
        public async Task<V1AuditLogOrderDal[]> Query(QueryAuditLogOrderDalModel model, CancellationToken token)
        {
            // Убеждаемся, что соединение инициализировано
            var connection = await _unitOfWork.GetConnection(token);

            var sql = @"
                SELECT * FROM audit_log_order 
                WHERE 1=1";

            var parameters = new DynamicParameters();

            if (model.Ids?.Length > 0)
            {
                sql += " AND id = ANY(@Ids)";
                parameters.Add("Ids", model.Ids);
            }

            if (model.OrderIds?.Length > 0)
            {
                sql += " AND order_id = ANY(@OrderIds)";
                parameters.Add("OrderIds", model.OrderIds);
            }

            if (model.CustomerIds?.Length > 0)
            {
                sql += " AND customer_id = ANY(@CustomerIds)";
                parameters.Add("CustomerIds", model.CustomerIds);
            }

            if (model.OrderStatuses?.Length > 0)
            {
                sql += " AND order_status = ANY(@OrderStatuses)";
                parameters.Add("OrderStatuses", model.OrderStatuses);
            }

            sql += " ORDER BY created_at DESC LIMIT @Limit OFFSET @Offset";
            parameters.Add("Limit", model.Limit);
            parameters.Add("Offset", model.Offset);

            var result = await connection.QueryAsync<V1AuditLogOrderDal>(
                new CommandDefinition(sql, parameters, cancellationToken: token));

            return result.ToArray();
        }
    }
}
