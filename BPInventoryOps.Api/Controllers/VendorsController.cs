using BPInventoryOps.Api.Auth;
using BPInventoryOps.Api.Dtos.Common;
using BPInventoryOps.Api.Dtos.Vendors;
using BPInventoryOps.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BPInventoryOps.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.EmployeeOrAbove)]
[Route("api/vendors")]
public sealed class VendorsController(IVendorService vendorService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<VendorResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<VendorResponse>>> List(
        [FromQuery] VendorListQuery request,
        CancellationToken cancellationToken)
    {
        return Ok(await vendorService.ListAsync(request, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<VendorResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VendorResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        return Ok(await vendorService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ManagerOrAbove)]
    [ProducesResponseType<VendorResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VendorResponse>> Create(
        CreateVendorRequest request,
        CancellationToken cancellationToken)
    {
        VendorResponse response = await vendorService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.ManagerOrAbove)]
    [ProducesResponseType<VendorResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<VendorResponse>> Update(
        int id,
        UpdateVendorRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await vendorService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.ManagerOrAbove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Deactivate(
        int id,
        CancellationToken cancellationToken)
    {
        await vendorService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/reactivate")]
    [Authorize(Policy = AuthorizationPolicies.ManagerOrAbove)]
    [ProducesResponseType<VendorResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<VendorResponse>> Reactivate(
        int id,
        CancellationToken cancellationToken)
    {
        return Ok(await vendorService.ReactivateAsync(id, cancellationToken));
    }
}
