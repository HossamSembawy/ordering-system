using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;

namespace FulfilmentService.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        Task<T> AddAsync(T entity);
        Task<T?> GetByCondition(Expression<Func<T,bool>> expression);
        Task<List<T>?> GetListByCondition(Expression<Func<T, bool>> expression);
        Task<T> GetWithLocking(int id,string table);
        Task UpdateAsync(T entity);
        void DeleteAsync(T entity);

    }
}
