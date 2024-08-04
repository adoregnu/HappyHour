using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
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

            //SpiderNamesToChain = ["Javlibrary"];
            OverwriteActorThumb = true;
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
            using var ms = new MemoryStream(File.ReadAllBytes(fname));
            using var org = new Bitmap(ms);
            File.Delete(fname);

            if (org.Height <= org.Width) { return; }

            //Log.Print($"width: {org.Width}, height:{org.Height}");
            var cropArea = new Rectangle(0, 0, org.Width, org.Width);
            using var crop = org.Clone(cropArea, org.PixelFormat);
            crop.Save(fname);
        }

        protected override void AdjustKeyword()
        {
            string[] ends = { "V", "E" };
            if (ends.Any(e => Keyword.EndsWith(e)))
            {
                Keyword = Keyword[..^1];
            }
        }

        protected async override Task UpdateDb(IDictionary<string, object> items)
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
            await base.UpdateDb(items);
        }
    }
}
