using APICore.Common.DTO.Request;
using APICore.Data.Entities;
using APICore.Services.Utils;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface IUserService
    {
        Task<User> CreateUser(CreateUserRequest user);

        Task DeleteUser(int id);

        Task UpdateUser(int id, UpdateUserRequest user);

        Task<User> GetUser(int id);
        
        Task<PaginatedList<User>> GetAllUsers(int? page, int? perPage, string sortOrder = null);

            
    }
}
