using BPInventoryOps.Api.Dtos.Products;
using BPInventoryOps.Api.Pages.Shared;
using BPInventoryOps.Api.Services;
namespace BPInventoryOps.Api.Pages.Products;
public sealed class DetailsModel(IProductService products) : UiPageModel
{
    public ProductResponse Product { get; private set; } = null!;
    public async Task OnGetAsync(int id, CancellationToken ct) => Product = await products.GetByIdAsync(id, ct);
}
