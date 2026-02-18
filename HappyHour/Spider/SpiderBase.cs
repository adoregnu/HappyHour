using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Input;

using Scriban;
using CefSharp;

using HappyHour.ViewModel;
using HappyHour.Interfaces;
using HappyHour.Model;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Text.RegularExpressions;
using CefSharp.DevTools.WebAuthn;

namespace HappyHour.Spider
{
    internal class ScrapItem : NotifyPropertyChanged
    {
        private bool _canUpdate;

        public string Name { get; set; }
        public bool CanUpdate
        {
            get => _canUpdate;
            set => Set(ref _canUpdate, value);
        }
    }

    internal class SpiderBase : NotifyPropertyChanged
    {
        private static string _keyword;
        private static DefaultDownloader _downloader;

        private bool _saveDb;
        private bool _isCookieSet;
        private bool _isSpiderWorking;
        private bool? _checkAll;
        private IAvMedia _selectedMedia;
        private string _regexPattern;

        protected readonly Queue<IDictionary<string, object>> _itemQueue = new();
        protected virtual IDownloader Downloader => _downloader;
        protected List<string> SpiderNamesToChain = [];
        public List<string> UrlPatternsToFilter = [];

        public List<ScrapItem> ScrapItems { get; set; }
        public bool IsSpiderWorking
        {
            get => _isSpiderWorking;
            set => Set(ref _isSpiderWorking, value);
        }

        public Visibility ChainVisibility { set; get; } = Visibility.Visible;

        public ObservableCollection<SpiderBase> SpiderChain { get; set; } = [];

        public SpiderViewModel Browser { get; private set; }
        public IAvMedia SelectedMedia
        {
            get => _selectedMedia;
            set
            {
                if (value != null)
                {
                    Keyword = value.Pid;
                }
                _selectedMedia = value;
            }
        }
        public IAvMedia SearchMedia { get; set; }

        public string URL;
        public string Name { get; protected set; } = "Base";
        public string ScriptName { get; set; }
        public virtual string SearchURL => URL;
        public bool OverwritePoster { get; set; }
        public bool OverwriteActorThumb { get; set; }
        public bool OverwriteActorThumbDb { get; set; }

