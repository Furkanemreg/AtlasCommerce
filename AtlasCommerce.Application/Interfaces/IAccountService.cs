using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Application.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.Interfaces
{
    public interface IAccountService
    {
        Task<BaseResponse> LoginAsync(LoginVM loginVM);

        Task<BaseResponse> LogoutAsync();

        List<string?> GetAllRoles();

        Task<AppUserVM> GetByIdAsync(string id);

        Task<AppUserVM> GetByUserName(string userName);

        Task<BaseResponse> AddAsync(AppUserVM vm);

        Task<BaseResponse> UpdateAsync(AppUserVM vm);

        Task<BaseResponse> DeleteAsync(string id, string sessionUser);

        Task<BaseResponse> ResetPasswordAsync(ResetPasswordVM model);

        Task<PagedResult<AppUserVM>> GetAllAsync(int pageNumber, int pageSize, string? searchTerm = null);
        Task<PagedResult<AppUserVM>> GetAllUsersAsync(int pageNumber, int pageSize, string? searchTerm = null);
    }
}
