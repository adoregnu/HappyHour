﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using HtmlAgilityPack;

using HappyHour.ViewModel;
using HappyHour.ScrapItems;

namespace HappyHour.Spider
{
    class SpiderAvsox : SpiderBase
    {
        public override string SearchURL => $"{URL}search/{Keyword}";

        public SpiderAvsox(SpiderViewModel browser) : base(browser)
        {
            Name = "AVSOX";
            URL = "https://avsox.website/en/";
            ScriptName = "Avsox.js";
        }
    }
}
