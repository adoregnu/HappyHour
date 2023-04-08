using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Input;

using CefSharp;
using HappyHour.Interfaces;
using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    internal class SpiderSukebei : SpiderBase
    {
        private readonly string _dataPath;
        private int _numDuplicatedPid;

        public override string SearchURL => $"{URL}{Keyword}";

        public int NumPage { get; set; } = 1;
        public string PidToStop { get; set; }
        public bool StopOnExistingId { get; set; }
        public ICommand CmdStop { get; private set; }

        public SpiderSukebei(SpiderViewModel browser) : base(browser)
        {
            Name = "Sukebei";
            URL = "https://sukebei.nyaa.si/?f=2&c=2_2&q=";
            ScriptName = "Sukebei.js";

            _dataPath = App.Current.GetConf("general", "data_path") ?? @"d:\tmp\sehuatang";
            _dataPath += "\\censored";
        }
        public override void Navigate2(IAvMedia _)
        {
            _numDuplicatedPid = 0;
            IsSpiderWorking = true;
            if (Browser.Address == URL)
            {
                Browser.Address = "";
            }
            Browser.Address = URL;
        }

        private void SaveMagenetLink(dynamic items)
        {
            var torrents = items.torrents as IList<object>;
            Match m;
            foreach (dynamic item in torrents)
            {
                m = Regex.Match(item.title.ToString(), @"([0-9a-z]+-[0-9]+)", RegexOptions.IgnoreCase);
                if (!m.Success) { continue; }

                string pid = m.Groups[1].Value;
                if (!string.IsNullOrEmpty(PidToStop) &&
                    pid.Equals(PidToStop, StringComparison.OrdinalIgnoreCase))
                {
                    OnScrapCompleted(false);
                    return;
                }

                string outPath = $"{_dataPath}\\{pid}";
                string sukebeiPath = outPath + "\\sukebei";
                if (!new DirectoryInfo(sukebeiPath).Exists)
                {
                    _ = Directory.CreateDirectory(sukebeiPath);
                }
                string fileName = $"{sukebeiPath}\\{pid}.magnet";
                if (File.Exists(fileName))
                {
                    _numDuplicatedPid++;
                }
                if (_numDuplicatedPid > 3 && StopOnExistingId)
                {
                    OnScrapCompleted(false);
                    return;
                }
                File.WriteAllText(fileName, item.magnet.ToString());
                Browser.MediaList.AddMedia(outPath);
            }

            string nexPageLink = items.nextPage.ToString();
            m = Regex.Match(nexPageLink, @"p=(\d+)");
            if (m.Success && int.TryParse(m.Groups[1].Value, out int nextPage) &&
                nextPage <= NumPage)
            {
                Browser.MainView.StatusMessage =
                    $"Page:{nextPage}/{NumPage}";
                Browser.Address = nexPageLink;
            }
            else
            {
                OnScrapCompleted(false);
            }
        }

        public override bool OnJsMessageReceived(JavascriptMessageReceivedEventArgs msg)
        {
            dynamic d = msg.Message;
            Log.Print($"{d.type} : {d.data}");

            if (d.type == "items" && d.data == 0)
            {
                OnScrapCompleted(false);
            }
            else
            {
                SaveMagenetLink(d);
            }
            return true;
        }
    }
}
