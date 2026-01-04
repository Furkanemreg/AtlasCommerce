using AutoMapper;
using AutoMapper.QueryableExtensions;
using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.ViewModels;
using AtlasCommerce.Application.Wrappers;
using AtlasCommerce.Domain.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Persistance.Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IMapper _mapper;

        public AccountService(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, RoleManager<IdentityRole<Guid>> roleManager, IMapper mapper)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _mapper = mapper;
        }

        public async Task<BaseResponse> LoginAsync(LoginVM loginVM)
        {
            AppUser user = await _userManager.FindByNameAsync(loginVM.UserName);

            if (user is null || !user.IsActive)
                return new BaseResponse() { Success = false, Message = string.Format("{0} {1}", loginVM.UserName, "kullanıcı adı ile bir hesap yoktur.") };

            user.EmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
            user.PhoneNumberConfirmed = await _userManager.IsPhoneNumberConfirmedAsync(user);
            await _userManager.UpdateAsync(user);

            await _signInManager.SignOutAsync();

            var signInResult = await _signInManager.PasswordSignInAsync(user, loginVM.Password, loginVM.RememberMe, false);

            if (signInResult.Succeeded)
                return new BaseResponse() { Success = true, Message = string.Empty };

            return new BaseResponse() { Success = false, Message = "Kullanıcı adı veya şifre hatalıdır." };
        }

        public async Task<BaseResponse> LogoutAsync()
        {
            await _signInManager.SignOutAsync();
            return new BaseResponse() { Success = true, Message = string.Empty };
        }

        public async Task<PagedResult<AppUserVM>> GetAllAsync(int pageNumber = 1,int pageSize = 10,string? searchTerm = null)
        {
            //var query = _userManager.Users.AsQueryable();
            var userRoleUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var userIds = userRoleUsers.Select(u => u.Id);

            var query = _userManager.Users
                .Where(u => userIds.Contains(u.Id));

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim().ToLower();
                query = query.Where(u => u.UserName.ToLower().Contains(searchTerm) ||
                                         u.Email.ToLower().Contains(searchTerm) ||
                                         u.FirstName.ToLower().Contains(searchTerm) ||
                                         u.LastName.ToLower().Contains(searchTerm));
            }

            var totalCount = await query.CountAsync();
            var skip = (pageNumber - 1) * pageSize;

            var items = await query.OrderBy(u => u.CreatedAt).Skip(skip).Take(pageSize).ProjectTo<AppUserVM>(_mapper.ConfigurationProvider).ToListAsync();

            foreach (var vm in items)
            {
                if (vm.CreatedBy.HasValue)
                {
                    var cu = await _userManager.FindByIdAsync(vm.CreatedBy.Value.ToString());
                    if (cu != null)
                        vm.CreatedUser = _mapper.Map<AppUserVM>(cu);
                }
                if (vm.UpdatedBy.HasValue)
                {
                    var uu = await _userManager.FindByIdAsync(vm.UpdatedBy.Value.ToString());
                    if (uu != null)
                        vm.UpdatedUser = _mapper.Map<AppUserVM>(uu);
                }

                var userEntity = await _userManager.FindByIdAsync(vm.Id.ToString());
                var roles = await _userManager.GetRolesAsync(userEntity);
                vm.Roles = roles.ToList();

                vm.UserRole = roles.FirstOrDefault();
            }

            return new PagedResult<AppUserVM>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm
            };
        }

        public async Task<PagedResult<AppUserVM>> GetAllUsersAsync(int pageNumber = 1, int pageSize = 10, string? searchTerm = null)
        {
            //var query = _userManager.Users.AsQueryable();
            var userRoleUsers = await _userManager.GetUsersInRoleAsync("User");
            var userIds = userRoleUsers.Select(u => u.Id);

            var query = _userManager.Users
                .Where(u => userIds.Contains(u.Id));

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim().ToLower();
                query = query.Where(u => u.UserName.ToLower().Contains(searchTerm) ||
                                         u.Email.ToLower().Contains(searchTerm) ||
                                         u.FirstName.ToLower().Contains(searchTerm) ||
                                         u.LastName.ToLower().Contains(searchTerm));
            }

            var totalCount = await query.CountAsync();
            var skip = (pageNumber - 1) * pageSize;

            var items = await query.OrderBy(u => u.CreatedAt).Skip(skip).Take(pageSize).ProjectTo<AppUserVM>(_mapper.ConfigurationProvider).ToListAsync();

            foreach (var vm in items)
            {
                if (vm.CreatedBy.HasValue)
                {
                    var cu = await _userManager.FindByIdAsync(vm.CreatedBy.Value.ToString());
                    if (cu != null)
                        vm.CreatedUser = _mapper.Map<AppUserVM>(cu);
                }
                if (vm.UpdatedBy.HasValue)
                {
                    var uu = await _userManager.FindByIdAsync(vm.UpdatedBy.Value.ToString());
                    if (uu != null)
                        vm.UpdatedUser = _mapper.Map<AppUserVM>(uu);
                }

                var userEntity = await _userManager.FindByIdAsync(vm.Id.ToString());
                var roles = await _userManager.GetRolesAsync(userEntity);
                vm.Roles = roles.ToList();

                vm.UserRole = roles.FirstOrDefault();
            }

            return new PagedResult<AppUserVM>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm
            };
        }

        public async Task<AppUserVM> GetByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return new AppUserVM();

            AppUser user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return new AppUserVM();

            AppUserVM vm = _mapper.Map<AppUserVM>(user);

            if (user.CreatedBy != null)
            {
                var createdUser = await _userManager.FindByIdAsync(user.CreatedBy.ToString());
                if (createdUser != null)
                    vm.CreatedUser = _mapper.Map<AppUserVM>(createdUser);
            }

            if (user.UpdatedBy != null)
            {
                var updatedUser = await _userManager.FindByIdAsync(user.UpdatedBy.ToString());
                if (updatedUser != null)
                    vm.UpdatedUser = _mapper.Map<AppUserVM>(updatedUser);
            }

            var roles = await _userManager.GetRolesAsync(user);
            vm.Roles = roles.ToList();
            vm.UserRole = roles.FirstOrDefault();

            return vm;
        }

        public async Task<AppUserVM> GetByUserName(string userName)
        {
            if (string.IsNullOrEmpty(userName))
                return new AppUserVM();

            return await _userManager.FindByNameAsync(userName).ContinueWith(task => _mapper.Map<AppUserVM>(task.Result));
        }

        public List<string?> GetAllRoles()
        {
            List<string?> allRoles = _roleManager.Roles
                   .Select(r => r.Name)
                   .OrderBy(n => n)
                   .ToList();

            return allRoles;
        }

        public async Task<BaseResponse> AddAsync(AppUserVM vm)
        {
            if (vm == null || vm.Id != Guid.Empty)
                return new BaseResponse { Success = false, Message = "Kullanıcı oluşturulurken bir hata oluştu. Lütfen bilgileri kontrol edin." };

            if (string.IsNullOrWhiteSpace(vm.Password))
                return new BaseResponse { Success = false, Message = "Şifre bilgisi boş olamaz." };

            AppUser isSuitable = await _userManager.FindByNameAsync(vm.UserName);
            if(isSuitable != null)
                return new BaseResponse { Success = false, Message = string.Format("{0} {1}", vm.UserName, "kullanıcı adı sistemde zaten kayıtlıdır.") };

            var user = _mapper.Map<AppUser>(vm);

            var createResult = await _userManager.CreateAsync(user, vm.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                return new BaseResponse { Success = false, Message = $"Kullanıcı oluşturulamadı: {errors}" };
            }

            var roleToAdd = string.IsNullOrEmpty(vm.UserRole) ? "Admin" : vm.UserRole;

            if (!await _roleManager.RoleExistsAsync(roleToAdd))
                return new BaseResponse { Success = false, Message = $"‘{roleToAdd}’ rolü sistemde tanımlı değil." };

            var roleResult = await _userManager.AddToRoleAsync(user, roleToAdd);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                return new BaseResponse { Success = false, Message = $"Kullanıcı role atanamadı: {errors}" };
            }

            return new BaseResponse { Success = true, Message = "Kullanıcı başarıyla oluşturuldu."};
        }

        public async Task<BaseResponse> UpdateAsync(AppUserVM vm)
        {
            if (vm == null || vm.Id == Guid.Empty)
                return new BaseResponse { Success = false,  Message = "Kullanıcı bilgileri eksik." };

            var user = await _userManager.FindByIdAsync(vm.Id.ToString());
            if (user == null)
                return new BaseResponse { Success = false, Message = "Kullanıcı bulunamadı." };

            AppUser isSuitable = await _userManager.FindByNameAsync(vm.UserName);
            if (user.UserName != vm.UserName && isSuitable != null)
                return new BaseResponse { Success = false, Message = string.Format("{0} {1}", vm.UserName, "kullanıcı adı sistemde zaten kayıtlıdır.") };

            user.UserName = vm.UserName;
            user.FirstName = vm.FirstName;
            user.LastName = vm.LastName;
            user.Email = vm.Email;
            user.PhoneNumber = vm.PhoneNumber;
            user.IsActive = vm.IsActive;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                return new BaseResponse { Success = false, Message = $"Kullanıcı bilgileri güncellenemedi: {errors}" };
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                    return new BaseResponse { Success = false, Message = $"Eski roller kaldırılırken hata: {errors}" };
                }
            }

            var newRole = vm.UserRole?.Trim();
            if (!string.IsNullOrEmpty(newRole))
            {
                if (!await _roleManager.RoleExistsAsync(newRole))
                    return new BaseResponse { Success = false, Message = $"‘{newRole}’ rolü sistemde tanımlı değil." };

                var addResult = await _userManager.AddToRoleAsync(user, newRole);
                if (!addResult.Succeeded)
                {
                    var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                    return new BaseResponse { Success = false, Message = $"Yeni rol atanamadı: {errors}" };
                }
            }

            return new BaseResponse { Success = true, Message = "Kullanıcı başarıyla güncellendi." };
        }

        public async Task<BaseResponse> DeleteAsync(string id, string sessionUser)
        {
            if (string.IsNullOrEmpty(id))
                return new BaseResponse() { Success = false, Message = "Kullanıcı bulunamadı." };

            AppUser user = await _userManager.FindByIdAsync(id);

            if (user is null)
                return new BaseResponse() { Success = false, Message = "Kullanıcı bulunamadı." };

            if(sessionUser.Equals(user.UserName))
                return new BaseResponse() { Success = false, Message = "Kendi kullanıcınızı silemezsiniz." };

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Any())
            {
                var removeRolesResult = await _userManager.RemoveFromRolesAsync(user, roles);
                if (!removeRolesResult.Succeeded)
                {
                    var roleErrors = string.Join(", ", removeRolesResult.Errors.Select(e => e.Description));
                    return new BaseResponse() { Success = false, Message = "Kullanıcı rollerini silerken hata oluştu: " + roleErrors };
                }
            }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                return new BaseResponse() { Success = true, Message = "Kullanıcı başarıyla silindi." };
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new BaseResponse() { Success = false, Message = "Silme işlemi başarısız: " + errors };
            }
        }

        public async Task<BaseResponse> ResetPasswordAsync(ResetPasswordVM model)
        {
            if (model is null || model.Id.Equals(Guid.Empty))
                return new BaseResponse { Success = false, Message = "Şifre sıfırlama bilgileri eksik." };

            AppUser user = await _userManager.FindByIdAsync(model.Id.ToString());
            if (user is null)
                return new BaseResponse { Success = false, Message = "Kullanıcı bulunamadı." };

            await _userManager.RemovePasswordAsync(user);
            var result = await _userManager.AddPasswordAsync(user, model.Password);
            return new BaseResponse() { Success = result.Succeeded, Message = result.Succeeded ? "Şifre başarıyla sıfırlandı." : string.Join(", ", result.Errors.Select(e => e.Description)) };
        }
    }
}
