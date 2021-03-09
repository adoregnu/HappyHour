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
        readonly Dictionary<string, string> _typeDic
            = new Dictionary<string, string>
            {
                {  "censored", "ce" }, { "uncensored", "uc"}
            };

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
        }

        void OnMultiResult(object result)
        {
            if (!CheckResult(result, out List<string> list))
                goto NotFound;

            ParsingState = 1;
            if (list.Count > 1)
            {
                Log.Print("Multitple item matched, Select manually.");
            }
            else
            {
                Browser.Address = list[0];
            }
            return;
        NotFound:
            OnScrapCompleted();
        }

        public override void Scrap()
        {
            switch (ParsingState)
            {
                case 0:
                    Browser.ExecJavaScript(
                        XPath("//a[@class='movie-box']/@href"),
                        OnMultiResult);
                    break;
                case 1:
                    OnScrapCompleted();
                    break;
            }
        }
    }
}
