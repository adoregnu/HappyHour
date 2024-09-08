using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Input;

using CefSharp;
using HappyHour.Interfaces;
using HappyHour.ViewModel;
using System.Threading.Tasks;
using HappyHour.Model;
using System.Linq;

namespace HappyHour.Spider
{
    internal class SpiderSukebei : SpiderBase
    {
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

            ChainVisibility = System.Windows.Visibility.Collapsed;
        }

        public override void Navigate2(IAvMedia media, bool resetChain)
        {
            _numDuplicatedPid = 0;
            IsSpiderWorking = true;
            if (Browser.Address == URL)
            {
                Browser.Address = "";
            }
            Browser.Address = URL;
        }

        private async void SaveMagenetLink(dynamic items, TorrentDbContext _db)
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
                    await OnScrapCompleted(false);
                    return;
                }

                var torrent = _db.GetTorrent(pid);
                bool bexist = false;
                if (torrent.MagnetUrls.Any(m => m.SourceUrl == items.source))
                {
                    _numDuplicatedPid++;
                    Log.Print($"{pid} is already exists");
                    bexist = true;
                }
                if (StopOnExistingId && _numDuplicatedPid > 3)
                {
                    await OnScrapCompleted(false);
                    return;
                }
                if (!bexist)
                {
                    torrent.MagnetUrls.Add(new Magnet()
                    {
                        MagnetUrl = item.magnet,
                        SourceUrl = items.source,
                    });
                    if (string.IsNullOrEmpty(torrent.CoverPath))
                    {
                        if (long.TryParse(item.date, out long ts))
                        {
                            torrent.Date = DateTimeOffset.FromUnixTimeSeconds(ts).LocalDateTime;
                        }
                    }
                    if (torrent.StatusCode == 'N')
                    {
                        Browser.MediaList.AddMedia(torrent);
                    }
                    else
                    {
                        Log.Print($"{pid} already crawled");
                    }
                }
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
                await OnScrapCompleted(false);
            }
        }

        public async override ValueTask<bool> OnJsMessageReceived(JavascriptMessageReceivedEventArgs msg)
        {
            dynamic d = msg.Message;
            Log.Print($"{d.type} : {d.data}");

            if (d.type == "items" && d.data == 0)
            {
                await OnScrapCompleted(false);
            }
            else
            {
                using TorrentDbContext _db = new();
                SaveMagenetLink(d, _db);
                _db.SaveChanges();
            }
            return true;
        }
    }
}
