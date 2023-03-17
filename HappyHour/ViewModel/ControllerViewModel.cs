
using Unosquare.FFME.Common;
using HappyHour.Extension;
namespace HappyHour.ViewModel
{
    //using Foundation;
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Media;
    using Unosquare.FFME.Common;

    //using GalaSoft.MvvmLight;
    using Unosquare.FFME;
    using CommunityToolkit.Mvvm.ComponentModel;

    /// <summary>
    /// Represents a VM for the Controller Control.
    /// </summary>
    /// <seealso cref="AttachedViewModel" />
    public sealed class ControllerViewModel : ObservableObject
    {
        readonly PlayerViewModel _mp;
        readonly MediaElement _me;

        const string VideoEqContrast = "eq=contrast=";
        const string VideoEqBrightness = ":brightness=";
        const string VideoEqSaturation = ":saturation=";

        bool m_IsAudioControlEnabled = true;
        bool m_IsSpeedRatioEnabled = true;

        Visibility m_IsMediaOpenVisibility = Visibility.Visible;
        Visibility m_ClosedCaptionsVisibility = Visibility.Visible;
        Visibility m_AudioControlVisibility = Visibility.Visible;
        Visibility m_PauseButtonVisibility = Visibility.Visible;
        Visibility m_PlayButtonVisibility = Visibility.Visible;
        Visibility m_StopButtonVisibility = Visibility.Visible;
        Visibility m_CloseButtonVisibility = Visibility.Visible;
        //Visibility m_OpenButtonVisibility = Visibility.Visible;
        Visibility m_SeekBarVisibility = Visibility.Visible;
        Visibility m_BufferingProgressVisibility = Visibility.Visible;
        Visibility m_DownloadProgressVisibility = Visibility.Visible;
        Visibility m_BackButtonVisibility = Visibility.Collapsed;
        Visibility m_NextButtonVisibility = Visibility.Collapsed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControllerViewModel"/> class.
        /// </summary>
        /// <param name="root">The root.</param>
        internal ControllerViewModel(PlayerViewModel root)
        {
            _mp = root;
            _me = root.MediaPlayer;
        }

        /// <summary>
        /// Gets or sets the video contrast.
        /// </summary>
        public double VideoContrast
        {
            get => ParseVideoEqualizerFilter().Contrast;
            set => ApplyVideoEqualizerFilter(value, null, null);
        }

        /// <summary>
        /// Gets or sets the video brightness.
        /// </summary>
        public double VideoBrightness
        {
            get => ParseVideoEqualizerFilter().Brightness;
            set => ApplyVideoEqualizerFilter(null, value, null);
        }

        /// <summary>
        /// Gets or sets the video saturation.
        /// </summary>
        public double VideoSaturation
        {
            get => ParseVideoEqualizerFilter().Saturation;
            set => ApplyVideoEqualizerFilter(null, null, value);
        }

