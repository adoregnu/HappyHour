using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

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

        public async void Download()
        {
            try
            {
                foreach (var magnet in _torrent.MagnetUrls)
                {
                    var client = new QBittorrentClient(new Uri("http://192.168.50.26:8080"));
                    var magnetUri = new Uri(magnet.MagnetUrl);
                    var addRequest = new AddTorrentUrlsRequest(magnetUri) { Paused = false };
                    await client.AddTorrentsAsync(addRequest);
                    client.Dispose();
                }
                _torrent.StatusCode = 'D';
            }
            catch (Exception ex)
            {
                Log.Print(ex.Message);
            }
        }

        public void Exclude()
        {
            _torrent.StatusCode = 'E';
            Log.Print($"Mark excluded {Pid}");
        }

        public async override Task ReloadAsync()
        {
            //await Task.Run(() => Reload(files));
            using var db = new TorrentDbContext();
            _torrent = await db.GetTorrent( Pid );

            Poster = _torrent.CoverPath;
            BriefInfo = $"{Pid}\n{Date}";
        }
    }
}
