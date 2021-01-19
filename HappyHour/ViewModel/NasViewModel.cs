using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using CefSharp;

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
            Address = _nasUrl;
        }

        void OnFrameLoaded(object sender, FrameLoadEndEventArgs e)
        {
            if (Address != _nasUrl) return;
#if false
            Log.Print($"Num loading :{++_numLoading}, isMain {e.Frame.IsMain}");
            if (e.Frame.IsMain)
            { 
                WebBrowser.ExecuteScriptAsync(App.ReadResource("NasLogin.js"));
            }
#endif
            if (_numLoading == 4)
                WebBrowser.ExecuteScriptAsync(App.ReadResource("NasLogin.js"));
        }
    }
}
