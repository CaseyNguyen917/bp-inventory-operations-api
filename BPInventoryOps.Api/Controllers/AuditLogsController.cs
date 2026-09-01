using BPInventoryOps.Api.Auth;
using BPInventoryOps.Api.Dtos.AuditLogs;
using BPInventoryOps.Api.Dtos.Common;
using BPInventoryOps.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BPInventoryOps.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.ManagerOrAbove)]
[Route("api/audit-logs")]
public sealed class AuditLogsController(IAuditService auditService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<AuditLogResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<AuditLogResponse>>> List(
        [FromQuery] AuditLogListQuery request,
        CancellationToken cancellationToken)
    {
        return Ok(await auditService.ListAsync(request, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<AuditLogResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuditLogResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        return Ok(await auditService.GetByIdAsync(id, cancellationToken));
    }
}
