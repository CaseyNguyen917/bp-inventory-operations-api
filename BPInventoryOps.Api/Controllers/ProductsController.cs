using BPInventoryOps.Api.Auth;
using BPInventoryOps.Api.Dtos.Common;
using BPInventoryOps.Api.Dtos.Products;
using BPInventoryOps.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BPInventoryOps.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.EmployeeOrAbove)]
[Route("api/products")]
public sealed class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<ProductResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ProductResponse>>> List(
        [FromQuery] ProductListQuery request,
        CancellationToken cancellationToken)
    {
        return Ok(await productService.ListAsync(request, cancellationToken));
    }

    [HttpGet("low-stock")]
    [ProducesResponseType<PagedResponse<LowStockProductResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<LowStockProductResponse>>> ListLowStock(
        [FromQuery] LowStockProductQuery request,
        CancellationToken cancellationToken)
    {
        return Ok(await productService.ListLowStockAsync(request, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        return Ok(await productService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ManagerOrAbove)]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponse>> Create(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        ProductResponse response = await productService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.ManagerOrAbove)]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductResponse>> Update(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await productService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.ManagerOrAbove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Deactivate(
        int id,
        CancellationToken cancellationToken)
    {
        await productService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/reactivate")]
    [Authorize(Policy = AuthorizationPolicies.ManagerOrAbove)]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductResponse>> Reactivate(
        int id,
        CancellationToken cancellationToken)
    {
        return Ok(await productService.ReactivateAsync(id, cancellationToken));
    }
}