        public string Keyword
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_keyword) || string.IsNullOrWhiteSpace(_regexPattern))
                {
                    return _keyword;
                }

                var match = Regex.Match(_keyword, _regexPattern);
                if (!match.Success)
                {
                    return _keyword;
                }

                if (match.Groups.Count == 2)
                {
                    return match.Groups[1].Value;
                }
                else if (match.Groups.Count == 3)
                {
                    int zeroPadLength = 5 - match.Groups[2].Value.Length;
                    return $"{match.Groups[1].Value}{new string('0', zeroPadLength)}{match.Groups[2].Value}";
                }
                else
                {
                    return _keyword;
                }
            }
            set => Set(ref _keyword, value);
        }

        public ObservableCollection<string> RegexPatterns { get; set; } = [];

        public string RegexPattern
        {
            get => _regexPattern;
            set
            {
                if (_regexPattern == value)
                {
                    return;
                }

                Set(ref _regexPattern, value);
                if (!string.IsNullOrWhiteSpace(_regexPattern) && !RegexPatterns.Contains(_regexPattern))
                {
                    RegexPatterns.Add(_regexPattern);
                }
            }
        }
        public bool? CheckAll
        {
            get => _checkAll;
            set
            {
                Set(ref _checkAll, value);
                UpdateCheck(value);
            }
        }
        public ICommand CmdSearch { get; private set; }
        public ICommand CmdStopSpider { get; set; }
        public ICommand CmdScrap { get; set; }

        public SpiderBase(SpiderViewModel br)
        {
            Browser = br;
            _downloader ??= new DefaultDownloader(br);
            CmdSearch = new RelayCommand(() => { Navigate2(); });
            CmdStopSpider = new RelayCommand(async() =>
            {
                await OnScrapCompleted(false);
            });
            CmdScrap = new RelayCommand(() =>
            {
                IsSpiderWorking = true;
                _saveDb = true;
                SearchMedia = SelectedMedia;
                Scrap();
            });

            ScrapItems =
            [
                new ScrapItem() { CanUpdate = true, Name = "title" },
                new ScrapItem() { CanUpdate = true, Name = "date" },
                //new ScrapItem() { CanUpdate = true, Name = "runtime" },
                //new ScrapItem() { CanUpdate = true, Name = "director" },
                new ScrapItem() { CanUpdate = true, Name = "series" },
                new ScrapItem() { CanUpdate = true, Name = "maker" },
                new ScrapItem() { CanUpdate = true, Name = "label" },
                new ScrapItem() { CanUpdate = true, Name = "genre" },
                new ScrapItem() { CanUpdate = true, Name = "plot" },
                new ScrapItem() { CanUpdate = true, Name = "cover" },
                new ScrapItem() { CanUpdate = true, Name = "screenshot" },
                new ScrapItem() { CanUpdate = true, Name = "actor" },
                new ScrapItem() { CanUpdate = true, Name = "rating" },
            ];
        }

        public void ClearCache()
        {
            Downloader.ClearCache();
        }
        private void UpdateCheck(bool? check)
        {
            if (check == null) { return; }
            ScrapItems.ForEach(i =>
            {
                i.CanUpdate = check.Value;
            });
        }
        public void UpdateCheckAll()
        {
            int checkCount = 0;
            ScrapItems.ForEach(i =>
            {
                if (i.CanUpdate) { checkCount++; }
            });
            CheckAll = checkCount == ScrapItems.Count ? true
                : checkCount == 0 ? false : null;
        }
        public virtual void OnSelected()
        {
            Log.Print($"{Name} selected!");
            Downloader.Enable(true);
        }

        public virtual void OnDeselect()
        {
            Log.Print($"{Name} deselected!");
            Downloader.Enable(false);
        }

        protected virtual string GetScript(string name)
        {
            string common = App.ReadResource("Common.js");
            var template = Template.Parse(common + App.ReadResource(name));
            return template.Render(new { Pid = Keyword });
        }

        protected virtual List<Cookie> CreateCookie() { return null; }
        protected virtual void AdjustKeyword() { }

        public void SetCookies()
        {
            if (_isCookieSet)
            {
                return;
            }
            var cookies = CreateCookie();
            if (cookies == null)
            {
                return;
            }

            var cookieManager = Cef.GetGlobalCookieManager();
            foreach (var cookie in cookies)
            {
                _ = cookieManager.SetCookieAsync(URL, cookie);
            }
            _isCookieSet = true;
        }

        public virtual void Navigate2(IAvMedia searchMedia = null, bool resetChain = true)
        {
            if (string.IsNullOrEmpty(Keyword) && searchMedia == null)
            {
                Log.Print($"{Name}: Empty keyword!");
                return;
            }
            IsSpiderWorking = true;
            _saveDb = false;
            if (searchMedia != null)
            {
                Keyword = searchMedia.Pid;
                SearchMedia = searchMedia;
                _saveDb = true;
            }
            else if (SelectedMedia != null && SelectedMedia.Pid == Keyword)
            {
                SearchMedia = SelectedMedia;
            }
            AdjustKeyword();
            if (resetChain)
            {
                Browser.ResetChain();
            }
            Browser.SelectedSpider = this;
        }

        public virtual void SetAddress()
        {
            string newUrl = string.IsNullOrEmpty(Keyword) ? URL : SearchURL;
            if (Browser.Address == newUrl)
            {
                Browser.Address = "";
            }
            Log.Print($"Set new url : {newUrl}");
            Browser.Address = newUrl;
        }

        public static bool IterateDynamic(IDictionary<string, object> dict,
           Func<string, IDictionary<string, object>, bool> action)
        {
            bool IterateList(string key, List<object> list)
            {
                foreach (object obj in list)
                {
                    if (obj is IDictionary<string, object> dict2)
                    {
                        if (!IterateDynamic(dict2, action))
                        {
                            return false;
                        }
                    }
                    else if (obj is List<object> list2)
                    {
                        if (!IterateList(key, list2))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        //action(key, obj);
                    }
                }
                return true;
            }

            foreach (var item in dict)
            {
                if (item.Value == null)
                {
                    continue;
                }
                if (item.Value is IDictionary<string, object> dictionary)
                {
                    if (!IterateDynamic(dictionary, action))
                    {
                        return false;
                    }
                    _ = action(item.Key, dict);
                }
                else if (item.Value is List<object> list)
                {
                    if (!IterateList(item.Key, list))
                    {
                        return false;
                    }
                    _ = action(item.Key, dict);
                }
                else if (dict[item.Key] != null && action(item.Key, dict))
                {
                    return false;
                }
            }
            return true;
        }

        private bool FollowLink()
        {
            if (_itemQueue.Count == 0)
            {
                return false;
            }
            string link = null;
            _ = IterateDynamic(_itemQueue.Peek(), (key, dict) =>
            {
                if (key == "link")
                {
                    link = dict[key].ToString();
                    return dict.Remove(key);
                }
                return false;
            });

            if (!string.IsNullOrEmpty(link))
            {
                Browser.Address = link;
                return true;
            }

            _ = _itemQueue.Dequeue();
            return FollowLink();
        }

        protected async virtual Task OnScrapCompleted(bool bUpdated)
        {
            if (SearchMedia != null && SearchMedia.IsPlayable && FollowLink())
            {
                return;
            }

            _itemQueue.Clear();
            IsSpiderWorking = false;
            Log.Print($"{Name}: {Keyword} ScrapCompleted");

            if (bUpdated && SearchMedia != null)
            {
                await SearchMedia.ReloadAsync();
            }

            if (SearchMedia != null && !Browser.SetNextSpider(SearchMedia))
            {
                //await Task.Delay(1000);
                SearchMedia = null;
                Browser.OnScrapCompleted?.Invoke(this, true);
            }
        }

        public virtual void Scrap()
        {
            if (IsSpiderWorking && !string.IsNullOrEmpty(ScriptName))
            {
                Browser.ExecJavaScript(GetScript(ScriptName), bSuccess => {
                    if (!bSuccess)
                    {
                        IsSpiderWorking = false;
                        SearchMedia = null;
                        Browser.OnScrapCompleted?.Invoke(this, false);
                    }
                });
            }
        }

        private void SkipDownloadIfNeeded(IDictionary<string, object> items)
        {
            List<(string key, IDictionary<string, object> dict)> tmpList = [];
            IterateDynamic(items, (key, dict) =>
            {
                string path = null;
                if (!OverwritePoster && key is "cover")
                {
                    path = SearchMedia.GenPosterPath(dict[key].ToString());
                }
                else if (!OverwriteActorThumb && key is "thumb")
                {
                    path = SearchMedia.GenActorThumbPath(dict["name"].ToString(), dict[key].ToString());
                }

                if (path != null && File.Exists(path))
                {
                    tmpList.Add((key, dict));
                    Log.Print($"{Name}:: {key} is dropped by setting!");
                }
                return false;
            });
            tmpList.ForEach(tp => tp.dict.Remove(tp.key));
        }
        private void ApplyItemSettings(IDictionary<string, object> items)
        {
            ScrapItems.ForEach(i =>
            {
                if (items.ContainsKey(i.Name) && !i.CanUpdate)
                {
                    _ = items.Remove(i.Name);
                    Log.Print($"{Name}:: {i.Name} is dropped by setting!");
                }
            });
        }

        public async virtual ValueTask<bool> OnJsMessageReceived(JavascriptMessageReceivedEventArgs msg)
        {
            dynamic d = msg.Message;
            Log.Print($"{d.type} : {d.data}");

            if (d.type == "url")
            {
                Browser.Address = d.data;
                return true;
            }

            //if (!_saveDb) return true;

            if (d.type == "items")
            {
                if (d.data == 0)
                {
                    Log.Print($"{Name}: {Keyword}: No exact matched ID");
                    await OnScrapCompleted(true);
                    return true;
                }
                if (SearchMedia == null)
                {
                    Log.Print("Media is not selected!");
                    return true;
                }
                _itemQueue.Enqueue(d);
                try
                {
                    ApplyItemSettings(d);
                    //SkipDownloadIfNeeded(d);
                    await Downloader.Download(this, d);
                }
                catch (Exception ex)
                {
                    Log.Print($"{Name}:", ex);
                }
                return true;
            }
            return false;
        }

        public virtual void UpdateDownload(string rpath)
        {
            UiServices.Invoke(async () => {
                await Downloader?.UpdateDownload(this, rpath);
            });
        }

        protected async virtual Task UpdateDb(IDictionary<string, object> items)
        {
            items["path"] = SearchMedia.Path;
            items["pid"] = SearchMedia.Pid;
            await App.Current.DbContext.SetMovie(items);
        }

        protected virtual Task Translate(IDictionary<string, object> items)
        {
            //translate title and plot
            if (items.TryGetValue("title", out var title))
            { 

            }
    
            return Task.CompletedTask;
        }

        public async Task UpdateItemsAsync(IDictionary<string, object> items)
        {
            if (_saveDb && SearchMedia is AvMovie)
            {
                try
                {
                    //await Translate(items);
                    await UpdateDb(items);
                }
                catch (Exception ex)
                {
                    Log.Print($"{Name}: UpdateItem", ex);
                }
            }
            else if (SearchMedia is AvTorrent torrent)
            {
                await torrent.UpdateDb(items);
            }

            await OnScrapCompleted(true);
        }

        public override string ToString()
        {
            return Name;
        }

        public void InitChain(List<SpiderBase> spiders)
        {
            spiders.ForEach(s =>
            {
                if (SpiderNamesToChain.Any(n => n == s.Name))
                {
                    SpiderChain.Add(s);
                }
            });
        }
    }
}
