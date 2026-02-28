using APICore.Common.DTO;
using System.Threading.Tasks;

namespace APICore.Services
{

    public interface ICurrentUserContextProvider
    {
        Task<CurrentUserContext?> GetAsync(int userId);
    }
}
