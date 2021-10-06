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

using IniParser;
using IniParser.Model;

using HappyHour.Model;
using System.Text;

namespace HappyHour
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string LocalAppData { get; set; }
        public static AvDbContext DbContext { get; set; }
        public static IniData GConf { get; private set; }
        public const string Name = "HappyHour";

        /// <summary>
        /// Determines if the Application is in design mode.
        /// </summary>
        public static bool IsInDesignMode => !(Current is App) ||
            (bool)DesignerProperties.IsInDesignModeProperty
                .GetMetadata(typeof(DependencyObject)).DefaultValue;

        static App()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public static string ReadResource(string name)
        {
            // Determine path
            var assembly = Assembly.GetExecutingAssembly();
            string resourcePath = name;
            // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
            resourcePath = assembly.GetManifestResourceNames()
                .Single(str => str.EndsWith(name));
            if (string.IsNullOrEmpty(resourcePath))
            {
                return null;
            }

            using Stream stream = assembly.GetManifestResourceStream(resourcePath);
            using StreamReader reader = new (stream);
            return reader.ReadToEnd();
        }

        public App()
        {
            // Load configuration
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
            LocalAppData = Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%");
            LocalAppData += $"\\{Name}";
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

        FileIniDataParser _iniParser;
        void InitDefaultConf()
        {
            _iniParser = new FileIniDataParser();
            if (File.Exists(@$"{LocalAppData}\gconf.ini"))
            {
                GConf = _iniParser.ReadFile(@$"{LocalAppData}\gconf.ini");
            }
            else
            {
                GConf = new IniData();
            }
            if (!GConf.Sections.ContainsSection("general"))
            {
                GConf.Sections.AddSection("general");
                var general = GConf["general"];
                general.AddKey("torrent_path", @"z:\");
                general.AddKey("data_path", @"d:\tmp\");
                general.AddKey("last_path", @"d:\tmp\");
                general.AddKey("nas_url", "https://bsyoo.me:5001/");
            }
        }

        static void InitCefSharp()
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
            settings.CefCommandLineArgs.Add("ignore-certificate-errors");
            //settings.CefCommandLineArgs.Add("mute-audio", "true");
            //The location where cache data will be stored on disk. If empty an in-memory cache will be used for some features and a temporary disk cache for others.
            //HTML5 databases such as localStorage will only persist across sessions if a cache path is specified. 
            settings.CachePath = LocalAppData + "\\CEF\\cache";

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
        protected override void OnExit(ExitEventArgs e)
        {
            _iniParser.WriteFile($@"{LocalAppData}\gconf.ini", GConf);
            base.OnExit(e);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            SetupExceptionHandling();

            InitDefaultConf();
            InitCefSharp();

            try
            {
                DbContext = new AvDbContext();
            }
            catch (Exception ex)
            {
                Log.Print(ex.Message);
            }

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

        static void LogUnhandledException(Exception exception, string source)
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
