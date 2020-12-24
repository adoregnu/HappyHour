using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyHour.Model
{
    class AvModelConfig
    {
        public static void Config(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AvActorName>();
            modelBuilder.Entity<AvActor>();
            modelBuilder.Entity<AvStudio>();
            modelBuilder.Entity<AvGenre>();
            modelBuilder.Entity<AvSeries>();
            modelBuilder.Entity<AvItem>();
        }
    }
}
