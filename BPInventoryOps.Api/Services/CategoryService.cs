using System.Text.Json;
using BPInventoryOps.Api.Auth;
using BPInventoryOps.Api.Data;
using BPInventoryOps.Api.Dtos.Categories;
using BPInventoryOps.Api.Dtos.Common;
using BPInventoryOps.Api.Entities;
using BPInventoryOps.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace BPInventoryOps.Api.Services;

public sealed class CategoryService(
    ApplicationDbContext dbContext,
    IAuditService auditService) : ICategoryService
{
    public async Task<PagedResponse<CategoryResponse>> ListAsync(
        CategoryListQuery request,
        CancellationToken cancellationToken)
    {
        ValidateSort(request.SortBy, request.SortDirection);

        IQueryable<Category> query = dbContext.Categories.AsNoTracking();

        if (!request.IncludeInactive)
        {
            query = query.Where(category => category.IsActive);
        }

        string? search = NormalizeOptional(request.Search);
        if (search is not null)
        {
            query = query.Where(category => category.Name.Contains(search));
        }

        int totalCount = await query.CountAsync(cancellationToken);

        query = request.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? query.OrderByDescending(category => category.Name).ThenByDescending(category => category.Id)
            : query.OrderBy(category => category.Name).ThenBy(category => category.Id);

        List<CategoryResponse> items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(category => new CategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                IsActive = category.IsActive,
                CreatedAtUtc = category.CreatedAtUtc,
                UpdatedAtUtc = category.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return PagedResponse<CategoryResponse>.Create(
            items,
            request.Page,
            request.PageSize,
            totalCount);
    }

    public async Task<CategoryResponse> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        CategoryResponse? category = await dbContext.Categories
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new CategoryResponse
            {
                Id = item.Id,
                Name = item.Name,
                IsActive = item.IsActive,
                CreatedAtUtc = item.CreatedAtUtc,
                UpdatedAtUtc = item.UpdatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        return category ?? throw new NotFoundException($"Category {id} was not found.");
    }

    public async Task<CategoryResponse> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        string name = request.Name.Trim();

        if (await dbContext.Categories.AnyAsync(
                category => category.Name == name,
                cancellationToken))
        {
            throw new ConflictException($"A Category named '{name}' already exists.");
        }

        DateTime now = DateTime.UtcNow;
        Category category = new()
        {
            Name = name,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.Categories.Add(category);

        auditService.Add(
            AuditActions.CategoryCreated,
            nameof(Category),
            null,
            JsonSerializer.Serialize(new { category.Name }));

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(category);
    }

    public async Task<CategoryResponse> UpdateAsync(
        int id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        Category category = await dbContext.Categories
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Category {id} was not found.");

        string name = request.Name.Trim();

        if (await dbContext.Categories.AnyAsync(
                item => item.Id != id && item.Name == name,
                cancellationToken))
        {
            throw new ConflictException($"A Category named '{name}' already exists.");
        }

        category.Name = name;
        category.UpdatedAtUtc = DateTime.UtcNow;

        auditService.Add(
            AuditActions.CategoryUpdated,
            nameof(Category),
            category.Id.ToString(),
            JsonSerializer.Serialize(new { category.Name }));

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(category);
    }

    public async Task DeactivateAsync(int id, CancellationToken cancellationToken)
    {
        Category category = await dbContext.Categories
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Category {id} was not found.");

        if (!category.IsActive)
        {
            return;
        }

        bool hasActiveProducts = await dbContext.Products.AnyAsync(
            product => product.CategoryId == id && product.IsActive,
            cancellationToken);

        if (hasActiveProducts)
        {
            throw new ConflictException(
                "The Category cannot be deactivated while active Products reference it.");
        }

        category.IsActive = false;
        category.UpdatedAtUtc = DateTime.UtcNow;

        auditService.Add(
            AuditActions.CategoryDeactivated,
            nameof(Category),
            category.Id.ToString(),
            null);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<CategoryResponse> ReactivateAsync(
        int id,
        CancellationToken cancellationToken)
    {
        Category category = await dbContext.Categories
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Category {id} was not found.");

        if (category.IsActive)
        {
            return ToResponse(category);
        }

        category.IsActive = true;
        category.UpdatedAtUtc = DateTime.UtcNow;

        auditService.Add(
            AuditActions.CategoryReactivated,
            nameof(Category),
            category.Id.ToString(),
            null);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(category);
    }

    private static void ValidateSort(string sortBy, string sortDirection)
    {
        if (string.IsNullOrWhiteSpace(sortBy)
            || !sortBy.Equals("name", StringComparison.OrdinalIgnoreCase))
        {
            throw new RequestValidationException("Category sortBy must be 'name'.");
        }

        if (string.IsNullOrWhiteSpace(sortDirection)
            || (!sortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase)
                && !sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase)))
        {
            throw new RequestValidationException("sortDirection must be 'asc' or 'desc'.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static CategoryResponse ToResponse(Category category)
    {
        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            IsActive = category.IsActive,
            CreatedAtUtc = category.CreatedAtUtc,
            UpdatedAtUtc = category.UpdatedAtUtc
        };
    }
}
