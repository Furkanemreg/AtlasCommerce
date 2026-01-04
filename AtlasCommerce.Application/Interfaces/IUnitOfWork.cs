using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        Task<int> Commit();
        Task Rollback();
        Task CommitInTransaction();
        Task RollbackInTransaction();

        void DisposeInTransaction();
    }
}
