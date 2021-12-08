using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    class SpiderAvsox : SpiderBase
    {
        public override string SearchURL => $"{URL}search/{Keyword}";

        public SpiderAvsox(SpiderViewModel browser) : base(browser)
        {
            Name = "AVSOX";
            //URL = "https://avsox.website/en/";
            URL = "https://avsox.monster/en/";
            ScriptName = "Avsox.js";
        }
    }
}
