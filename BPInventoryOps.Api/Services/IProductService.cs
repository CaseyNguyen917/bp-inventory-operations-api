using BPInventoryOps.Api.Dtos.Common;
using BPInventoryOps.Api.Dtos.Products;

namespace BPInventoryOps.Api.Services;

public interface IProductService
{
    Task<PagedResponse<ProductResponse>> ListAsync(
        ProductListQuery request,
        CancellationToken cancellationToken);

    Task<PagedResponse<LowStockProductResponse>> ListLowStockAsync(
        LowStockProductQuery request,
        CancellationToken cancellationToken);

    Task<ProductResponse> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<ProductResponse> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken);

    Task<ProductResponse> UpdateAsync(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken);

    Task DeactivateAsync(int id, CancellationToken cancellationToken);

    Task<ProductResponse> ReactivateAsync(int id, CancellationToken cancellationToken);
}
