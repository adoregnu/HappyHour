using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using CefSharp;
using GalaSoft.MvvmLight.Command;
using HappyHour.Spider;

namespace HappyHour.ViewModel
{
    class GoogleEarthViewModel : BrowserBase
    {
        bool _isFrameLoaded = false;
        public ICommand CmdOpenNmea { get; private set; }

        public GoogleEarthViewModel() : base()
        {
            HeaderType = "googleEarth";
            CmdOpenNmea = new RelayCommand(() =>
            {
                if (_isFrameLoaded)
                {
                    ExecJavaScript(SpiderBase.XPathClickSingle("//*[@id='projects']"));
                }
            });
        }

        protected override void InitBrowser()
        {
            base.InitBrowser();
            Address = "https://earth.google.com/web/";
            WebBrowser.FrameLoadEnd += OnFrameLoaded;
        }

        void OnFrameLoaded(object sender, FrameLoadEndEventArgs e)
        {
            Log.Print($"IsMain : {e.Frame.IsMain}");
            if (e.Frame.IsMain)
            {
                _isFrameLoaded = true;
            }
        }
    }
}
