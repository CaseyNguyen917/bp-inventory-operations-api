using BPInventoryOps.Api.Dtos.Common;
using BPInventoryOps.Api.Dtos.Restocks;

namespace BPInventoryOps.Api.Services;

public interface IRestockService
{
    Task<PagedResponse<RestockSummaryResponse>> ListAsync(
        RestockListQuery request,
        CancellationToken cancellationToken);

    Task<RestockResponse> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<RestockResponse> CreateAsync(
        CreateRestockRequest request,
        CancellationToken cancellationToken);
}
