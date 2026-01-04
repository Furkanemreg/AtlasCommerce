using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Persistance.Context;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Persistance.Services
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        //private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UnitOfWork> _logger;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(ApplicationDbContext context,
            ILogger<UnitOfWork> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> Commit()
        {
            int result;

            try
            {
                result = await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency exception occurred.");
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdate exception occurred.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred.");
                throw;
            }

            return result;
        }

        public async Task Rollback()
        {
            await _context.DisposeAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        // TRANSACTION KULLANMAK İSTERSEK

        public async Task CommitInTransaction()
        {
            int result;

            try
            {
                result = await _context.SaveChangesAsync();
                if (_transaction != null)
                {
                    await _transaction.CommitAsync();
                    _transaction.Dispose();
                    _transaction = null;
                }
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency exception occurred.");
                if (_transaction != null)
                {
                    await _transaction.RollbackAsync();
                    _transaction.Dispose();
                    _transaction = null;
                }
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdate exception occurred.");
                if (_transaction != null)
                {
                    await _transaction.RollbackAsync();
                    _transaction.Dispose();
                    _transaction = null;
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred.");
                if (_transaction != null)
                {
                    await _transaction.RollbackAsync();
                    _transaction.Dispose();
                    _transaction = null;
                }
                throw;
            }
        }
        public async Task RollbackInTransaction()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                _transaction.Dispose();
                _transaction = null;
            }
        }

        public void DisposeInTransaction()
        {
            if (_transaction != null)
            {
                _transaction.Dispose();
                _transaction = null;
            }
            _context.Dispose();
        }
    }
}
