using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    class SpiderAvdbs : SpiderBase
    {
        public override string SearchURL =>
            $"{URL}menu/search.php?kwd={Keyword}&tab=2";

        public SpiderAvdbs(SpiderViewModel browser) : base(browser)
        {
            Name = "Avdbs";
            URL = "https://www.avdbs.com/";
        }

        public override void Scrap()
        {
            OnScrapCompleted();
        }
    }
}
