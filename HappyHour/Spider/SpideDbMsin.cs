using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    class SpideDbMsin: SpiderBase
    {
        public override string SearchURL
        {
            get
            {
                if (Keyword.StartsWith("FC2", System.StringComparison.OrdinalIgnoreCase))
                {
                    return $"{URL}/search/movie?str={Keyword}";
                }
                else
                {
                    return $"{URL}/jp.search/movie?str={Keyword}";
                }
            }
        } 

        public SpideDbMsin(SpiderViewModel browser) : base(browser)
        {
            Name = "db.msin";
            URL = "https://db.msin.jp";
            ScriptName = "db.msin.js";
        }
    }
}
