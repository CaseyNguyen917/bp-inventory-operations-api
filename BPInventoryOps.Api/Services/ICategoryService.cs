using BPInventoryOps.Api.Dtos.Categories;
using BPInventoryOps.Api.Dtos.Common;

namespace BPInventoryOps.Api.Services;

public interface ICategoryService
{
    Task<PagedResponse<CategoryResponse>> ListAsync(
        CategoryListQuery request,
        CancellationToken cancellationToken);

    Task<CategoryResponse> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<CategoryResponse> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken);

    Task<CategoryResponse> UpdateAsync(
        int id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken);

    Task DeactivateAsync(int id, CancellationToken cancellationToken);

    Task<CategoryResponse> ReactivateAsync(int id, CancellationToken cancellationToken);
}
