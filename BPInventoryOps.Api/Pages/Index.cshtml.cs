using BPInventoryOps.Api.Dtos.Products;
using BPInventoryOps.Api.Dtos.Restocks;
using BPInventoryOps.Api.Pages.Shared;
using BPInventoryOps.Api.Services;
namespace BPInventoryOps.Api.Pages;
public sealed class IndexModel(IProductService products, IRestockService restocks) : UiPageModel
{
    public int ProductCount { get; private set; }
    public int LowCount { get; private set; }
    public int RestockCount { get; private set; }
    public IReadOnlyList<LowStockProductResponse> LowStock { get; private set; } = [];
    public IReadOnlyList<RestockSummaryResponse> RecentRestocks { get; private set; } = [];
    public async Task OnGetAsync(CancellationToken ct)
    {
        ProductCount = (await products.ListAsync(new ProductListQuery { PageSize = 1 }, ct)).TotalCount;
        var low = await products.ListLowStockAsync(new LowStockProductQuery { PageSize = 5 }, ct);
        LowCount = low.TotalCount;
        LowStock = low.Items;
        var recent = await restocks.ListAsync(new RestockListQuery { PageSize = 4 }, ct);
        RestockCount = recent.TotalCount;
        RecentRestocks = recent.Items;
    }
}
