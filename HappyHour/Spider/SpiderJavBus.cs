using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    class SpiderJavBus : SpiderBase
    {
        public List<string> SearchTypes { get; set; } = new List<string>
        {
            "censored", "uncensored"
        };
        public string SearchType { get; set; } = "censored";

        public override string SearchURL
        {
            get
            {
                if (SearchType == "censored")
                    return $"{URL}search/{Keyword}";
                else
                    return $"{URL}uncensored/search/{Keyword}";
            }
        }

        public SpiderJavBus(SpiderViewModel browser) : base(browser)
        {
            Name = "JavBus";
            URL = "https://www.javbus.com/en/";
            ScriptName = "JavBus.js";
        }
    }
}
