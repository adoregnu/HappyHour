using System;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using HappyHour.ViewModel;
using HappyHour.Interfaces;
using System.Threading.Tasks;
using System.Threading;
using CefSharp.DevTools.CSS;
using System.Drawing;
using System.Reflection.Metadata;
using System.Drawing.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HappyHour.Model
{
    internal abstract class AvMediaBase : ObservableObject, IAvMedia
    {
        private string _poster;
        private string _briefInfo;

        public static SortType Sort = SortType.DateCreated;
        public string Pid { get; set; }
        public string Path { get; set; }
        public virtual DateTime Date { get; set; }
        public string Poster
        {
            get => _poster;
            set 
            {
                SetProperty(ref _poster, value);
            }
        }
        public string BriefInfo
        {
            get => _briefInfo;
            set => SetProperty(ref _briefInfo, value);
        }


        public bool IsPlayable
        {
            get => this is AvMovie;
        }

        public virtual int CompareTo(IAvMedia media)
        {
            return Sort switch 
            {
                SortType.Pid => Pid.CompareTo(media.Pid),
                _ => Date.CompareTo(media.Date)
            };
        }

        public string GenPosterPath(string fileName, bool isScreenshot = false)
        {
            string ext = System.IO.Path.GetExtension(fileName);
            if (isScreenshot)
            {
                int idx = 0;
                string candidate;
                do {
                    candidate = @$"{Path}\{Pid}_screenshot_{idx}{ext}";
                    if (!File.Exists(candidate))
                    {
                        break;
                    }
                    idx++;
                } while (true);

                return candidate; 
            }
            else
            {
                return @$"{Path}\{Pid}_poster{ext}";
            }
        }

        public string GenActorThumbPath(string actorName, string fileName)
        {
            string ext = System.IO.Path.GetExtension(fileName);
            //return $@"{Path}\{Pid}\.actors\{name.Remove(' ', '_')}{ext}";
            var rname = actorName.Split(';')[0];
            return $@"{App.Current.LocalAppData}\db\{rname.Replace(' ', '_')}{ext}";
        }

        public abstract void Reload(string[] files);
        public abstract Task ReloadAsync(string[] files);
    }
}
