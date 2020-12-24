using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel;

using log4net;
using log4net.Config;

using Unosquare.FFME;

using CefSharp;
using CefSharp.Wpf;

using HappyHour.Model;

namespace HappyHour
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string CurrentPath { get; set; }
        public static string DataPath { get; set; }
        public static string JavPath { get; set; }
        public static string LocalAppData { get; set; }
        public static AvDbContext DbContext { get; set; }

        /// <summary>
        /// Determines if the Application is in design mode.
        /// </summary>
        public static bool IsInDesignMode => !(Current is App) ||
            (bool)DesignerProperties.IsInDesignModeProperty
                .GetMetadata(typeof(DependencyObject)).DefaultValue;
        public App()
        {
            // Load configuration
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

            // Change the default location of the ffmpeg binaries 
            // (same directory as application)
            Library.FFmpegDirectory = @"ffmpeg" +
                (Environment.Is64BitProcess ? @"\x64" : string.Empty);

            // Multi-threaded video enables the creation of independent
            // dispatcher threads to render video frames. This is an experimental
            // feature and might become deprecated in the future as no real
            // performance enhancements have been detected.
            //Library.EnableWpfMultiThreadedVideo = !Debugger.IsAttached;
            // test with true and false
        }

        void InitCefSharp()
        {
            var settings = new CefSettings
            {
                MultiThreadedMessageLoop = true,
                ExternalMessagePump = false,
                RemoteDebuggingPort = 8088,

                //Enables Uncaught exception handler
                UncaughtExceptionStackSize = 10
            };

            // Off Screen rendering (WPF/Offscreen)
            if (settings.WindowlessRenderingEnabled)
            {
                //Disable Direct Composition to test 
                // https://github.com/cefsharp/CefSharp/issues/1634
                //settings.CefCommandLineArgs.Add("disable-direct-composition");

                // DevTools doesn't seem to be working when this is enabled
                // http://magpcss.org/ceforum/viewtopic.php?f=6&t=14095
                settings.CefCommandLineArgs.Add("enable-begin-frame-scheduling");
                //settings.CefCommandLineArgs.Add("disable-image-loading");

            }
            settings.CefCommandLineArgs.Add("enable-experimental-web-platform-features");

            //The location where cache data will be stored on disk. If empty an in-memory cache will be used for some features and a temporary disk cache for others.
            //HTML5 databases such as localStorage will only persist across sessions if a cache path is specified. 
            //settings.CachePath = LocalAppData + "CEF\\cache";
            //settings.CachePath = LocalAppData + @"Google\Chrome\User Data\Default";

            //This must be set before Cef.Initialized is called
            CefSharpSettings.FocusedNodeChangedEnabled = true;

            //Async Javascript Binding - methods are queued on TaskScheduler.Default.
            //Set this to true to when you have methods that return Task<T>
            //CefSharpSettings.ConcurrentTaskExecution = true;

            //Legacy Binding Behaviour - Same as Javascript Binding in version 57 and below
            //See issue https://github.com/cefsharp/CefSharp/issues/1203 for details
            //CefSharpSettings.LegacyJavascriptBindingEnabled = true;

            //Exit the subprocess if the parent process happens to close
            //This is optional at the moment
            //https://github.com/cefsharp/CefSharp/pull/2375/
            CefSharpSettings.SubprocessExitIfParentProcessClosed = true;

            if (!Cef.Initialize(settings, performDependencyCheck: false))
            {
                throw new Exception("Unable to Initialize Cef");
            }
        }

        async void PreLoadFFmpeg()
        {
            // Pre-load FFmpeg libraries in the background. This is optional.
            // FFmpeg will be automatically loaded if not already loaded
            // when you try to open a new stream or file. See issue #242
            try
            {
                // Pre-load FFmpeg
                await Library.LoadFFmpegAsync();
            }
            catch (Exception ex)
            {
                var dispatcher = Current?.Dispatcher;
                if (dispatcher == null)
                    return;

                await dispatcher.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show(MainWindow,
                        $"Unable to Load FFmpeg Libraries from path:\r\n    {Library.FFmpegDirectory}" +
                        $"\r\nMake sure the above folder contains FFmpeg shared binaries (dll files) for the " +
                        $"applicantion's architecture ({(Environment.Is64BitProcess ? "64-bit" : "32-bit")})" +
                        $"\r\nTIP: You can download builds from https://ffmpeg.zeranoe.com/builds/" +
                        $"\r\n{ex.GetType().Name}: {ex.Message}\r\n\r\nApplication will exit.",
                        "FFmpeg Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    Current?.Shutdown();
                }));
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            SetupExceptionHandling();

            InitCefSharp();
            Task.Run(() => PreLoadFFmpeg());
        }

        private void SetupExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                LogUnhandledException((Exception)e.ExceptionObject,
                    "AppDomain.CurrentDomain.UnhandledException");

            DispatcherUnhandledException += (s, e) =>
            {
                LogUnhandledException(e.Exception,
                    "Application.Current.DispatcherUnhandledException");
                e.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                LogUnhandledException(e.Exception,
                    "TaskScheduler.UnobservedTaskException");
                e.SetObserved();
            };
        }

        private void LogUnhandledException(Exception exception, string source)
        {
            string message = $"Unhandled exception ({source})";
            try
            {
                AssemblyName assemblyName
                    = Assembly.GetExecutingAssembly().GetName();
                message = string.Format("Unhandled exception in {0} v{1}",
                    assemblyName.Name, assemblyName.Version);
            }
            catch (Exception ex)
            {
                //_logger.Error(ex, "Exception in LogUnhandledException");
                Log.Print("Exception in LogUnhandledException", ex);
            }
            finally
            {
                //_logger.Error(exception, message);
                Log.Print(message, exception);
                if (App.Current != null)
                    App.Current.Shutdown(1);
            }
        }
    }
}
