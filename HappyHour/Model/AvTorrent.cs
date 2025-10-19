using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using HappyHour.CefHandler;
using QBittorrent.Client;

namespace HappyHour.Model
{
    internal class AvTorrent : AvMediaBase
    {
        private  Torrent _torrent;

        public List<string> Screenshots => _torrent.Screenshots.Select(s => s.Value).ToList();

        public AvTorrent(Torrent torrent)
        {
            _torrent = torrent;
            Pid = torrent.PID;
            Date = torrent.Date;

            Poster = _torrent.CoverPath;
            BriefInfo = $"{Pid}\n{Date}";
        }

        private void UpdateDbStatus(char status)
        {
            using var db = new TorrentDbContext();
            db.Attach(_torrent);
            _torrent.StatusCode = status;
            db.SaveChanges();

            if (File.Exists(_torrent.CoverPath))
            {
                File.Delete(_torrent.CoverPath);
            }
            foreach (var s in _torrent.Screenshots)
            {
                if (File.Exists(s.Value)) File.Delete(s.Value);
            }
        }

        public async void Download()
        {
            var magnets = _torrent.MagnetUrls.Where(m => m.SourceUrl.Contains("sehuatang"));
            if (!magnets.Any())
            {
                magnets = _torrent.MagnetUrls;
            }

            try
            {
                foreach (var magnet in magnets)
                {
                    var client = new QBittorrentClient(new Uri("http://192.168.50.26:8080"));
                    var magnetUri = new Uri(magnet.MagnetUrl);
                    var addRequest = new AddTorrentUrlsRequest(magnetUri) { Paused = false };
                    await client.AddTorrentsAsync(addRequest);
                    client.Dispose();
                }
                UpdateDbStatus('D');
            }
            catch (Exception ex)
            {
                Log.Print(ex.Message);
            }
        }

        public void Exclude()
        {
            UpdateDbStatus('E');
            Log.Print($"Mark excluded {Pid}");
        }

        public async Task UpdateDb(IDictionary<string, object> items)
        {
            items.TryGetValue("cover", out object cover);
            items.TryGetValue("screenshot", out object screenshot);
            if (cover == null && screenshot == null) return;

            using var db = new TorrentDbContext();
            db.Attach(_torrent);

            if (cover != null)
            {
                _torrent.CoverPath = cover.ToString();
            }
            if (screenshot != null)
            {
                if (screenshot is List<object> ms)
                {
                    ms.ForEach(s => _torrent.Screenshots.Add(new StringData() { Value = s.ToString() }) );
                }
                else
                {
                    _torrent.Screenshots.Add(new StringData() { Value = screenshot.ToString() });
                }
            }
            db.SaveChanges();
            await ReloadAsync();
        }

        public async override Task ReloadAsync()
        {
            //await Task.Run(() => Reload(files));
            using var db = new TorrentDbContext();
            _torrent = await db.GetTorrent( Pid );

            Poster = _torrent.CoverPath;
            BriefInfo = $"{Pid}\n{Date}";
            OnPropertyChanged(nameof(Screenshots));
        }
    }
}
