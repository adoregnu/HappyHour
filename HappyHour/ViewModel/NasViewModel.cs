using CefSharp;
using Scriban;

namespace HappyHour.ViewModel
{
    class NasViewModel : BrowserBase
    {
        int _numLoading = 0;
        readonly string _nasUrl;

        public NasViewModel() : base()
        {
            _nasUrl = App.Current.GConf["general"]["nas_url"];
        }

        protected override void InitBrowser()
        {
            base.InitBrowser();

            WebBrowser.FrameLoadEnd += OnFrameLoaded;
            Address = _nasUrl;
        }

        void OnFrameLoaded(object sender, FrameLoadEndEventArgs e)
        {
            if (Address != _nasUrl) return;

            _numLoading++;
            //if (_numLoading == 4) Login("NasLogin.js");
        }
    }
}
