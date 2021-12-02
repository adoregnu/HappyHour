﻿using System;
using System.Collections.Generic;
using System.Linq;

using HappyHour.Interfaces;

namespace HappyHour.Model
{
    internal class AvMovie : NotifyPropertyChanged, IAvMedia
    {
        public string Pid { get; set; }
        public string Path { get; set; }
        public string Poster { get; set; }
        public MediaType Type { get; set; }
        public DateTime Date { get; set; }
        public DateType DateType { get; set; }

        public List<string> Movies { get; set; } = new();
        public List<string> SubTitles { get; set; } = new();

        public AvItem MovieInfo { get; set; }

        public AvMovie(string path)
        {
            Type = MediaType.Movie;
            Path = path;
            Pid = path.Split('\\').Last();
        }

        public List<string> GetFiles()
        {
            return Movies;
        }

        private readonly string[] sub_exts = new string[] {
            ".smi", ".srt", ".sub", ".ass", ".ssa", ".sup"
        };
        private  readonly string[] video_exts = new string[] {
            ".mp4", ".avi", ".mkv", ".ts", ".wmv", ".m4v"
        };

        public void UpdateInfo(string file)
        {
            if (file.Contains("_poster."))
            {
                Poster = file;
            }
            else if (sub_exts.Any(s => file.EndsWith(s, StringComparison.OrdinalIgnoreCase)))
            {
                SubTitles.Add(file);
            }
            else if (video_exts.Any(s => file.EndsWith(s, StringComparison.OrdinalIgnoreCase)))
            {
                Movies.Add(file);
            }
        }
    }
}