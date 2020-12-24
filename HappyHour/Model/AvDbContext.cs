using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

namespace HappyHour.Model
{
    //using  DbInitializer = SqliteDropCreateDatabaseWhenModelChanges<AvDbContext>;
    public class AvDbContext : DbContext
    {
        //public AvDbContext(string nameOrConnectionString)
        //    : base(nameOrConnectionString)
        protected override void OnConfiguring(DbContextOptionsBuilder optionbuilder)
        {
            optionbuilder.UseSqlite(@"Data Source=db\AvDb.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            AvModelConfig.Config(modelBuilder);

            //var initilizer = new DbInitializer(modelBuilder);
            //Database.SetInitializer(initilizer);
            base.OnModelCreating(modelBuilder);
        }
        public override int SaveChanges()
        {
            var entities = from e in ChangeTracker.Entries()
                           where e.State == EntityState.Added
                               || e.State == EntityState.Modified
                           select e.Entity;
            foreach (var entity in entities)
            {
                var validationContext = new ValidationContext(entity);
                Validator.ValidateObject(entity, validationContext);
            }

            return base.SaveChanges();
        }

        public DbSet<AvActorName> ActorNames { get; set; }
        public DbSet<AvActor> Actors { get; set; }
        public DbSet<AvGenre> Genres { get; set; }
        public DbSet<AvStudio> Studios { get; set; }
        public DbSet<AvSeries> Series { get; set; }
        public DbSet<AvItem> Items { get; set; }
    }
}
