using AtlasCommerce.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Persistance.Services
{
    public class BaseService<T>(IRepository<T> repository) : IBaseService<T>
    where T : class
    {
        public virtual async Task<List<T>> GetAllAsync()
        {
            return await repository.GetAllAsync();
        }

        public virtual async Task<List<T>> GetAllAsync(Expression<Func<T, bool>> predicate)
        {
            return await repository.GetAllAsync(predicate);
        }
        public virtual async Task<List<T>> GetAllAsync(params Expression<Func<T, object>>[] includes)
        {
            return await repository.GetAllAsync(includes);
        }

        public virtual async Task<T?> GetByIdAsync(Guid id)
        {
            return await repository.GetByIdAsync(id);
        }

        public virtual Task<T?> GetByIdAsync(Guid id, params Expression<Func<T, object>>[] includes)
        {
            return repository.GetByIdAsync(id, includes);
        }

        public virtual async Task AddAsync(T entity)
        {
            await repository.AddAsync(entity);
        }

        public virtual async Task AddRangeAsync(List<T> entities)
        {
            await repository.AddRangeAsync(entities);
        }

        public virtual async Task UpdateAsync(T entity)
        {
            await repository.UpdateAsync(entity);
        }
        public virtual async Task UpdateRangeAsync(List<T> entities)
        {
            await repository.UpdateRangeAsync(entities);
        }

        public virtual async Task DeleteAsync(T entity)
        {
            await repository.DeleteAsync(entity);
        }

        public Task HardDeleteAsync(T entity)
        {
            return repository.HardDeleteAsync(entity);
        }

        public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await repository.FirstOrDefaultAsync(predicate);
        }

        public virtual async Task<int> CountAllAsync()
        {
            return await repository.CountAllAsync();
        }

        public virtual async Task<int> CountWhereAsync(Expression<Func<T, bool>> predicate)
        {
            return await repository.CountWhereAsync(predicate);
        }

        public virtual async Task<IEnumerable<T>> ExecuteStoredProcedureAsync(string storedProcedure, params object[] parameters)
        {
            return await repository.ExecuteStoredProcedureAsync(storedProcedure, parameters);
        }

        public virtual Task<bool> ExecuteNonQueryAsync(string storedProcedure, params object[] parameters)
        {
            return repository.ExecuteNonQueryAsync(storedProcedure, parameters);
        }

        public Task<bool> RemoveRangeAsync(List<T> entities)
        {
            return repository.RemoveRangeAsync(entities);
        }

        public virtual async Task<(List<T> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        bool includeDeletedRecords = false,
        params Expression<Func<T, object>>[] includes)
        {
            return await repository.GetPagedAsync(pageNumber, pageSize, predicate, includeDeletedRecords, includes);
        }
    }
}
