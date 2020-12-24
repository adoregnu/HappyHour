using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

using HappyHour.ViewModel;

namespace HappyHour.View
{
    /// <summary>
    /// Interaction logic for MediaPlayerView.xaml
    /// </summary>
    public partial class PlayerView : UserControl
    {
        bool IsControllerHideCompleted;
        DateTime LastMouseMoveTime;
        Point LastMousePosition;
        DispatcherTimer MouseMoveTimer;

        Storyboard HideControllerAnimation =>
            FindResource("HideControlOpacity") as Storyboard;
        Storyboard ShowControllerAnimation =>
            FindResource("ShowControlOpacity") as Storyboard;

        Unosquare.FFME.MediaElement _me;

        public PlayerView()
        {
            InitializeComponent();
            InitializePlayer();
        }

        void InitializePlayer()
        {
            LastMouseMoveTime = DateTime.UtcNow;

            Loaded += (s, e) =>
            {
                Storyboard.SetTarget(HideControllerAnimation, ControllerPanel);
                Storyboard.SetTarget(ShowControllerAnimation, ControllerPanel);

                HideControllerAnimation.Completed += (es, ee) =>
                {
                    ControllerPanel.Visibility = Visibility.Hidden;
                    IsControllerHideCompleted = true;
                };

                ShowControllerAnimation.Completed += (es, ee) =>
                {
                    IsControllerHideCompleted = false;
                };
                _me = (DataContext as PlayerViewModel).MediaPlayer;
            };

            MouseMove += (s, e) =>
            {
                var currentPosition = e.GetPosition(this);
                if (Math.Abs(currentPosition.X - LastMousePosition.X) > double.Epsilon ||
                    Math.Abs(currentPosition.Y - LastMousePosition.Y) > double.Epsilon)
                    LastMouseMoveTime = DateTime.UtcNow;

                LastMousePosition = currentPosition;
            };

            MouseLeave += (s, e) =>
            {
                LastMouseMoveTime = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(10));
            };

            MouseMoveTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(150),
                IsEnabled = true
            };

            MouseMoveTimer.Tick += (s, e) =>
            {
                var elapsedSinceMouseMove = DateTime.UtcNow.Subtract(LastMouseMoveTime);
                if (elapsedSinceMouseMove.TotalMilliseconds >= 3000 &&
                    _me.IsOpen && ControllerPanel.IsMouseOver == false &&
                    PropertiesPanel.Visibility != Visibility.Visible &&
                    ControllerPanel.SoundMenuPopup.IsOpen == false)
                {
                    if (IsControllerHideCompleted) return;
                    Cursor = Cursors.None;
                    HideControllerAnimation?.Begin();
                    IsControllerHideCompleted = false;
                }
                else
                {
                    Cursor = Cursors.Arrow;
                    ControllerPanel.Visibility = Visibility.Visible;
                    ShowControllerAnimation?.Begin();
                }
            };

            MouseMoveTimer.Start();
        }

    }
}
