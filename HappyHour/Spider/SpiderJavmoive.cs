using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    class SpiderJavmoive : SpiderBase
    {
        public override string SearchURL => $"{URL}search.html?k={Keyword}";
        public SpiderJavmoive(SpiderViewModel browser) : base(browser)
        {
            Name = "JavMovie";
            URL = "http://javmovie.com/en/";
            ScriptName = "JavMovie.js";
        }
    }
}
