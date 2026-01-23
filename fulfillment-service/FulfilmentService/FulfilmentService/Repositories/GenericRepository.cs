using FulfilmentService.Database;
using FulfilmentService.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;

namespace FulfilmentService.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly FulfilmentDbContext _dbContext;

        public GenericRepository(FulfilmentDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<T> AddAsync(T entity)
        {
            try
            {
                await _dbContext.AddAsync(entity);
                await _dbContext.SaveChangesAsync();
                return entity;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public void DeleteAsync(T entity)
        {
            try
            {
                _dbContext.Set<T>().Remove(entity);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<T?> GetByCondition(Expression<Func<T, bool>> expression)
        {
            var entity = await _dbContext.Set<T>().FirstOrDefaultAsync(expression);
            return entity;
        }
        public async Task<List<T>?> GetListByCondition(Expression<Func<T, bool>> expression)
        {
            var entity = await _dbContext.Set<T>().Where(expression).ToListAsync();
            return entity;
        }

        public Task<T> GetWithLocking(int id, string table)
        {
            var worker = _dbContext.Set<T>().FromSqlRaw($"SELECT * FROM {table} WITH (UPDLOCK, ROWLOCK) WHERE Id = {id}").FirstOrDefaultAsync();
            return worker;
        }

        


        public async Task UpdateAsync(T entity)
        {
            var updatedEntity = _dbContext.Set<T>().Update(entity);
            await _dbContext.SaveChangesAsync();
        }
    }
}
