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
