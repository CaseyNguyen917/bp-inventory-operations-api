using System.Text.Json;
using BPInventoryOps.Api.Auth;
using BPInventoryOps.Api.Data;
using BPInventoryOps.Api.Dtos.Common;
using BPInventoryOps.Api.Dtos.Vendors;
using BPInventoryOps.Api.Entities;
using BPInventoryOps.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace BPInventoryOps.Api.Services;

public sealed class VendorService(
    ApplicationDbContext dbContext,
    IAuditService auditService) : IVendorService
{
    public async Task<PagedResponse<VendorResponse>> ListAsync(
        VendorListQuery request,
        CancellationToken cancellationToken)
    {
        ValidateSort(request.SortBy, request.SortDirection);

        IQueryable<Vendor> query = dbContext.Vendors.AsNoTracking();

        if (!request.IncludeInactive)
        {
            query = query.Where(vendor => vendor.IsActive);
        }

        string? search = NormalizeOptional(request.Search);
        if (search is not null)
        {
            query = query.Where(vendor => vendor.Name.Contains(search));
        }

        int totalCount = await query.CountAsync(cancellationToken);

        query = request.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? query.OrderByDescending(vendor => vendor.Name).ThenByDescending(vendor => vendor.Id)
            : query.OrderBy(vendor => vendor.Name).ThenBy(vendor => vendor.Id);

        List<VendorResponse> items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(vendor => new VendorResponse
            {
                Id = vendor.Id,
                Name = vendor.Name,
                ContactName = vendor.ContactName,
                Phone = vendor.Phone,
                Email = vendor.Email,
                IsActive = vendor.IsActive,
                CreatedAtUtc = vendor.CreatedAtUtc,
                UpdatedAtUtc = vendor.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return PagedResponse<VendorResponse>.Create(
            items,
            request.Page,
            request.PageSize,
            totalCount);
    }

    public async Task<VendorResponse> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        VendorResponse? vendor = await dbContext.Vendors
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new VendorResponse
            {
                Id = item.Id,
                Name = item.Name,
                ContactName = item.ContactName,
                Phone = item.Phone,
                Email = item.Email,
                IsActive = item.IsActive,
                CreatedAtUtc = item.CreatedAtUtc,
                UpdatedAtUtc = item.UpdatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        return vendor ?? throw new NotFoundException($"Vendor {id} was not found.");
    }

    public async Task<VendorResponse> CreateAsync(
        CreateVendorRequest request,
        CancellationToken cancellationToken)
    {
        string name = request.Name.Trim();

        if (await dbContext.Vendors.AnyAsync(
                vendor => vendor.Name == name,
                cancellationToken))
        {
            throw new ConflictException($"A Vendor named '{name}' already exists.");
        }

        DateTime now = DateTime.UtcNow;
        Vendor vendor = new()
        {
            Name = name,
            ContactName = NormalizeOptional(request.ContactName),
            Phone = NormalizeOptional(request.Phone),
            Email = NormalizeOptional(request.Email),
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.Vendors.Add(vendor);

        auditService.Add(
            AuditActions.VendorCreated,
            nameof(Vendor),
            null,
            JsonSerializer.Serialize(new { vendor.Name }));

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(vendor);
    }

    public async Task<VendorResponse> UpdateAsync(
        int id,
        UpdateVendorRequest request,
        CancellationToken cancellationToken)
    {
        Vendor vendor = await dbContext.Vendors
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Vendor {id} was not found.");

        string name = request.Name.Trim();

        if (await dbContext.Vendors.AnyAsync(
                item => item.Id != id && item.Name == name,
                cancellationToken))
        {
            throw new ConflictException($"A Vendor named '{name}' already exists.");
        }

        vendor.Name = name;
        vendor.ContactName = NormalizeOptional(request.ContactName);
        vendor.Phone = NormalizeOptional(request.Phone);
        vendor.Email = NormalizeOptional(request.Email);
        vendor.UpdatedAtUtc = DateTime.UtcNow;

        auditService.Add(
            AuditActions.VendorUpdated,
            nameof(Vendor),
            vendor.Id.ToString(),
            JsonSerializer.Serialize(new { vendor.Name }));

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(vendor);
    }

    public async Task DeactivateAsync(int id, CancellationToken cancellationToken)
    {
        Vendor vendor = await dbContext.Vendors
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Vendor {id} was not found.");

        if (!vendor.IsActive)
        {
            return;
        }

        bool hasActiveProducts = await dbContext.Products.AnyAsync(
            product => product.PrimaryVendorId == id && product.IsActive,
            cancellationToken);

        if (hasActiveProducts)
        {
            throw new ConflictException(
                "The Vendor cannot be deactivated while active Products reference it.");
        }

        vendor.IsActive = false;
        vendor.UpdatedAtUtc = DateTime.UtcNow;

        auditService.Add(
            AuditActions.VendorDeactivated,
            nameof(Vendor),
            vendor.Id.ToString(),
            null);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<VendorResponse> ReactivateAsync(
        int id,
        CancellationToken cancellationToken)
    {
        Vendor vendor = await dbContext.Vendors
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Vendor {id} was not found.");

        if (vendor.IsActive)
        {
            return ToResponse(vendor);
        }

        vendor.IsActive = true;
        vendor.UpdatedAtUtc = DateTime.UtcNow;

        auditService.Add(
            AuditActions.VendorReactivated,
            nameof(Vendor),
            vendor.Id.ToString(),
            null);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(vendor);
    }

    private static void ValidateSort(string sortBy, string sortDirection)
    {
        if (string.IsNullOrWhiteSpace(sortBy)
            || !sortBy.Equals("name", StringComparison.OrdinalIgnoreCase))
        {
            throw new RequestValidationException("Vendor sortBy must be 'name'.");
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

    private static VendorResponse ToResponse(Vendor vendor)
    {
        return new VendorResponse
        {
            Id = vendor.Id,
            Name = vendor.Name,
            ContactName = vendor.ContactName,
            Phone = vendor.Phone,
            Email = vendor.Email,
            IsActive = vendor.IsActive,
            CreatedAtUtc = vendor.CreatedAtUtc,
            UpdatedAtUtc = vendor.UpdatedAtUtc
        };
    }
}
