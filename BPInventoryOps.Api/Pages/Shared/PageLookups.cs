using BPInventoryOps.Api.Dtos.Categories;
using BPInventoryOps.Api.Dtos.Vendors;
using BPInventoryOps.Api.Dtos.Products;
using BPInventoryOps.Api.Services;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BPInventoryOps.Api.Pages.Shared;

public sealed class PageLookups(ICategoryService categories, IVendorService vendors, IProductService products)
{
    public async Task<List<SelectListItem>> CategoriesAsync(CancellationToken ct)
    {
        List<SelectListItem> result = [];
        for (int page = 1; ; page++)
        {
            var data = await categories.ListAsync(new CategoryListQuery { Page = page, PageSize = 100 }, ct);
            result.AddRange(data.Items.Select(x => new SelectListItem(x.Name, x.Id.ToString())));
            if (page >= data.TotalPages) return result;
        }
    }

    public async Task<List<SelectListItem>> VendorsAsync(CancellationToken ct, bool includeInactive = false)
    {
        List<SelectListItem> result = [];
        for (int page = 1; ; page++)
        {
            var data = await vendors.ListAsync(new VendorListQuery { Page = page, PageSize = 100, IncludeInactive = includeInactive }, ct);
            result.AddRange(data.Items.Select(x => new SelectListItem(x.Name + (x.IsActive ? "" : " (inactive)"), x.Id.ToString())));
            if (page >= data.TotalPages) return result;
        }
    }

    public async Task<List<SelectListItem>> ProductsAsync(CancellationToken ct, int? vendorId = null, bool includeInactive = false)
    {
        List<SelectListItem> result = [];
        for (int page = 1; ; page++)
        {
            var data = await products.ListAsync(new ProductListQuery { Page = page, PageSize = 100, VendorId = vendorId, IncludeInactive = includeInactive }, ct);
            result.AddRange(data.Items.Select(x => new SelectListItem(
                $"{x.Name} · {x.Sku} · {x.QuantityOnHand} in stock" + (x.IsActive ? "" : " (inactive)"), x.Id.ToString())));
            if (page >= data.TotalPages) return result;
        }
    }
}
