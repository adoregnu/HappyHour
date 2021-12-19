using System.Collections.Generic;

using CefSharp;
using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    internal class Spider1pondo : SpiderBase
    {
        public override string SearchURL => $"{URL}movies/{Keyword}/";
        public Spider1pondo(SpiderViewModel browser) : base(browser)
        {
            Name = "1pondo";
            //URL = "https://www.1pondo.tv/";
            URL = "https://en.1pondo.tv/";
            ScriptName = "1pondo.js";
        }
#if false
        protected override List<Cookie> CreateCookie()
        {
            return new List<Cookie> {
                new Cookie
                {
                    Name = "adc",
                    Value = "1",
                    Domain = "mgstage.com",
                    Path = "/"
                }
            };
        }
#endif
    }
}
