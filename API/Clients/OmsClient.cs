using API.DAL.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using API.DAL.Models;

namespace API.Clients
{
    public class OmsClient(HttpClient client, ILogger<OmsClient> logger)
    {
        public async Task<V1AuditLogOrderResponse> LogOrder(V1AuditLogOrderRequest request, CancellationToken token)
        {
            try
            {
                // ВАЖНО: Используем snake_case для соответствия настройкам API
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    WriteIndented = true
                };

                var requestJson = JsonSerializer.Serialize(request, jsonOptions);

                // Логируем ВСЮ информацию о запросе
                logger.LogInformation("=== OmsClient Request Details ===");
                logger.LogInformation("BaseAddress: {BaseAddress}", client.BaseAddress?.ToString() ?? "NULL");
                logger.LogInformation("Request URI: {RequestUri}", "api/v1/audit-log-order");
                logger.LogInformation("Full URL: {FullUrl}", $"{client.BaseAddress}api/v1/audit-log-order");
                logger.LogInformation("Request JSON: {Json}", requestJson);
                logger.LogInformation("=== End Request Details ===");

                var response = await client.PostAsync("api/v1/audit-log-order",
                    new StringContent(requestJson, Encoding.UTF8, "application/json"), token);

                logger.LogInformation("Response status: {StatusCode}", response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(token);

                    // Для десериализации ответа используем snake_case для соответствия настройкам API
                    var responseOptions = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                        PropertyNameCaseInsensitive = true
                    };

                    return JsonSerializer.Deserialize<V1AuditLogOrderResponse>(content, responseOptions);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync(token);

                    // ПОДРОБНОЕ исключение
                    var exceptionDetails = $@"
HTTP {(int)response.StatusCode} {response.StatusCode}
Request URL: {client.BaseAddress}api/v1/audit-log-order
Request Method: POST
Response Content: {errorContent}
Request Headers: {string.Join(", ", client.DefaultRequestHeaders)}
Request Body: {requestJson}
";

                    logger.LogError("Audit log failed: {Details}", exceptionDetails);
                    throw new HttpRequestException(exceptionDetails);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "=== OmsClient Full Error ===");
                logger.LogError("Exception Type: {Type}", ex.GetType().FullName);
                logger.LogError("Exception Message: {Message}", ex.Message);
                logger.LogError("Stack Trace: {StackTrace}", ex.StackTrace);

                if (ex.InnerException != null)
                {
                    logger.LogError("Inner Exception: {InnerMessage}", ex.InnerException.Message);
                    logger.LogError("Inner Stack Trace: {InnerStackTrace}", ex.InnerException.StackTrace);
                }
                logger.LogError("=== End OmsClient Error ===");

                throw;
            }
        }
    }
}
