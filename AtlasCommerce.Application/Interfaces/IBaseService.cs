using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.Interfaces
{
    public interface IBaseService<T> where T : class
    {
        Task<List<T>> GetAllAsync();

        Task<List<T>> GetAllAsync(Expression<Func<T, bool>> predicate);

        Task<List<T>> GetAllAsync(params Expression<Func<T, object>>[] includes);

        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);

        Task<T?> GetByIdAsync(Guid id);

        Task<T?> GetByIdAsync(Guid id, params Expression<Func<T, object>>[] includes);

        Task AddAsync(T entity);

        Task AddRangeAsync(List<T> entities);

        Task UpdateAsync(T entity);

        Task UpdateRangeAsync(List<T> entities);

        Task DeleteAsync(T entity);

        Task HardDeleteAsync(T entity);

        Task<int> CountAllAsync();

        Task<int> CountWhereAsync(Expression<Func<T, bool>> predicate);

        Task<IEnumerable<T>> ExecuteStoredProcedureAsync(string storedProcedure, params object[] parameters);

        Task<bool> ExecuteNonQueryAsync(string storedProcedure, params object[] parameters);

        Task<bool> RemoveRangeAsync(List<T> entities);

        Task<(List<T> Items, int TotalCount)> GetPagedAsync(int pageNumber,
                                                  int pageSize,
                                                  Expression<Func<T, bool>>? predicate = null,
                                                  bool includeDeletedRecords = false,
                                                  params Expression<Func<T, object>>[] includes);
    }
}
