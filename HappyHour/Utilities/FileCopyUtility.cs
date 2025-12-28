using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using HappyHour.View;
using RoboSharp; // added

namespace HappyHour.Utilities
{
    public class FileCopyUtility
    {
        #region Public Copy / Move APIs
        public static async Task<bool> CopyItemsWithProgressAsync(IEnumerable<string> sourcePaths, string destinationFolder, bool overwrite = false, bool includeSubFolders = true, Window owner = null)
            => await ProcessItemsAsync(sourcePaths, destinationFolder, overwrite, includeSubFolders, false, owner);

        public static async Task<bool> MoveItemsWithProgressAsync(IEnumerable<string> sourcePaths, string destinationFolder, bool overwrite = false, bool includeSubFolders = true, Window owner = null)
            => await ProcessItemsAsync(sourcePaths, destinationFolder, overwrite, includeSubFolders, true, owner);
        #endregion

        #region Core Process Helpers

        private static async Task<bool> ProcessItemsAsync(IEnumerable<string> sourcePaths, string destinationFolder, bool overwrite, bool includeSubFolders, bool isMove, Window owner)
        {
            var list = sourcePaths.ToList();
            if (!list.Any()) return true;
            Directory.CreateDirectory(destinationFolder);

            var files = new List<FileInfo>();
            var folders = new List<string>();
            long totalBytes = 0;
            foreach (var p in list)
            {
                if (File.Exists(p)) { var fi = new FileInfo(p); files.Add(fi); totalBytes += fi.Length; }
                else if (Directory.Exists(p))
                {
                    var items = await Task.Run(() => GetAllItemsInFolder(p, includeSubFolders));
                    files.AddRange(items.Files); folders.AddRange(items.Folders); totalBytes += items.TotalBytes;
                }
            }
            if (!files.Any() && !folders.Any())
            {
                MessageBox.Show("처리할 유효한 항목이 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            var progressWindow = new ProgressWindow { Title = isMove ? "항목 이동 진행 상황" : "항목 복사 진행 상황" };
            if (owner != null) progressWindow.Owner = owner;
            progressWindow.DestinationFolder = destinationFolder;
            progressWindow.InitializeProgress(files.Count + folders.Count, totalBytes);
            progressWindow.Show();

            try
            {
                return await Task.Run(async () =>
                {
                    var root = GetCommonRootPath(list.Where(Directory.Exists));
                    foreach (var folder in folders)
                    {
                        if (progressWindow.CancellationToken.IsCancellationRequested) return false;
                        var relative = !string.IsNullOrEmpty(root) ? Path.GetRelativePath(root, folder) : Path.GetFileName(folder);
                        var target = Path.Combine(destinationFolder, relative);
                        progressWindow.UpdateCurrentFile($"폴더 {(isMove ? "이동" : "생성")}: {relative}");
                        try { Directory.CreateDirectory(target); }
                        catch (Exception ex)
                        {
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                                MessageBox.Show($"폴더 생성 실패 '{relative}': {ex.Message}", "폴더 생성 오류", MessageBoxButton.OK, MessageBoxImage.Warning));
                        }
                        progressWindow.CompleteFile();
                        await Task.Delay(10, progressWindow.CancellationToken);
                    }

                    foreach (var file in files)
                    {
                        if (progressWindow.CancellationToken.IsCancellationRequested) return false;
                        var target = CalculateTargetPath(file.FullName, list, destinationFolder);
                        progressWindow.UpdateCurrentFile(file.Name);
                        Directory.CreateDirectory(Path.GetDirectoryName(target)!);

                        if (File.Exists(target) && !overwrite)
                        {
                            var result = await Application.Current.Dispatcher.InvokeAsync(() =>
                                MessageBox.Show($"파일 '{file.Name}'이 이미 존재합니다. 덮어쓰시겠습니까?", isMove ? "파일 덮어쓰기 (이동)" : "파일 덜어쓰기 (복사)", MessageBoxButton.YesNoCancel, MessageBoxImage.Question));
                            if (result == MessageBoxResult.Cancel) return false;
                            if (result == MessageBoxResult.No) { progressWindow.CompleteFile(); continue; }
                        }

                        bool success = await RoboSharpFileCopy(file.FullName, target, progressWindow, isMove);
                        if (!success) return false;
                        progressWindow.CompleteFile();
                    }

                    if (isMove)
                    {
                        foreach (var p in list.Where(Directory.Exists))
                        {
                            try
                            {
                                if (Directory.Exists(p) && !Directory.GetFiles(p, "*", SearchOption.AllDirectories).Any())
                                    Directory.Delete(p, true);
                            }
                            catch (Exception ex)
                            {
                                await Application.Current.Dispatcher.InvokeAsync(() =>
                                    MessageBox.Show($"원본 폴더 삭제 실패 '{p}': {ex.Message}", "폴더 삭제 오류", MessageBoxButton.OK, MessageBoxImage.Warning));
                            }
                        }
                    }
                    return true;
                });
            }
            catch (OperationCanceledException) { return false; }
            catch (Exception ex)
            {
                MessageBox.Show($"항목 {(isMove ? "이동" : "복사")} 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            finally { progressWindow.Dispatcher.Invoke(() => progressWindow.Close()); }
        }
        #endregion

        #region General Helpers
        private class FolderItems
        {
            public List<FileInfo> Files { get; } = new();
            public List<string> Folders { get; } = new();
            public long TotalBytes { get; set; }
        }
        private static FolderItems GetAllItemsInFolder(string folderPath, bool includeSub)
        {
            var result = new FolderItems();
            try
            {
                var opt = includeSub ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                foreach (var f in Directory.GetFiles(folderPath, "*", opt))
                {
                    try { var fi = new FileInfo(f); result.Files.Add(fi); result.TotalBytes += fi.Length; }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"파일 접근 실패: {f}, {ex.Message}"); }
                }
                if (includeSub) result.Folders.AddRange(Directory.GetDirectories(folderPath, "*", opt));
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"폴더 탐색 실패: {folderPath}, {ex.Message}"); }
            return result;
        }
        private static string GetCommonRootPath(IEnumerable<string> paths)
        {
            var list = paths.ToList();
            if (list.Count == 0)
                return string.Empty;
            if (list.Count == 1)
                return Path.GetDirectoryName(list[0]) ?? string.Empty;

            var common = list[0];
            foreach (var p in list.Skip(1)) { common = GetCommonPath(common, p); if (string.IsNullOrEmpty(common)) break; }
            return common ?? string.Empty;
        }
        private static string GetCommonPath(string p1, string p2)
        {
            var a = p1.Split(Path.DirectorySeparatorChar); var b = p2.Split(Path.DirectorySeparatorChar); var parts = new List<string>();
            for (int i = 0; i < Math.Min(a.Length, b.Length); i++) { if (string.Equals(a[i], b[i], StringComparison.OrdinalIgnoreCase)) parts.Add(a[i]); else break; }
            return parts.Any() ? string.Join(Path.DirectorySeparatorChar, parts) : string.Empty;
        }
        private static string CalculateTargetPath(string filePath, List<string> sourcePaths, string destinationFolder)
        {
            var containing = sourcePaths.FirstOrDefault(p => filePath.StartsWith(p, StringComparison.OrdinalIgnoreCase));
            if (containing != null)
            {
                if (File.Exists(containing)) return Path.Combine(destinationFolder, Path.GetFileName(filePath));
                var rel = Path.GetRelativePath(containing, filePath);
                return Path.Combine(destinationFolder, Path.GetFileName(containing), rel);
            }
            return Path.Combine(destinationFolder, Path.GetFileName(filePath));
        }
        #endregion


