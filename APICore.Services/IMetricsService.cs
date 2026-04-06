using APICore.Common.DTO.Response;
using System.Threading;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface IMetricsService
    {
        Task<MetricsTrafficResponse> GetTrafficAsync(int businessId, string? period, CancellationToken cancellationToken = default);

        Task<MetricsProductsResponse> GetProductsAsync(int businessId, string? period, CancellationToken cancellationToken = default);

        Task<MetricsSalesResponse> GetSalesAsync(int businessId, string? period, CancellationToken cancellationToken = default);

        Task<MetricsCustomersResponse> GetCustomersAsync(int businessId, string? period, CancellationToken cancellationToken = default);
    }
}
