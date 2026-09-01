using BPInventoryOps.Api.Auth;
using BPInventoryOps.Api.Dtos.Categories;
using BPInventoryOps.Api.Dtos.Common;
using BPInventoryOps.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BPInventoryOps.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.EmployeeOrAbove)]
[Route("api/categories")]
public sealed class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<CategoryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<CategoryResponse>>> List(
        [FromQuery] CategoryListQuery request,
        CancellationToken cancellationToken)
    {
        return Ok(await categoryService.ListAsync(request, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        return Ok(await categoryService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ManagerOrAbove)]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> Create(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        CategoryResponse response = await categoryService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.ManagerOrAbove)]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CategoryResponse>> Update(
        int id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await categoryService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.ManagerOrAbove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Deactivate(
        int id,
        CancellationToken cancellationToken)
    {
        await categoryService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/reactivate")]
    [Authorize(Policy = AuthorizationPolicies.ManagerOrAbove)]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CategoryResponse>> Reactivate(
        int id,
        CancellationToken cancellationToken)
    {
        return Ok(await categoryService.ReactivateAsync(id, cancellationToken));
    }
}
