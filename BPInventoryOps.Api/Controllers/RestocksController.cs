using BPInventoryOps.Api.Auth;
using BPInventoryOps.Api.Dtos.Common;
using BPInventoryOps.Api.Dtos.Restocks;
using BPInventoryOps.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BPInventoryOps.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.EmployeeOrAbove)]
[Route("api/restocks")]
public sealed class RestocksController(IRestockService restockService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<RestockSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<RestockSummaryResponse>>> List(
        [FromQuery] RestockListQuery request,
        CancellationToken cancellationToken)
    {
        return Ok(await restockService.ListAsync(request, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<RestockResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RestockResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        return Ok(await restockService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<RestockResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RestockResponse>> Create(
        CreateRestockRequest request,
        CancellationToken cancellationToken)
    {
        RestockResponse response = await restockService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }
}
