using System.Threading.Tasks;

namespace APICore.Services
{
    public interface ISubscriptionQuotaService
    {
        Task EnsureCanAddProductAsync(int organizationId);
        Task EnsureCanAddUserAsync(int organizationId);
        Task EnsureCanAddLocationAsync(int organizationId);
    }
}
