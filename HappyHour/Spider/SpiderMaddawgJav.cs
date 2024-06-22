using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    class SpiderMaddawgJav: SpiderBase
    {
        public override string SearchURL => $"{URL}?s={Keyword}";
        public SpiderMaddawgJav(SpiderViewModel browser) : base(browser)
        {
            Name = "Maddawg JAV";
            URL = "http://maddawgjav.net/";
            ScriptName = "MaddawgJav.js";
        }
    }
}
