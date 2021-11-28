using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using HappyHour.Spider;

namespace HappyHour.ScrapItems
{
    class ItemR18V3  : ItemBase2
    {
        public ItemR18V3(SpiderBase spider) : base(spider)
        { 
        }

        protected override void UpdateDate(string date)
        {
            Match m = Regex.Match(date, @"([\w\.]+) (\d+), (\d+)");
            if (!m.Success)
            {
                return;
            }
            var mmm = m.Groups[1].Value.Substring(0, 3);
            var d = m.Groups[2].Value;
            var yyyy = m.Groups[3].Value;
            string newdate = $"{mmm} {d} {yyyy}";
            _avItem.DateReleased = DateTime.ParseExact(newdate, "MMM d yyyy", enUS);
        }
    }
}
