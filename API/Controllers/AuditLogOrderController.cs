using API.BLL.Models;
using API.BLL.Services;
using API.Validators;
using API.DAL.Models;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{


    [ApiController]
    [Route("api/v1/audit-log-order")]
    public class AuditLogOrderController : ControllerBase
    {
        private readonly AuditLogOrderService _auditLogOrderService;
        private readonly V1AuditLogOrderRequestValidator _validator;

        public AuditLogOrderController(AuditLogOrderService auditLogOrderService, V1AuditLogOrderRequestValidator validator)
        {
            _auditLogOrderService = auditLogOrderService;
            _validator = validator;
        }

        [HttpPost]
        public async Task<ActionResult<V1AuditLogOrderResponse>> CreateAuditLogs(
            [FromBody] V1AuditLogOrderRequest request,
            CancellationToken token)
        {
            var validationResult = await _validator.ValidateAsync(request, token);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            var result = await _auditLogOrderService.CreateAuditLogs(request, token);
            return Ok(result);
        }

        [HttpGet]
        public async Task<ActionResult<V1AuditLogOrderResponse>> GetAuditLogs(
            [FromQuery] QueryAuditLogOrderModel model,
            CancellationToken token)
        {
            var result = await _auditLogOrderService.GetAuditLogs(model, token);
            return Ok(result);
        }
    }
}
