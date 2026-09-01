using BPInventoryOps.Api.Auth;
using BPInventoryOps.Api.Dtos.Common;
using BPInventoryOps.Api.Dtos.Users;
using BPInventoryOps.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BPInventoryOps.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/users")]
public sealed class UsersController(
    IUserAdministrationService userAdministrationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<UserResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<UserResponse>>> List(
        [FromQuery] UserListQuery request,
        CancellationToken cancellationToken)
    {
        return Ok(await userAdministrationService.ListAsync(request, cancellationToken));
    }

    [HttpGet("{id}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetById(
        string id,
        CancellationToken cancellationToken)
    {
        return Ok(await userAdministrationService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Create(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        UserResponse response = await userAdministrationService
            .CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id}/role")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> ChangeRole(
        string id,
        ChangeUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await userAdministrationService
            .ChangeRoleAsync(id, request, cancellationToken));
    }

    [HttpPost("{id}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(
        string id,
        CancellationToken cancellationToken)
    {
        await userAdministrationService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/reactivate")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Reactivate(
        string id,
        CancellationToken cancellationToken)
    {
        return Ok(await userAdministrationService.ReactivateAsync(id, cancellationToken));
    }
}
