using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;

using Unosquare.FFME;
using Unosquare.FFME.Common;

using GalaSoft.MvvmLight.Command;
using Ude;

using HappyHour.Model;
using HappyHour.Extension;
using HappyHour.Interfaces;

namespace HappyHour.ViewModel
{
    internal class PlayerViewModel : Pane, IDisposable
    {
        private bool _isPropertiesPanelOpen = App.IsInDesignMode;
        private bool _isPlayerLoaded = App.IsInDesignMode;
        private bool _isPlaying;
        private int _fileIndex;

        private AvMovie _avMovie;
        private IMediaList _mediaList;

        public AvMovie Movie
        {
            get => _avMovie;
            set => Set(ref _avMovie, value);
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

        public string BackgroundImage =>
            MediaPlayer.IsOpen && Movie != null ? Movie.Poster : null;

        //public ICommand MouseEnterCommand { get; private set; }
        //public ICommand MouseLeaveCommand { get; private set; }
        public ICommand KeyDownCommand { get; private set; }

        public ICommand PlayCommand { get; private set; }
        public ICommand PauseCommand { get; private set; }
        public ICommand StopCommand { get; private set; }
        public ICommand CloseCommand { get; private set; }
        public ICommand BackCommand { get; private set; }
        public ICommand NextCommand { get; private set; }

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

        private void InitMediaEventHandler()
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
                int numFiles = Movie != null ? Movie.Files.Count : 0;
                Controller.BackButtonVisibility = (numFiles > 0 && _fileIndex > 0) ?
                    Visibility.Visible : Visibility.Collapsed;
                Controller.NextButtonVisibility = (numFiles > 0 && _fileIndex >= 0 && _fileIndex < numFiles - 1) ?
                    Visibility.Visible : Visibility.Collapsed;
            }, nameof(Movie));
        }

        public void SetMediaItem(IAvMedia media)
        {
            if (media is null or not AvMovie) { return; }

            Movie = (AvMovie)media;
            IsSelected = true;
            _fileIndex = 0;
            Open();
        }

        private async void Open()
        {
            if (MediaPlayer.IsOpen)
            {
                _ = await MediaPlayer.Close();
            }

            string file = Path.GetFileName(Movie.Files[_fileIndex]);
            Title = $"{file} ({_fileIndex + 1}/{Movie.Files.Count})";
            _ = await MediaPlayer.Open(new Uri(Movie.Files[_fileIndex]));
            RaisePropertyChanged(nameof(Movie));
        }

        private async void Close()
        {
            Title = "Player";
            if (MediaPlayer.IsOpen)
            {
                _ = await MediaPlayer.Close();
            }
            Movie = null;
        }

        private void Back()
        {
            _fileIndex = (_fileIndex - 1) % Movie.Files.Count;
            Open();
        }

        private void Next()
        {
            _fileIndex = (_fileIndex + 1) % Movie.Files.Count;
            Open();
        }

        private static void ConvertEncodingIfNeeded(string subPath)
        {
            ICharsetDetector cdet = new CharsetDetector();
            try
            {
                using FileStream fs = new(subPath, FileMode.Open);
                cdet.Feed(fs);
                cdet.DataEnd();
                if (cdet.Charset == "UTF-8")
                {
                    fs.Close();
                    return;
                }

                Log.Print("Charset: {0}, confidence: {1}", cdet.Charset, cdet.Confidence);

                fs.Position = 0;
                StreamReader sr = new(fs, Encoding.GetEncoding("euc-kr"), true);
                string text = sr.ReadToEnd();
                sr.Close();

                var attr = File.GetAttributes(subPath);
                if (attr.HasFlag(FileAttributes.ReadOnly))
                {
                    attr &= ~FileAttributes.ReadOnly;
                    File.SetAttributes(subPath, attr);
                }

                var sw = new StreamWriter(File.Open(subPath, FileMode.Create));
                sw.Write(text);
                sw.Close();
            }
            catch (Exception ex)
            {
                Log.Print(ex.Message, ex);
            }
        }


        private void OnMediaOpening(object sender, MediaOpeningEventArgs e)
        {
            CurrentMediaOptions = e.Options;
            var subs = Movie.Subtitles;
            if (subs != null)
            {
                string currFile = Path.GetFileNameWithoutExtension(Movie.Files[_fileIndex]);
                string sub = subs.FirstOrDefault(s =>
                    Path.GetFileNameWithoutExtension(s).StartsWith(currFile, StringComparison.OrdinalIgnoreCase));

                string[] exts = { ".smi", ".srt" };
                if (sub != null && exts.Any(e => e.Contains(Path.GetExtension(sub), StringComparison.OrdinalIgnoreCase)))
                {
                    ConvertEncodingIfNeeded(sub);
                    CurrentMediaOptions.SubtitlesSource = sub;
                }
            }
        }

        private void OnMediaEnded(object sendoer, EventArgs e)
        {
            int numFiles = Movie.Files.Count;
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

        public async void OnPKeyDown(KeyEventArgs e)
        {
            if (e.OriginalSource is System.Windows.Controls.TextBox) { return; }

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
                Log.Print("MediaPlayer.Dispose");
            }
            IsDisposed = true;
        }
      }
}
