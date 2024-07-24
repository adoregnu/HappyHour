using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using HappyHour;
using System.IO;

namespace HappyHour.Model
{
    /*
    Tools -> Nuget Package Manager -> Package Manager Console

    PM> Add-Migration InitialCreate -Context MovieDbContext
    PM> Update-Database -Context MovieDbContext
     */
    public partial class MovieDbContext: DbContext
    {
        public DbSet<ShortText> ShortTexts { get; set; }
        public DbSet<LongText> LongTexts { get; set; }
        public DbSet<ImageBlob> Images { get; set; }
        public DbSet<Genre> MovieGenres { get; set; }
        public DbSet<ActorName> ActorNames { get; set; }
        public DbSet<Actor> Actors { get; set; }
        public DbSet<Label> Labels { get; set; } 
        public DbSet<Maker> Makers { get; set; }
        public DbSet<Series> Series { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<Movie> Movies { get; set; }

        public MovieDbContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //CREATE DATABASE avdb;
            //GRANT ALL privileges ON avdb.* TO 'adoregnu'@'%';
            string connectionUrl = "server=192.168.50.26; database=avdb; user=adoregnu; password=abcd1234";
            optionsBuilder.UseMySql(connectionUrl, ServerVersion.AutoDetect(connectionUrl));
        }

    }
}
