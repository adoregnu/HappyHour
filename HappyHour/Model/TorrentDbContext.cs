using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.ApplicationServices;

namespace HappyHour.Model
{
    public class TorrentDbContext : DbContext
    {
        public DbSet<Torrent>  Torrents {  get; set; }
        public DbSet<Magnet> Magnets { get; set; }
        public DbSet<StringData> Strings { get; set; }
        public TorrentDbContext()
        {
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //var dbpath = @"c: \Users\adore\AppData\Local\HappyHour";
            var dbpath = @"c:\Users\adore\source\repos\HappyHour\HappyHour";
            string connectionUrl = $@"Data Source={dbpath}\crawled.db";
            optionsBuilder.UseSqlite(connectionUrl);
        }

        public async ValueTask<Torrent> GetTorrent(string pid)
        {
            var  torrent = await Torrents
                .Include(t => t.MagnetUrls)
                .Include(t => t.Screenshots)
                .Where(t => t.PID == pid)
                .FirstOrDefaultAsync();
            if (torrent == null)
            {
                torrent = new Torrent()
                {
                    PID = pid, MagnetUrls  = [], Screenshots = [], StatusCode = 'N'
                };
                Torrents.Add(torrent);
            }
            return torrent;
        }
    }
}
