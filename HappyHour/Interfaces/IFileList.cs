using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyHour.Interfaces
{
    public delegate void FileListDirChangeEventHandler(object sender, DirectoryInfo e);
    public delegate void FileListWatcherEventHandler(object sender, FileSystemEventArgs e);

    interface IFileList
    {
        DirectoryInfo CurrDirInfo { get; }
        FileListDirChangeEventHandler DirChanged { set; get; }
        FileListWatcherEventHandler DirModifed { get; set; }
    }
}
