using BPInventoryOps.Api.Dtos.Users;
using BPInventoryOps.Api.Dtos.Common;
using BPInventoryOps.Api.Pages.Shared;
using BPInventoryOps.Api.Services;
using Microsoft.AspNetCore.Mvc;
namespace BPInventoryOps.Api.Pages.Users;
public sealed class IndexModel(IUserAdministrationService service) : UiPageModel
{
    [BindProperty(Name = "Query", SupportsGet = true)] public UserListQuery Query { get; set; } = new();
    public PagedResponse<UserResponse> Data { get; private set; } = null!;
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (InvalidQuery() is { } invalid) return invalid;
        Data = await service.ListAsync(Query, ct);
        return Page();
    }
}
