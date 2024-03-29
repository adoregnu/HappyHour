﻿using System.ComponentModel.DataAnnotations;
using System.Linq;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace HappyHour.Model
{
    public class AvDbContext : DbContext
    {

        public AvDbContext(DbContextOptions<AvDbContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            AvModelConfig.Config(modelBuilder);
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
