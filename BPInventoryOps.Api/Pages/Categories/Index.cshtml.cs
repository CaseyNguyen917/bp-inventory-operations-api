using BPInventoryOps.Api.Dtos.Categories;
using BPInventoryOps.Api.Dtos.Common;
using BPInventoryOps.Api.Pages.Shared;
using BPInventoryOps.Api.Services;
using Microsoft.AspNetCore.Mvc;
namespace BPInventoryOps.Api.Pages.Categories;
public sealed class IndexModel(ICategoryService service) : UiPageModel
{
    [BindProperty(Name = "Query", SupportsGet = true)] public CategoryListQuery Query { get; set; } = new();
    public PagedResponse<CategoryResponse> Data { get; private set; } = null!;
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (InvalidQuery() is { } invalid) return invalid;
        Data = await service.ListAsync(Query, ct);
        return Page();
    }
}
