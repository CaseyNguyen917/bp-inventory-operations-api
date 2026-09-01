using BPInventoryOps.Api.Dtos.Common;
using BPInventoryOps.Api.Dtos.Vendors;

namespace BPInventoryOps.Api.Services;

public interface IVendorService
{
    Task<PagedResponse<VendorResponse>> ListAsync(
        VendorListQuery request,
        CancellationToken cancellationToken);

    Task<VendorResponse> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<VendorResponse> CreateAsync(
        CreateVendorRequest request,
        CancellationToken cancellationToken);

    Task<VendorResponse> UpdateAsync(
        int id,
        UpdateVendorRequest request,
        CancellationToken cancellationToken);

    Task DeactivateAsync(int id, CancellationToken cancellationToken);

    Task<VendorResponse> ReactivateAsync(int id, CancellationToken cancellationToken);
}
