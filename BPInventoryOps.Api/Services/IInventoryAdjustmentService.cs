using BPInventoryOps.Api.Dtos.Common;
using BPInventoryOps.Api.Dtos.InventoryAdjustments;

namespace BPInventoryOps.Api.Services;

public interface IInventoryAdjustmentService
{
    Task<PagedResponse<InventoryAdjustmentResponse>> ListAsync(
        InventoryAdjustmentListQuery request,
        CancellationToken cancellationToken);

    Task<InventoryAdjustmentResponse> GetByIdAsync(
        int id,
        CancellationToken cancellationToken);

    Task<InventoryAdjustmentResponse> CreateAsync(
        CreateInventoryAdjustmentRequest request,
        CancellationToken cancellationToken);
}
