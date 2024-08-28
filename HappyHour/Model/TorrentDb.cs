using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HappyHour.Model
{
    public class StringData
    {
        [Key]
        public int Key { get; set; }
        public string Value { get; set; }
    }

    public class Magnet
    {
        [Key]
        public int Key { get; set; }
        public string SourceUrl { get; set; }
        public string MagnetUrl { get; set; }
    }
    public class Torrent
    {
        [Key]
        public long Key { get; set; }
        [MaxLength(32)]
        public string PID { get; set; }
        public DateTime Date { get; set; }
        public string CoverPath { get; set; }
        public ICollection<Magnet> MagnetUrls { get; set; }
        public ICollection<StringData> Screenshots { get; set; }
        public char StatusCode { get; set; } //C (Crawled), E(Excluded), D(Downloaed),
    }
}
