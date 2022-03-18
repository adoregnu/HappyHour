using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    class SpiderMmd : SpiderBase
    {
        public override string SearchURL => $"{URL}search?q={Keyword}";
        public SpiderMmd(SpiderViewModel browser) : base(browser)
        {
            Name = "MMD";
            URL = "https://www.micmicdoll.com/";
            ScriptName = "MMD.js";
        }
    }
}
