using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Grand.Core.Data.IntegrationData
{
    public class SqlDBRepository<T> : ISqlRepository<T> where T : class
    {
        private readonly IntegrationDataContext _context = null;

        public SqlDBRepository(IntegrationDataContext context)
        {
            _context = context;
        }
        
        public async Task<List<T>> GetAll()
        {
            return await _context.Set<T>().AsNoTracking().ToListAsync();
        }

        public async Task<IEnumerable<T>> GetWithRawSql(string query, params object[] parameters)
        {
            return await _context.Set<T>().ToListAsync(); //.FromSqlRaw(query, parameters).ToListAsync();
        }

        public async Task<IEnumerable<T>> GetBy(Expression<Func<T, bool>> filter = null, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null, string includeProperties = "")
        {
            IQueryable<T> query = _context.Set<T>();

            if (filter != null)
            {
                query = query.AsNoTracking().Where(filter);
            }

            if (includeProperties != null)
            {
                query = includeProperties.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries).Aggregate(query, (current, includeProperty) => current.Include(includeProperty));
            }

            if (orderBy != null)
            {
                return await orderBy(query).ToListAsync();
            }
        
            return await query.ToListAsync();
        }
        
        public async Task<IEnumerable<T>> GetPagedBy(Expression<Func<T, bool>> filter = null, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null, string includeProperties = "", int pageIndex = 1,
            int pageSize = 10)
        {
            IQueryable<T> query = _context.Set<T>();

            if (filter != null)
            {
                query = query.AsNoTracking().Where(filter);
            }

            if (includeProperties != null)
            {
                query = includeProperties.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries).Aggregate(query, (current, includeProperty) => current.Include(includeProperty));
            }

            if (orderBy != null)
            {
                return await orderBy(query).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            }

            return await query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
        }

        public async Task<T> GetOneBy(Expression<Func<T, bool>> filter = null, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null, string includeProperties = "")
        {
            IQueryable<T> query = _context.Set<T>();

            if (filter != null)
            {
                query = query.AsNoTracking().Where(filter);
            }

            if (includeProperties != null)
            {
                query = includeProperties.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries).Aggregate(query, (current, includeProperty) => current.Include(includeProperty));
            }

            if (orderBy != null)
            {
                return await orderBy(query).FirstOrDefaultAsync();
            }
        
            return await query.FirstOrDefaultAsync();
        }
    }
}