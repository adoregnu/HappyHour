using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using HappyHour.ScrapItems;
using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    class SpiderJavfree : SpiderBase
    {
        public override string SearchURL => $"{URL}?s={Keyword}";

        public SpiderJavfree(SpiderViewModel browser) : base(browser)
        {
            Name = "Javfree";
            URL = "https://javfree.me/";
            ScriptName = "Javfree.js";
        }
    }
}
