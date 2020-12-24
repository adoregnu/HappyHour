using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

namespace HappyHour.Model
{
    class AvModelConfig
    {
        public static void Config(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AvActorName>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<AvActor>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<AvStudio>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<AvGenre>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<AvSeries>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<AvItem>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();
        }
    }
}
