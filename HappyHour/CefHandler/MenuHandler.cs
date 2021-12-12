using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CefSharp;
using CefSharp.Wpf;

using GalaSoft.MvvmLight.Command;

using HappyHour.ViewModel;
using Scriban;

namespace HappyHour.CefHandler
{
    internal class MenuHandler : IContextMenuHandler
    {
        private const int CefUserCommand = 26503;
        private readonly SpiderViewModel _browser;
        private string _imageUrl;

        public MenuHandler(SpiderViewModel spider = null)
        {
            _browser = spider;
        }

        void IContextMenuHandler.OnBeforeContextMenu(
            IWebBrowser chromiumWebBrowser,
            IBrowser browser,
            IFrame frame,
            IContextMenuParams parameters,
            IMenuModel model)
        {
            //Log.Print("Context menu opened");
            //Log.Print(parameters.MisspelledWord);

            if (model.Count > 0)
            {
                _ = model.AddSeparator();
            }

            _ = model.AddItem((CefMenuCommand)26501, "Show DevTools");
            _ = model.AddItem((CefMenuCommand)26502, "Close DevTools");

            int cefCmdId = CefUserCommand;

            if (parameters.MediaType == ContextMenuMediaType.Image)
            {
                _ = model.AddItem((CefMenuCommand)cefCmdId++, "Edit Image");
                _imageUrl = parameters.SourceUrl;
            }

            if (string.IsNullOrEmpty(parameters.SelectionText))
            {
                return;
            }

            _ = model.AddSeparator();

            _ = model.AddItem((CefMenuCommand)cefCmdId++, "Google");
            _ = model.AddItem((CefMenuCommand)cefCmdId++, "Google Translate");
            _ = model.AddItem((CefMenuCommand)cefCmdId++, "Search PID");

            if (_browser != null)
            {
                _ = model.AddSeparator();
                foreach (var spider in _browser.Spiders)
                {
                    _ = model.AddItem((CefMenuCommand)cefCmdId++, spider.Name);
                }
            }
        }

        bool IContextMenuHandler.OnContextMenuCommand(
            IWebBrowser chromiumWebBrowser,
            IBrowser browser,
            IFrame frame,
            IContextMenuParams parameters,
            CefMenuCommand commandId,
            CefEventFlags eventFlags)
        {
            if (commandId == (CefMenuCommand)26501)
            {
                browser.GetHost().ShowDevTools();
                return true;
            }
            if (commandId == (CefMenuCommand)26502)
            {
                browser.GetHost().CloseDevTools();
                return true;
            }

            return false;
        }
        void IContextMenuHandler.OnContextMenuDismissed(
            IWebBrowser chromiumWebBrowser,
            IBrowser browser,
            IFrame frame)
        {
            var webBrowser = (ChromiumWebBrowser)chromiumWebBrowser;

            webBrowser.Dispatcher.Invoke(() =>
            {
                webBrowser.ContextMenu = null;
            });
        }

        bool IContextMenuHandler.RunContextMenu(
            IWebBrowser chromiumWebBrowser,
            IBrowser browser,
            IFrame frame,
            IContextMenuParams parameters,
            IMenuModel model,
            IRunContextMenuCallback callback)
        {
            //NOTE: Return false to use the built in Context menu - 
            // in WPF this requires you integrate into your existing message loop,
            // read the General Usage Guide for more details
            //https://github.com/cefsharp/CefSharp/wiki/General-Usage#multithreadedmessageloop
            //return false;

            var webBrowser = (ChromiumWebBrowser)chromiumWebBrowser;

            //IMenuModel is only valid in the context of this method,
            // so need to read the values before invoking on the UI thread
            var menuItems = GetMenuItems(model).ToList();
            webBrowser.Dispatcher.Invoke(() =>
            {
                var menu = new ContextMenu
                {
                    IsOpen = true
                };

                void handler(object s, RoutedEventArgs e)
                {
                    menu.Closed -= handler;

                    //If the callback has been disposed then it's already been executed
                    //so don't call Cancel
                    if (!callback.IsDisposed)
                    {
                        callback.Cancel();
                    }
                }

                menu.Closed += handler;

                foreach (var item in menuItems)
                {
                    if (item.Item2 == CefMenuCommand.NotFound &&
                        string.IsNullOrWhiteSpace(item.Item1))
                    {
                        _ = menu.Items.Add(new Separator());
                        continue;
                    }

                    _ = menu.Items.Add(new MenuItem
                    {
                        Header = item.Item1.Replace("&", "_"),
                        IsEnabled = item.Item3,
                        Command = new RelayCommand(() =>
                            ProcessMenu(item, browser, parameters), keepTargetAlive: true)
                    });
                }
                webBrowser.ContextMenu = menu;
            });

            return true;
        }

