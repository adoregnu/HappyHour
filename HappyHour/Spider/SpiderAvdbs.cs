using System.Collections.Generic;
using System.Drawing;

using HappyHour.ViewModel;
using Scriban;

namespace HappyHour.Spider
{
    internal class SpiderAvdbs : SpiderBase
    {
        public override string SearchURL =>
            $"{URL}menu/search.php?kwd={Keyword}&tab=2";

        public SpiderAvdbs(SpiderViewModel browser) : base(browser)
        {
            Name = "Avdbs";
            URL = "https://www.avdbs.com/";
            ScriptName = "Avdbs.js";

            ScrapItems.ForEach(i =>
            {
                if (i.Name is not "actor")
                {
                    i.CanUpdate = false;
                }
            });
        }

        int _numScrap = 0;
        public override void OnSelected()
        {
            base.OnSelected();
            _numScrap = 0;
        }

        private void Search()
        {
            var template = Template.Parse(App.ReadResource("Avdbs_search.js"));
            string js = template.Render(new { Pid = Keyword, });
            Browser.ExecJavaScript(js);
        }

        public override void SetAddress()
        {
            if (_numScrap > 0)
            {
                Search();
            }
            else
            {
                Browser.Address = URL;
            }
        }

        public override void Scrap()
        {
            if (_numScrap > 0)
            {
                base.Scrap();
            }
            else
            {
                Search();
            }
            _numScrap++;
        }

        private void CropImage(string fname)
        {
            Log.Print($"{Name}: CropImage: {fname}");
            string realPath = @$"{App.LocalAppData}\db\{fname}";

            var org = new Bitmap(realPath);
            if (org.Height <= org.Width) { return; }

            //Log.Print($"width: {org.Width}, height:{org.Height}");
            var cropArea = new Rectangle(0, 0, org.Width, org.Width);
            var crop = org.Clone(cropArea, org.PixelFormat);
            org.Dispose();

            //Log.Print($"width:{crop.Width}, height:{crop.Height}");
            crop.Save(realPath);
        }

        protected override void UpdateDb(IDictionary<string, object> items)
        {
            _ = IterateDynamic(items, (key, dict) =>
            {
                if (key is "thumb")
                {
                    CropImage(dict[key].ToString());
                    return true;
                }
                return false;
            });
            base.UpdateDb(items);
        }
    }
}
