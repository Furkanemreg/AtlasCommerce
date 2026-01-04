using AtlasCommerce.Application.Attributes;
using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Domain.Common;
using AtlasCommerce.Persistance.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Persistance.Services
{
    public class Repository<T>(ApplicationDbContext context) : IRepository<T> where T : BaseEntity
    {
        readonly DbSet<T> _entities = context.Set<T>();

        public virtual async Task<List<T>> GetAllAsync()
        {
            var entityType = typeof(T);

            if (Attribute.IsDefined(entityType, typeof(IgnoreCompanyFilterAttribute)))
            {
                return await _entities
                    .AsNoTracking()
                    .Where(i => !i.IsDeleted)
                    .ToListAsync();
            }

            return await _entities
                .AsNoTracking()
                .Where(i => !i.IsDeleted)
                .ToListAsync();
        }

        public async Task<int> CountAllAsync()
        {
            var entityType = typeof(T);

            if (Attribute.IsDefined(entityType, typeof(IgnoreCompanyFilterAttribute)))
            {
                return await _entities
                    .CountAsync(e => !e.IsDeleted);
            }

            return await _entities
                .CountAsync(e => !e.IsDeleted);
        }

        public async Task<T?> GetByIdAsync(Guid id)
        {
            var entityType = typeof(T);
            if (Attribute.IsDefined(entityType, typeof(IgnoreCompanyFilterAttribute)))
            {
                return await _entities.FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
            }

            return await _entities.FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
        }

        public Task<List<T>> GetAllAsync(Expression<Func<T, bool>> predicate)
        {
            var entityType = typeof(T);
            if (Attribute.IsDefined(entityType, typeof(IgnoreCompanyFilterAttribute)))
            {
                return _entities.Where(i => !i.IsDeleted).Where(predicate).ToListAsync();
            }

            return _entities.Where(e => !e.IsDeleted).Where(predicate).ToListAsync();
        }
        public async Task<List<T>> GetAllAsync(params Expression<Func<T, object>>[] includes)
        {
            var entityType = typeof(T);

            IQueryable<T> query = _entities.AsNoTracking();

            query = query.Where(e => !e.IsDeleted);

            //if (!Attribute.IsDefined(entityType, typeof(IgnoreCompanyFilterAttribute)))
            //{
            //    query = query.Where(e => e.CompanyId == companyId);
            //}

            // Include'ları uygula
            if (includes != null && includes.Length > 0)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            return await query.ToListAsync();
        }

        public async Task<T> AddAsync(T entity)
        {
            await _entities.AddAsync(entity);
            return entity;
        }

        public Task AddRangeAsync(List<T> entities)
        {
            _entities.AddRange(entities);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(T entity)
        {
            _entities.Update(entity);
            return Task.CompletedTask;
        }

        public Task UpdateRangeAsync(List<T> entities)
        {
            _entities.UpdateRange(entities);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(T entity)
        {
            entity.IsDeleted = true;
            _entities.Update(entity);

            return Task.CompletedTask;
        }

        public Task HardDeleteAsync(T entity)
        {
            _entities.Remove(entity);
            return Task.CompletedTask;
        }

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            var entityType = typeof(T);

            if (Attribute.IsDefined(entityType, typeof(IgnoreCompanyFilterAttribute)))
            {
                return await _entities
                    .Where(e => !e.IsDeleted)
                    .FirstOrDefaultAsync(predicate);
            }

            return await _entities
                .Where(e => !e.IsDeleted)
                .FirstOrDefaultAsync(predicate);
        }

        public async Task<int> CountWhereAsync(Expression<Func<T, bool>> predicate)
        {
            var entityType = typeof(T);

            if (Attribute.IsDefined(entityType, typeof(IgnoreCompanyFilterAttribute)))
            {
                return await _entities
                    .Where(e => !e.IsDeleted)
                    .CountAsync(predicate);
            }

            return await _entities
                .Where(e => !e.IsDeleted)
                .CountAsync(predicate);
        }

        public Task<bool> RemoveRangeAsync(List<T> entities)
        {
            _entities.RemoveRange(entities);
            return Task.FromResult(true);
        }

        public Task<T?> GetByIdAsync(Guid id, params Expression<Func<T, object>>[] includes)
        {
            var entityType = typeof(T);
            var query = _entities.AsQueryable();
            query = includes.Aggregate(query, (current, include) => current.Include(include));

            if (Attribute.IsDefined(entityType, typeof(IgnoreCompanyFilterAttribute)))
            {
                return query.FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
            }

            return query.FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
        }

        public async Task<IEnumerable<T>> ExecuteStoredProcedureAsync(string storedProcedure, params object[] parameters)
        {
            return await context.Set<T>().FromSqlRaw(storedProcedure, parameters).AsNoTracking().ToListAsync();
        }

        public async Task<bool> ExecuteNonQueryAsync(string storedProcedure, params object[] parameters)
        {
            var rowsAffected = await context.Database.ExecuteSqlRawAsync(storedProcedure, parameters);
            return rowsAffected > 0;
        }

        public async Task<(List<T> Items, int TotalCount)> GetPagedAsync(
    int pageNumber,
    int pageSize,
    Expression<Func<T, bool>>? predicate = null,
    bool includeDeletedRecords = false,
    params Expression<Func<T, object>>[] includes)
        {
            var query = _entities.AsQueryable();
            var entityType = typeof(T);

            if (Attribute.IsDefined(entityType, typeof(IgnoreCompanyFilterAttribute)))
            {
                query = includeDeletedRecords
                    ? query.Where(e => e.IsDeleted)
                    : query.Where(e => !e.IsDeleted);
            }
            else
            {
                query = includeDeletedRecords
                    ? query.Where(e => e.IsDeleted)
                    : query.Where(e => !e.IsDeleted);
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            int totalCount = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

    }
}
