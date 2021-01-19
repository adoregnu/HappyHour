using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using CefSharp;

using HappyHour.CefHandler;

namespace HappyHour.ViewModel
{
    class NasViewModel : BrowserBase
    {
        int _numLoading = 0;
        readonly string _nasUrl;

        public NasViewModel() : base()
        {
            _nasUrl = App.GConf["general"]["nas_url"];
        }

        protected override void InitBrowser()
        {
            base.InitBrowser();

            WebBrowser.FrameLoadEnd += OnFrameLoaded;
            WebBrowser.MenuHandler = new MenuHandler();
            Address = _nasUrl;
        }

        void OnFrameLoaded(object sender, FrameLoadEndEventArgs e)
        {
            if (Address != _nasUrl) return;

            Log.Print($"Num loading :{++_numLoading}, isMain {e.Frame.IsMain}");
            if (e.Frame.IsMain)
            { 
                //WebBrowser.ExecuteScriptAsync(App.ReadResource("NasLogin.js"));
            }
            if (_numLoading == 4)
                WebBrowser.ExecuteScriptAsync(App.ReadResource("NasLogin.js"));
        }
    }
}
