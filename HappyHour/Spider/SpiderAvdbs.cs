using System.Collections.Generic;
using System.Drawing;

using HappyHour.ViewModel;

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