        private static string GetScript(string action, string spider = "")
        {
            var template = Template.Parse(App.ReadResource("SearchText.js"));
            return template.Render(new { Action = action, Spider = spider });
        }

        private void ProcessMenu(Tuple<string, CefMenuCommand, bool> item,
            IBrowser browser, IContextMenuParams parameters)
        {
            //BUG: CEF currently not executing callbacks correctly 
            // so we manually map the commands below
            //see https://github.com/cefsharp/CefSharp/issues/1767
            //The following line worked in previous versions, 
            // it doesn't now, so custom EXAMPLE below
            //callback.Continue(item.Item2, CefEventFlags.None);

            //NOTE: Note all menu item options below have been tested, 
            // you can work out the rest
            switch (item.Item2)
            {
#if false
                case CefMenuCommand.Back:
                    browser.GoBack();
                    break;
                case CefMenuCommand.Forward:
                    browser.GoForward();
                    break;
                case CefMenuCommand.Cut:
                    browser.FocusedFrame.Cut();
                    break;
                case CefMenuCommand.Copy:
                    browser.FocusedFrame.Copy();
                    break;
                case CefMenuCommand.Paste:
                    browser.FocusedFrame.Paste();
                    break;
                case CefMenuCommand.Print:
                    browser.GetHost().Print();
                    break;
                case CefMenuCommand.ViewSource:
                    browser.FocusedFrame.ViewSource();
                    break;
                case CefMenuCommand.Undo:
                    browser.FocusedFrame.Undo();
                    break;
                case CefMenuCommand.StopLoad:
                    browser.StopLoad();
                    break;
                case CefMenuCommand.SelectAll:
                    browser.FocusedFrame.SelectAll();
                    break;
                case CefMenuCommand.Redo:
                    browser.FocusedFrame.Redo();
                    break;
                case CefMenuCommand.Find:
                    browser.GetHost().Find(0, parameters.SelectionText, true, false, false);
                    break;
                case CefMenuCommand.AddToDictionary:
                    browser.GetHost().AddWordToDictionary(parameters.MisspelledWord);
                    break;
                case CefMenuCommand.Reload:
                    browser.Reload();
                    break;
                case CefMenuCommand.ReloadNoCache:
                    browser.Reload(ignoreCache: true);
                    break;
#endif
                case (CefMenuCommand)26501:
                    browser.GetHost().ShowDevTools();
                    break;
                case (CefMenuCommand)26502:
                    browser.GetHost().CloseDevTools();
                    break;
                case (CefMenuCommand)CefUserCommand:
                    Log.Print(_imageUrl);
                    break;
                case (CefMenuCommand)(CefUserCommand + 1):
                    _browser.ExecJavaScript(GetScript("google_search"));
                    break;
                case (CefMenuCommand)(CefUserCommand + 2):
                    _browser.ExecJavaScript(GetScript("google_translate"));
                    break;
                case (CefMenuCommand)(CefUserCommand + 3):
                    _browser.ExecJavaScript(GetScript("pid_search_in_db"));
                    break;
                default:
                    _browser.ExecJavaScript(GetScript("pid_search_in_spider", item.Item1));
                    break;
            }
        }

        private static IEnumerable<Tuple<string, CefMenuCommand, bool>>
            GetMenuItems(IMenuModel model)
        {
            for (int  i = 0; i < model.Count; i++)
            {
                string header = model.GetLabelAt(i);
                var commandId = model.GetCommandIdAt(i);
                bool isEnabled = model.IsEnabledAt(i);
                yield return new Tuple<string, CefMenuCommand, bool>(
                    header, commandId, isEnabled);
            }
        }
    }
}
