﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

using QBittorrent.Client;

namespace HappyHour.Model
{
    internal class AvTorrent : AvMediaBase
    {
        public List<string> Screenshots { get; set; } = new();
        public List<string> Torrents { get; set; } = new();
        public Visibility MagnetVisibility { get; set; } = Visibility.Collapsed;
        public Visibility TorrentVisibility { get; set; } = Visibility.Collapsed;

        public AvTorrent(string path)
        {
            Path = path;
            Pid = path.Split('\\').Last();
        }

        public async void Download(string ext)
        {
            try
            {
                bool downloaded = false;
                foreach (string file in Torrents)
                {
                    if (ext is "torrent" && file.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                    {
                        string torrent = System.IO.Path.GetFileName(file);
                        File.Copy(file, App.GConf["general"]["torrent_path"] + torrent);
                        downloaded = true;
                    }
                    else if (ext is "magnet" && file.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                    {
                        var client = new QBittorrentClient(new Uri("http://localhost:8080"));
                        var magnets = new Uri(File.ReadAllText(file));
                        var addRequest = new AddTorrentUrlsRequest(magnets) { Paused = true };
                        await client.AddTorrentsAsync(addRequest);
                        client.Dispose();
                        downloaded = true;
                    }
                }
                if (downloaded)
                {
                    File.Create($"{Path}\\.downloaded").Dispose();
                    Log.Print($"Mark downloaded {Path}");
                }
            }
            catch (Exception ex)
            {
                Log.Print(ex.Message);
            }
        }

        public void Exclude()
        {
            try
            {
                File.Create($"{Path}\\.excluded").Dispose();
                Log.Print($"Mark excluded {Path}");
            }
            catch (Exception ex)
            {
                Log.Print("Exclude: ", ex.Message);
            }
        }

        public override void Reload(string[] files = null)
        {
            if (files == null)
            {
                //files = Directory.GetFiles(Path);
                files = Directory.GetFiles(Path, "*", new EnumerationOptions { RecurseSubdirectories = true });
            }
            Torrents.Clear();
            Screenshots.Clear();
            foreach (string file in files)
            {
                if (file.Contains("_poster.") || file.Contains("_cover."))
                {
                    Poster = file;
                }
                else if (file.Contains("_screenshot", StringComparison.OrdinalIgnoreCase))
                {
                    Screenshots.Add(file);
                }
                else if (file.EndsWith("torrent", StringComparison.OrdinalIgnoreCase))
                {
                    Torrents.Add(file);
                    Date = File.GetCreationTime(file);
                    TorrentVisibility = Visibility.Visible;
                }
                else if (file.EndsWith("magnet", StringComparison.OrdinalIgnoreCase))
                {
                    Torrents.Add(file);
                    Date = File.GetCreationTime(file);
                    MagnetVisibility = Visibility.Visible;
                }
            }
            BriefInfo = $"{Pid}\n{Date}";
            if (MagnetVisibility == Visibility.Visible)
            {
                BriefInfo += "\nMagnet";
            }
        }
    }
}
