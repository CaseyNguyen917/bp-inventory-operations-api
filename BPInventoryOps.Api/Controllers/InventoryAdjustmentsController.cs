using BPInventoryOps.Api.Auth;
using BPInventoryOps.Api.Dtos.Common;
using BPInventoryOps.Api.Dtos.InventoryAdjustments;
using BPInventoryOps.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BPInventoryOps.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.EmployeeOrAbove)]
[Route("api/inventory-adjustments")]
public sealed class InventoryAdjustmentsController(
    IInventoryAdjustmentService inventoryAdjustmentService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<InventoryAdjustmentResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<InventoryAdjustmentResponse>>> List(
        [FromQuery] InventoryAdjustmentListQuery request,
        CancellationToken cancellationToken)
    {
        return Ok(await inventoryAdjustmentService.ListAsync(request, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<InventoryAdjustmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryAdjustmentResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        return Ok(await inventoryAdjustmentService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<InventoryAdjustmentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InventoryAdjustmentResponse>> Create(
        CreateInventoryAdjustmentRequest request,
        CancellationToken cancellationToken)
    {
        InventoryAdjustmentResponse response = await inventoryAdjustmentService
            .CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }
}