        /// <summary>
        /// Gets or sets the is media open visibility.
        /// </summary>
        public Visibility IsMediaOpenVisibility
        {
            get => m_IsMediaOpenVisibility;
            set => SetProperty(ref m_IsMediaOpenVisibility, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is audio control enabled.
        /// </summary>
        public bool IsAudioControlEnabled
        {
            get => m_IsAudioControlEnabled;
            set => SetProperty(ref m_IsAudioControlEnabled, value);
        }

        /// <summary>
        /// Gets or sets the CC channel button control visibility.
        /// </summary>
        public Visibility ClosedCaptionsVisibility
        {
            get => m_ClosedCaptionsVisibility;
            set => SetProperty(ref m_ClosedCaptionsVisibility, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is speed ratio enabled.
        /// </summary>
        public bool IsSpeedRatioEnabled
        {
            get => m_IsSpeedRatioEnabled;
            set => SetProperty(ref m_IsSpeedRatioEnabled, value);
        }

        /// <summary>
        /// Gets or sets the audio control visibility.
        /// </summary>
        public Visibility AudioControlVisibility
        {
            get => m_AudioControlVisibility;
            set => SetProperty(ref m_AudioControlVisibility, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether media should start playing from the start when it reaches the end.
        /// </summary>
        public bool IsLoopingMediaEnabled
        {
            get
            {
                if (_me == null) return false;
                return _me.LoopingBehavior == MediaPlaybackState.Play;
            }
            set
            {
                if (_me == null) return;
                _me.LoopingBehavior = value ?
                    MediaPlaybackState.Play : MediaPlaybackState.Pause;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the pause button visibility.
        /// </summary>
        public Visibility PauseButtonVisibility
        {
            get => m_PauseButtonVisibility;
            set => SetProperty(ref m_PauseButtonVisibility, value);
        }

        /// <summary>
        /// Gets or sets the play button visibility.
        /// </summary>
        public Visibility PlayButtonVisibility
        {
            get => m_PlayButtonVisibility;
            set => SetProperty(ref m_PlayButtonVisibility, value);
        }

        /// <summary>
        /// Gets or sets the stop button visibility.
        /// </summary>
        public Visibility StopButtonVisibility
        {
            get => m_StopButtonVisibility;
            set => SetProperty(ref m_StopButtonVisibility, value);
        }
        public Visibility BackButtonVisibility
        {
            get => m_BackButtonVisibility;
            set => SetProperty(ref m_BackButtonVisibility, value);
        }
        public Visibility NextButtonVisibility
        {
            get => m_NextButtonVisibility;
            set => SetProperty(ref m_NextButtonVisibility, value);
        }
        /// <summary>
        /// Gets or sets the close button visibility.
        /// </summary>
        public Visibility CloseButtonVisibility
        {
            get => m_CloseButtonVisibility;
            set => SetProperty(ref m_CloseButtonVisibility, value);
        }

        /// <summary>
        /// Gets or sets the open button visibility.
        /// </summary>
        ///public Visibility OpenButtonVisibility
        //{
        //    get => m_OpenButtonVisibility;
        //    set => Set(ref m_OpenButtonVisibility, value);
        //}

        /// <summary>
        /// Gets or sets the seek bar visibility.
        /// </summary>
        public Visibility SeekBarVisibility
        {
            get => m_SeekBarVisibility;
            set => SetProperty(ref m_SeekBarVisibility, value);
        }

        /// <summary>
        /// Gets or sets the buffering progress visibility.
        /// </summary>
        public Visibility BufferingProgressVisibility
        {
            get => m_BufferingProgressVisibility;
            set => SetProperty(ref m_BufferingProgressVisibility, value);
        }

        /// <summary>
        /// Gets or sets the download progress visibility.
        /// </summary>
        public Visibility DownloadProgressVisibility
        {
            get => m_DownloadProgressVisibility;
            set => SetProperty(ref m_DownloadProgressVisibility, value);
        }

        /// <summary>
        /// Gets or sets the media element zoom.
        /// </summary>
        public double MediaElementZoom
        {
            get
            {
                if (_me == null) return 1d;
                var transform = _me.RenderTransform as ScaleTransform;
                return transform?.ScaleX ?? 1d;
            }
            set
            {
                if (_me == null) return;
                if (!(_me.RenderTransform is ScaleTransform transform))
                {
                    transform = new ScaleTransform(1, 1);
                    _me.RenderTransformOrigin = new Point(0.5, 0.5);
                    _me.RenderTransform = transform;
                }

                transform.ScaleX = value;
                transform.ScaleY = value;

                if (transform.ScaleX < 0.1d || transform.ScaleY < 0.1)
                {
                    transform.ScaleX = 0.1d;
                    transform.ScaleY = 0.1d;
                }
                else if (transform.ScaleX > 5d || transform.ScaleY > 5)
                {
                    transform.ScaleX = 5;
                    transform.ScaleY = 5;
                }

                OnPropertyChanged();
            }
        }

        /// <inheritdoc />
        internal void OnApplicationLoaded()
        {
            //base.OnApplicationLoaded();
            //var m = App.ViewModel.MediaElement;

            _me.WhenChanged(() => IsMediaOpenVisibility = _me.IsOpen ?
                Visibility.Visible : Visibility.Hidden, nameof(_me.IsOpen));

            _me.WhenChanged(() => ClosedCaptionsVisibility = _me.HasClosedCaptions ?
                Visibility.Visible : Visibility.Collapsed, nameof(_me.HasClosedCaptions));

            _me.WhenChanged(() =>
            {
                AudioControlVisibility = _me.HasAudio ?
                    Visibility.Visible : Visibility.Hidden;
                IsAudioControlEnabled = _me.HasAudio;
            }, nameof(_me.HasAudio));

            _me.WhenChanged(() => PauseButtonVisibility = _me.CanPause &&
                _me.IsPlaying ? Visibility.Visible : Visibility.Collapsed,
                nameof(_me.CanPause), nameof(_me.IsPlaying));

            _me.WhenChanged(() =>
            {
                PlayButtonVisibility = /*_me.IsOpen &&*/
                    _me.IsPlaying == false &&
                    _me.HasMediaEnded == false &&
                    _me.IsSeeking == false &&
                    _me.IsChanging == false ?
                        Visibility.Visible : Visibility.Collapsed;
            },
            //nameof(_me.IsOpen),
            nameof(_me.IsPlaying),
            nameof(_me.HasMediaEnded),
            nameof(_me.IsSeeking),
            nameof(_me.IsChanging));

            _me.WhenChanged(() =>
            {
                StopButtonVisibility = _me.IsOpen &&
                    _me.IsChanging == false &&
                    _me.IsSeeking == false &&
                    (_me.HasMediaEnded || (_me.IsSeekable &&
                        _me.MediaState != MediaPlaybackState.Stop)) ?
                        Visibility.Visible : Visibility.Hidden;
            },
            nameof(_me.IsOpen),
            nameof(_me.HasMediaEnded),
            nameof(_me.IsSeekable),
            nameof(_me.MediaState),
            nameof(_me.IsChanging),
            nameof(_me.IsSeeking));

            _me.WhenChanged(() => CloseButtonVisibility = (_me.IsOpen || _me.IsOpening) ?
                Visibility.Visible : Visibility.Collapsed,
                nameof(_me.IsOpen),
                nameof(_me.IsOpening));

            _me.WhenChanged(() => SeekBarVisibility = _me.IsSeekable ?
                Visibility.Visible : Visibility.Collapsed, nameof(_me.IsSeekable));

            _me.WhenChanged(() =>
            {
                BufferingProgressVisibility = _me.IsOpening || (_me.IsBuffering && _me.BufferingProgress < 0.95)
                    ? Visibility.Visible : Visibility.Hidden;
            },
            nameof(_me.IsOpening),
            nameof(_me.IsBuffering),
            nameof(_me.BufferingProgress),
            nameof(_me.Position));

            _me.WhenChanged(() =>
            {
                DownloadProgressVisibility = _me.IsOpen && _me.HasMediaEnded == false &&
                    ((_me.DownloadProgress > 0d && _me.DownloadProgress < 0.95) || _me.IsLiveStream)
                        ? Visibility.Visible : Visibility.Hidden;
            },
            nameof(_me.IsOpen),
            nameof(_me.HasMediaEnded),
            nameof(_me.DownloadProgress),
            nameof(_me.IsLiveStream));

            //_me.WhenChanged(() => OpenButtonVisibility = _me.IsOpening == false ?
            //    Visibility.Visible : Visibility.Hidden, nameof(_me.IsOpening));

            _me.WhenChanged(() => IsSpeedRatioEnabled = _me.IsOpening == false,
                nameof(_me.IsOpen), nameof(_me.IsSeekable));
        }

        private EqualizerFilterValues ParseVideoEqualizerFilter()
        {
            var result = new EqualizerFilterValues
            {
                Contrast = 1d, Brightness = 0d, Saturation = 1d
            };

            if (_me == null || _me.HasVideo == false) return result;

            var currentFilter = _mp.CurrentMediaOptions?.VideoFilter;
            if (string.IsNullOrWhiteSpace(currentFilter)) return result;

            var cIx = currentFilter.LastIndexOf(VideoEqContrast, StringComparison.Ordinal);
            var bIx = currentFilter.LastIndexOf(VideoEqBrightness, StringComparison.Ordinal);
            var sIx = currentFilter.LastIndexOf(VideoEqSaturation, StringComparison.Ordinal);

            if (cIx < 0 || bIx < 0 || sIx < 0) return result;

            var cLiteral = currentFilter.Substring(cIx + VideoEqContrast.Length, 6);
            var bLiteral = currentFilter.Substring(bIx + VideoEqBrightness.Length, 6);
            var sLiteral = currentFilter.Substring(sIx + VideoEqSaturation.Length, 6);

            result.Contrast = double.Parse(cLiteral, CultureInfo.InvariantCulture);
            result.Brightness = double.Parse(bLiteral, CultureInfo.InvariantCulture);
            result.Saturation = double.Parse(sLiteral, CultureInfo.InvariantCulture);

            return result;
        }

        private void ApplyVideoEqualizerFilter(double? contrast, double? brightness, double? saturation)
        {
            if (_me == null || _me.HasVideo == false || _mp.CurrentMediaOptions == null)
                return;

            try
            {
                var currentValues = ParseVideoEqualizerFilter();

                contrast = contrast == null ? currentValues.Contrast :
                            contrast < -2d ? -2d : contrast > 2d ? 2d : contrast;
                brightness = brightness == null ? currentValues.Brightness :
                            brightness < -1d ? -1d : brightness > 1d ? 1d : brightness;
                saturation = saturation == null ? currentValues.Saturation :
                            saturation < 0d ? 0d : saturation > 3d ? 3d : saturation;

                var targetFilter = $"{VideoEqContrast}{contrast:+0.000;-0.000}" +
                    $"{VideoEqBrightness}{brightness:+0.000;-0.000}" +
                    $"{VideoEqSaturation}{saturation:+0.000;-0.000}";
                var currentFilter = _mp.CurrentMediaOptions?.VideoFilter;

                if (string.IsNullOrWhiteSpace(currentFilter))
                {
                    _mp.CurrentMediaOptions.VideoFilter = targetFilter;
                    return;
                }

                var cIx = currentFilter.LastIndexOf(VideoEqContrast, StringComparison.Ordinal);
                _mp.CurrentMediaOptions.VideoFilter = cIx < 0
                    ? $"{currentFilter},{targetFilter}"
                    : currentFilter.Substring(0, cIx) + targetFilter;
            }
            finally
            {
                OnPropertyChanged(nameof(VideoContrast));
                OnPropertyChanged(nameof(VideoBrightness));
                OnPropertyChanged(nameof(VideoSaturation));

                // Notify a change in Video Equalizer
                //_mp.NotificationMessage = $"Contrast:   {contrast:+0.000;-0.000}\r\n" +
                //    $"Brightness: {brightness:+0.000;-0.000}\r\n" +
                //    $"Saturation: {saturation:+0.000;-0.000}";
            }
        }

        private struct EqualizerFilterValues
        {
            public double Contrast;
            public double Brightness;
            public double Saturation;
        }
    }
}
