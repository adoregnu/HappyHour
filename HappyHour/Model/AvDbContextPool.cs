using CefSharp.DevTools.Target;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyHour.Model
{
    public class AvDbContextPool
    {
        readonly DbContextOptions<AvDbContext> _options;
        readonly PooledDbContextFactory<AvDbContext> _pool;
        static AvDbContextPool instance = null;

        public static AvDbContext CreateContext()
        { 
            instance ??= new AvDbContextPool();
            return instance._pool.CreateDbContext(); 
        }
        AvDbContextPool()
        {
            _options = new DbContextOptionsBuilder<AvDbContext>()
                .UseSqlite($@"Data Source={App.Current.LocalAppData}\db\AvDb.db")
                .Options;

            _pool = new PooledDbContextFactory<AvDbContext>(_options);
        }
    }
}