        #region CopyFileEx Fallback

        private static async Task<bool> RoboSharpFileCopy(string sourceFile, string destinationFile, ProgressWindow progressWindow, bool isMove)
        {
            try
            {
                var sourceDir = Path.GetDirectoryName(sourceFile);
                var fileName = Path.GetFileName(sourceFile);
                var destDir = Path.GetDirectoryName(destinationFile);

                Directory.CreateDirectory(destDir);

                var cmd = new RoboCommand();
                cmd.CopyOptions.Source = sourceDir;
                cmd.CopyOptions.Destination = destDir;
                cmd.CopyOptions.FileFilter = [ fileName ];
                cmd.CopyOptions.UseUnbufferedIo = true;
                cmd.CopyOptions.MultiThreadedCopiesCount = 8;
                cmd.CopyOptions.MoveFiles = isMove;

                double lastTransferred = 0;
                var fileInfo = new FileInfo(sourceFile);
                var totalSize = fileInfo.Length;

                cmd.OnCopyProgressChanged += (s, e) =>
                {
                    if (progressWindow.CancellationToken.IsCancellationRequested)
                    {
                        cmd.Stop();
                        return;
                    }

                    var transferred = e.CurrentFileProgress - lastTransferred;
                    if (transferred >= 1.0)
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            progressWindow.UpdateCurrentFile(e.CurrentFile.Name);
                            progressWindow.UpdateFileProgress((long)e.CurrentFileProgress, 100);
                            progressWindow.UpdateOverallProgress((long)(e.CurrentFile.Size * (transferred / 100)));
                        }),  DispatcherPriority.Background);
                        lastTransferred = e.CurrentFileProgress;
                    }
                };

                var results = await cmd.StartAsync();
                return results.Status.Successful;
            }
            catch (OperationCanceledException)
            {
                return false; // Task was cancelled
            }
            catch (Exception ex)
            {
                MessageBox.Show($"파일 '{Path.GetFileName(sourceFile)}' {(isMove ? "이동" : "복사")} 중 오류: {ex.Message}", $"파일 {(isMove ? "이동" : "복사")} 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        #endregion
    }
}