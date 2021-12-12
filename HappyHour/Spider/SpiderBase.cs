using System;
using System.Collections.Generic;
using System.Windows.Input;

using Scriban;
using CefSharp;

using GalaSoft.MvvmLight.Command;

using HappyHour.ViewModel;
using HappyHour.ScrapItems;
using HappyHour.Interfaces;

namespace HappyHour.Spider
{
    internal delegate void ScrapCompletedHandler(SpiderBase spider);
    internal class ScrapItem : NotifyPropertyChanged
    {
        private bool _canUpdate;
        private static bool _skipDlIfExists;

        public string Name { get; set; }
        public bool SkipDlIfExists
        {
            get => _skipDlIfExists;
            set => Set(ref _skipDlIfExists, value);
        }

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
        private IAvMedia _selectedMedia;

        protected readonly Queue<IDictionary<string, object>> _itemQueue = new();
        protected virtual IDownloader Downloader => _downloader;

        public List<ScrapItem> ScrapItems { get; set; }
        public bool IsSpiderWorking
        {
            get => _isSpiderWorking;
            set => Set(ref _isSpiderWorking, value);
        }

        public SpiderViewModel Browser { get; private set; }
        public ScrapCompletedHandler ScrapCompleted { get; set; }
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

        public string Keyword
        {
            get => _keyword;
            set => Set(ref _keyword, value);
        }

        public ICommand CmdSearch { get; private set; }
        public ICommand CmdStopSpider { get; set; }

        public SpiderBase(SpiderViewModel br)
        {
            Browser = br;
            if (_downloader == null)
            {
               _downloader = new DefaultDownloader(br);
            }
            CmdSearch = new RelayCommand(() => { Navigate2(); });
            CmdStopSpider = new RelayCommand(() => OnScrapCompleted(false));

            ScrapItems = new()
            {
                new ScrapItem() { CanUpdate = true, Name = "title" },
                new ScrapItem() { CanUpdate = true, Name = "date" },
                //new ScrapItem() { CanUpdate = true, Name = "runtime" },
                //new ScrapItem() { CanUpdate = true, Name = "director" },
                new ScrapItem() { CanUpdate = true, Name = "series" },
                new ScrapItem() { CanUpdate = true, Name = "studio" },
                new ScrapItem() { CanUpdate = true, Name = "genre" },
                new ScrapItem() { CanUpdate = true, Name = "plot" },
                new ScrapItem() { CanUpdate = false, Name = "cover" },
                new ScrapItem() { CanUpdate = true, Name = "actor" },
                new ScrapItem() { CanUpdate = true, Name = "rating" },
            };
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
            var template = Template.Parse(App.ReadResource(name));
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
                cookieManager.SetCookieAsync(URL, cookie);
            }
            _isCookieSet = true;
        }

        public virtual void Navigate2(IAvMedia searchMedia = null)
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
            Browser.SelectedSpider = this;
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
                }
                else if (item.Value is List<object> list)
                {
                    if (!IterateList(item.Key, list))
                    {
                        return false;
                    }
                    action(item.Key, dict);
                }
                else if (action(item.Key, dict))
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
                    _ = dict.Remove(key);
                    return true;
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

        protected virtual void OnScrapCompleted(bool bUpdated)
        {
            if (/*bUpdated && */FollowLink())
            {
                return;
            }

            _itemQueue.Clear();
            IsSpiderWorking = false;
            Log.Print($"{Name}: {Keyword} ScrapCompleted");

            if (bUpdated && SearchMedia != null)
            {
                SearchMedia.Reload();
            }
            SearchMedia = null;
            ScrapCompleted?.Invoke(this);
        }

        public virtual void Scrap()
        {
            if (IsSpiderWorking && !string.IsNullOrEmpty(ScriptName))
            {
                Browser.ExecJavaScript(GetScript(ScriptName));
                return;
            }
        }

        private void ApplyItemSettings(IDictionary<string, object> items)
        {
            ScrapItems.ForEach(i =>
            {
                if (items.ContainsKey(i.Name) && !i.CanUpdate)
                {
                    _ = items.Remove(i.Name);
                    Log.Print($"{Name}:: {i.Name} dropped by setting!");
                }
            });
        }

        public virtual void OnJsMessageReceived(JavascriptMessageReceivedEventArgs msg)
        {
            dynamic d = msg.Message;
            Log.Print($"{d.type} : {d.data}");
            if (d.type == "url")
            {
                Browser.Address = d.data;
            }
            else if (d.type == "items")
            {
                if (d.data == 0)
                {
                    Log.Print($"{Name}: {Keyword}: No exact matched ID");
                    OnScrapCompleted(false);
                    return;
                }
                _itemQueue.Enqueue(d);
                try
                {
                    ApplyItemSettings(d);
                    if (SearchMedia != null)
                    {
                        Downloader.Download(this, d);
                    }
                    else
                    {
                        OnScrapCompleted(false);
                    }
                }
                catch (Exception ex)
                {
                    Log.Print($"{Name}:", ex);
                }
            }
        }

        protected virtual void UpdateDb(IDictionary<string, object> items)
        {
            try
            {
                new ItemBase2(SearchMedia).UpdateItems(items);
            }
            catch (Exception ex)
            {
                Log.Print($"{Name}: UpdateDb failed.", ex);
            }
        }

        public void UpdateItems(IDictionary<string, object> items)
        {
            try
            {
                if (_saveDb)
                {
                    UpdateDb(items);
                }
            }
            catch (Exception ex)
            {
                Log.Print($"{Name}: UpdateItem", ex);
            }
            OnScrapCompleted(true);
        }

        public override string ToString()
        {
            return URL;
        }
    }
}
