using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace HappyHour.View
{
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
           out ulong lpFreeBytesAvailable,
           out ulong lpTotalNumberOfBytes,
           out ulong lpTotalNumberOfFreeBytes);

        private CancellationTokenSource _cancellationTokenSource;
        private DateTime _startTime;
        private long _totalBytesToCopy;
        private long _totalBytesCopied;
        private int _totalFileCount;
        private int _completedFileCount;
        private DateTime _lastUpdateTime;
        private long _lastBytesCopied;

        public bool IsCancelled { get; private set; }

        /// <summary>
        /// 복사 대상 폴더 경로
        /// </summary>
        public string DestinationFolder { get; set; }

        /// <summary>
        /// 네이티브 복사에서 마지막으로 전송된 바이트 수 추적
        /// </summary>
        public long? LastTransferredBytes { get; set; }

        public ProgressWindow()
        {
            InitializeComponent();
            _cancellationTokenSource = new CancellationTokenSource();
            _startTime = DateTime.Now;
            _lastUpdateTime = DateTime.Now;
        }

        /// <summary>
        /// 파일 복사 진행 상황을 초기화합니다.
        /// </summary>
        /// <param name="totalFiles">복사할 전체 파일 수</param>
        /// <param name="totalBytes">복사할 전체 바이트 수</param>
        public void InitializeProgress(int totalFiles, long totalBytes)
        {
            _totalFileCount = totalFiles;
            _totalBytesToCopy = totalBytes;
            _completedFileCount = 0;
            _totalBytesCopied = 0;

            Dispatcher.Invoke(() =>
            {
                CompletedFilesTextBlock.Text = $"0 / {totalFiles}";
                CopiedSizeTextBlock.Text = $"0 MB / {FormatBytes(totalBytes)}";
                PercentageTextBlock.Text = "0%";
                FilePercentageTextBlock.Text = "0%";
                OverallProgressBar.Value = 0;
                FileProgressBar.Value = 0;
            });
        }

        /// <summary>
        /// 현재 복사 중인 파일을 업데이트합니다.
        /// </summary>
        /// <param name="fileName">현재 복사 중인 파일명</param>
        public void UpdateCurrentFile(string fileName)
        {
            Dispatcher.Invoke(() =>
            {
                CurrentFileTextBlock.Text = Path.GetFileName(fileName);
                FileProgressBar.Value = 0;
                FilePercentageTextBlock.Text = "0%";
            });
        }

        /// <summary>
        /// 파일별 복사 진행률을 업데이트합니다.
        /// </summary>
        /// <param name="currentFileBytes">현재 파일에서 복사된 바이트 수</param>
        /// <param name="totalFileBytes">현재 파일의 전체 바이트 수</param>
        public void UpdateFileProgress(long currentFileBytes, long totalFileBytes)
        {
            if (totalFileBytes == 0) return;

            var filePercentage = (double)currentFileBytes / totalFileBytes * 100;
            
            Dispatcher.Invoke(() =>
            {
                FileProgressBar.Value = filePercentage;
                FilePercentageTextBlock.Text = $"{filePercentage:F1}%";
            });
        }

        /// <summary>
        /// 전체 복사 진행률을 업데이트합니다.
        /// </summary>
        /// <param name="bytesCopied">추가로 복사된 바이트 수</param>
        public void UpdateOverallProgress(long bytesCopied)
        {
            _totalBytesCopied += bytesCopied;
            
            var overallPercentage = _totalBytesToCopy > 0 ? 
                (double)_totalBytesCopied / _totalBytesToCopy * 100 : 0;

            Dispatcher.Invoke(() =>
            {
                OverallProgressBar.Value = overallPercentage;
                PercentageTextBlock.Text = $"{overallPercentage:F1}%";
                CopiedSizeTextBlock.Text = $"{FormatBytes(_totalBytesCopied)} / {FormatBytes(_totalBytesToCopy)}";
                
                UpdateSpeedAndTime();
            });
        }

        /// <summary>
        /// 파일 완료를 알립니다.
        /// </summary>
        public void CompleteFile()
        {
            _completedFileCount++;
            
            Dispatcher.Invoke(() =>
            {
                CompletedFilesTextBlock.Text = $"{_completedFileCount} / {_totalFileCount}";
                FileProgressBar.Value = 100;
                FilePercentageTextBlock.Text = "100%";
            });
        }

        /// <summary>
        /// 복사 속도와 남은 시간을 업데이트합니다.
        /// </summary>
        private void UpdateSpeedAndTime()
        {
            var currentTime = DateTime.Now;
            var elapsedTime = currentTime - _lastUpdateTime;
            
            if (elapsedTime.TotalSeconds >= 1) // 1초마다 업데이트
            {
                var bytesSinceLastUpdate = _totalBytesCopied - _lastBytesCopied;
                var speed = bytesSinceLastUpdate / elapsedTime.TotalSeconds;
                
                SpeedTextBlock.Text = $"{FormatBytes((long)speed)}/s";
                
                if (speed > 0)
                {
                    var remainingBytes = _totalBytesToCopy - _totalBytesCopied;
                    var remainingSeconds = remainingBytes / speed;
                    var remainingTime = TimeSpan.FromSeconds(remainingSeconds);
                    
                    if (remainingTime.TotalHours >= 1)
                        RemainingTimeTextBlock.Text = $"{remainingTime.Hours:D2}:{remainingTime.Minutes:D2}:{remainingTime.Seconds:D2}";
                    else
                        RemainingTimeTextBlock.Text = $"{remainingTime.Minutes:D2}:{remainingTime.Seconds:D2}";
                }
                else
                {
                    RemainingTimeTextBlock.Text = "계산 중...";
                }

                if (!string.IsNullOrEmpty(DestinationFolder))
                {
                    try
                    {
                        string pathToCheck = DestinationFolder;
                        if (!pathToCheck.EndsWith(Path.DirectorySeparatorChar.ToString()))
                        {
                            pathToCheck += Path.DirectorySeparatorChar;
                        }

                        if (GetDiskFreeSpaceEx(pathToCheck, out ulong freeBytesAvailable, out _, out _))
                        {
                            long remainingBytesToCopy = _totalBytesToCopy - _totalBytesCopied;
                            long estimatedFreeSpace = (long)freeBytesAvailable - remainingBytesToCopy;
                            RemainingDiskSpaceTextBlock.Text = FormatBytes(estimatedFreeSpace);
                        }
                        else
                        {
                            RemainingDiskSpaceTextBlock.Text = "확인 불가";
                        }
                    }
                    catch
                    {
                        RemainingDiskSpaceTextBlock.Text = "오류";
                    }
                }
                
                _lastUpdateTime = currentTime;
                _lastBytesCopied = _totalBytesCopied;
            }
        }

        /// <summary>
        /// 바이트를 읽기 쉬운 형태로 포맷합니다.
        /// </summary>
        /// <param name="bytes">바이트 수</param>
        /// <returns>포맷된 문자열</returns>
        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return $"{len:F2} {sizes[order]}";
        }

        /// <summary>
        /// 취소 토큰을 반환합니다.
        /// </summary>
        public CancellationToken CancellationToken => _cancellationTokenSource.Token;

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("파일 복사를 취소하시겠습니까?", "취소 확인", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                IsCancelled = true;
                _cancellationTokenSource.Cancel();
                CancelButton.IsEnabled = false;
                CancelButton.Content = "취소 중...";
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_totalFileCount != _completedFileCount && !IsCancelled && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                Log.Print($"OnClosing: {IsCancelled}, {_cancellationTokenSource.Token.IsCancellationRequested}, " +
                    $"{_completedFileCount}/{_totalFileCount}");
                var result = MessageBox.Show("파일 복사가 진행 중입니다. 정말로 창을 닫으시겠습니까?", 
                    "복사 진행 중", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
                
                IsCancelled = true;
                _cancellationTokenSource.Cancel();
            }
            
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            _cancellationTokenSource?.Dispose();
            base.OnClosed(e);
        }
    }
}
