using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;

using Unosquare.FFME;
using Unosquare.FFME.Common;

using GalaSoft.MvvmLight.Command;
using Ude;

using HappyHour.Model;
using HappyHour.Extension;
using HappyHour.Interfaces;

namespace HappyHour.ViewModel
{
    class PlayerViewModel : Pane, IDisposable
    {
        bool _isPropertiesPanelOpen = App.IsInDesignMode;
        bool _isPlayerLoaded = App.IsInDesignMode;
        bool _isPlaying = false;
        MediaItem _mediaItem;

        public MediaItem MediaItem
        {
            get => _mediaItem;
            set => Set(ref _mediaItem, value);
        }
        public MediaElement MediaPlayer { get; private set; }
        public MediaOptions CurrentMediaOptions { get; set; }
        public ControllerViewModel Controller { get; private set; }
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is properties panel open.
        /// </summary>
        public bool IsPropertiesPanelOpen
        {
            get => _isPropertiesPanelOpen;
            set => Set(ref _isPropertiesPanelOpen, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is application loaded.
        /// </summary>
        public bool IsPlayerLoaded
        {
            get => _isPlayerLoaded;
            set => Set(ref _isPlayerLoaded, value);
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set => Set(ref _isPlaying, value);
        }

        public string BackgroundImage
        {
            get
            {
                if (MediaPlayer.IsOpen && MediaItem != null)
                    return MediaItem.Poster;
                else
                    return null;
            }
        }

        //public ICommand MouseEnterCommand { get; private set; }
        //public ICommand MouseLeaveCommand { get; private set; }
        public ICommand KeyDownCommand { get; private set; }

        public ICommand PlayCommand { get; private set; }
        public ICommand PauseCommand { get; private set; }
        public ICommand StopCommand { get; private set; }
        public ICommand CloseCommand { get; private set; }
        public ICommand BackCommand { get; private set; }
        public ICommand NextCommand { get; private set; }

        IMediaList _mediaList;
        public IMediaList MediaList
        {
            get => _mediaList;
            set
            {
                _mediaList = value;
                _mediaList.ItemDoubleClickedHandler += (o, i) => SetMediaItem(i);
            }
        }

        public PlayerViewModel()
        {
            Title = "Player";
            MediaElement.FFmpegMessageLogged += OnMediaFFmpegMessageLogged;

            MediaPlayer = new MediaElement
            {
                Background = Brushes.Black,
                // https://stackoverflow.com/questions/24321237/switching-a-control-over-different-windows-inside-contentcontrol
                UnloadedBehavior = MediaPlaybackState.Manual,
                IsDesignPreviewEnabled = true,
                IsMuted = true
            };
            MediaPlayer.LoadedBehavior = MediaPlaybackState.Stop;


            KeyDownCommand = new RelayCommand<KeyEventArgs>(e => OnPKeyDown(e));

            PlayCommand = new RelayCommand(async () => await MediaPlayer.Play());
            PauseCommand = new RelayCommand(async () => await MediaPlayer.Pause());
            StopCommand = new RelayCommand(async () => await MediaPlayer.Stop());
            CloseCommand = new RelayCommand(() => Close());
            BackCommand = new RelayCommand(() => Back());
            NextCommand = new RelayCommand(() => Next());
            Controller = new ControllerViewModel(this);
            Controller.OnApplicationLoaded();
            InitMediaEventHandler();

            IsPlayerLoaded = true;
        }

        int _fileIndex = 0;
        void InitMediaEventHandler()
        {
            //MediaPlayer.MediaReady += OnMediaReady;
            //MediaPlayer.MediaInitializing += OnMediaInitializing;
            MediaPlayer.MediaEnded += OnMediaEnded;
            MediaPlayer.MediaOpening += OnMediaOpening;
            MediaPlayer.WhenChanged(() =>  
                IsPlaying = MediaPlayer.IsPlaying ||
                    MediaPlayer.IsSeeking ||
                    MediaPlayer.MediaState == MediaPlaybackState.Pause,
                nameof(MediaPlayer.IsPlaying));
            MediaPlayer.WhenChanged(() =>
                RaisePropertyChanged(nameof(BackgroundImage)),
                nameof(MediaPlayer.IsOpen));

            this.WhenChanged(() =>
            {
                var numFiles = MediaItem != null
                    ? MediaItem.MediaFiles.Count : 0;
                Controller.BackButtonVisibility =
                    (numFiles > 0 && _fileIndex > 0)
                        ? Visibility.Visible : Visibility.Collapsed;
                Controller.NextButtonVisibility =
                    (numFiles > 0 && _fileIndex >= 0 && _fileIndex < numFiles - 1)
                        ? Visibility.Visible : Visibility.Collapsed;
            }, nameof(MediaItem));
        }

        public void SetMediaItem(MediaItem media)
        {
            if (media == null) return;

            MediaItem = media;
            IsSelected = true;
            _fileIndex = 0;
            Open();
        }

        async void Open()
        { 
            if (MediaPlayer.IsOpen)
            {
                await MediaPlayer.Close();
            }

            var files = MediaItem.MediaFiles;
            var file = Path.GetFileName(files[_fileIndex]);
            Title = $"{file} ({_fileIndex + 1}/{files.Count})";
            await MediaPlayer.Open(new Uri(files[_fileIndex]));
            RaisePropertyChanged(nameof(MediaItem));
        }

        async void Close()
        {
            Title = "Player";
            if (MediaPlayer.IsOpen)
                await MediaPlayer.Close();
            MediaItem = null;
        }

        void Back()
        {
            _fileIndex = (_fileIndex - 1) % MediaItem.MediaFiles.Count;
            Open();
        }

        void Next()
        {
            _fileIndex = (_fileIndex + 1) % MediaItem.MediaFiles.Count;
            Open();
        }

        string DetectEncoding(string subPath)
        {
            using FileStream fs = File.OpenRead(subPath);
            ICharsetDetector cdet = new CharsetDetector();
            cdet.Feed(fs);
            cdet.DataEnd();
            if (cdet.Charset != null)
            {
                Log.Print("Charset: {0}, confidence: {1}",
                     cdet.Charset, cdet.Confidence);
                return cdet.Charset;
            }
            return null;
        }


        void OnMediaOpening(object sender, MediaOpeningEventArgs e)
        {
            CurrentMediaOptions = e.Options;
            var subs = MediaItem.Subtitles;
            if (subs != null)
            {
                var currFile = Path.GetFileNameWithoutExtension(
                    MediaItem.MediaFiles[_fileIndex]);
                var sub = subs.FirstOrDefault(s =>
                    Path.GetFileNameWithoutExtension(s)
                        .StartsWith(currFile, StringComparison.OrdinalIgnoreCase));
                if (sub != null)
                {
                    //string charset = DetectEncoding(sub);
                    //if (charset != null && charset != "UTF-8")
                    //    CurrentMediaOptions.DecoderParams["sub_charenc"] = charset;
                    CurrentMediaOptions.SubtitlesSource = sub;
                }
            }
        }

        void OnMediaEnded(object sendoer, EventArgs e)
        {
            var numFiles = MediaItem.MediaFiles.Count;
            if (numFiles > 0 && _fileIndex >= 0 && _fileIndex < numFiles - 1)
            {
                Next();
            }
        }

#if false
        void OnMediaReady(object sender, EventArgs e)
        {
            //Log.Print($"OnMediaReady");
        }

        void OnMediaInitializing(object sender, MediaInitializingEventArgs e)
        { 
            //Log.Print($"OnMediaInitializing");
        }
#endif
        static readonly Key[] TogglePlayPauseKeys = {
            Key.Play, Key.MediaPlayPause, Key.Space
        };

        async public void OnPKeyDown(KeyEventArgs e)
        {
            if (e.OriginalSource is System.Windows.Controls.TextBox)
                return;

            // Pause
            if (TogglePlayPauseKeys.Contains(e.Key) && MediaPlayer.IsPlaying)
            {
                PauseCommand.Execute(null);
                return;
            }
            // Play
            if (TogglePlayPauseKeys.Contains(e.Key) && MediaPlayer.IsPlaying == false)
            {
                PlayCommand.Execute(null);
                return;
            }

            // Seek to left
            if (e.Key == Key.Left)
            {
                var fpos = MediaPlayer.FramePosition;
                fpos -= TimeSpan.FromSeconds(5);
                await MediaPlayer.Seek(fpos);
                //await MediaPlayer.StepBackward();
                return;
            }

            // Seek to right
            if (e.Key == Key.Right)
            {
                var fpos = MediaPlayer.FramePosition;
                fpos += TimeSpan.FromSeconds(5);
                await MediaPlayer.Seek(fpos);
                //await MediaPlayer.StepForward();
                return;
            }

            // Volume Up
            if (e.Key == Key.Add || e.Key == Key.VolumeUp)
            {
                MediaPlayer.Volume += MediaPlayer.Volume >= 1 ? 0 : 0.05;
                //ViewModel.NotificationMessage = $"Volume: {Media.Volume:p0}";
                return;
            }

            // Volume Down
            if (e.Key == Key.Subtract || e.Key == Key.VolumeDown)
            {
                MediaPlayer.Volume -= MediaPlayer.Volume <= 0 ? 0 : 0.05;
                //ViewModel.NotificationMessage = $"Volume: {Media.Volume:p0}";
                return;
            }

            // Mute/Unmute
            if (e.Key == Key.M || e.Key == Key.VolumeMute)
            {
                MediaPlayer.IsMuted = !MediaPlayer.IsMuted;
                //ViewModel.NotificationMessage = 
                //  Media.IsMuted ? "Muted." : "Unmuted.";
                return;
            }

            // Increase speed
            if (e.Key == Key.Up)
            {
                MediaPlayer.SpeedRatio += 0.05;
                return;
            }

            // Decrease speed
            if (e.Key == Key.Down)
            {
                MediaPlayer.SpeedRatio -= 0.05;
                return;
            }
        }
        void OnMediaFFmpegMessageLogged(object sender, MediaLogMessageEventArgs e)
        {
            if (e.MessageType != MediaLogMessageType.Warning &&
                e.MessageType != MediaLogMessageType.Error)
                return;

            if (string.IsNullOrWhiteSpace(e.Message) == false &&
                e.Message.ContainsOrdinal("Using non-standard frame rate"))
                return;

            Log.Print(e.Message);
        }

        void IDisposable.Dispose()
        {
            if (!IsDisposed)
            {
                MediaPlayer.RemoveSubscripion();
                MediaPlayer.Dispose();
                IsDisposed = true;
            }
            IsDisposed = true;
        }
    }
}
