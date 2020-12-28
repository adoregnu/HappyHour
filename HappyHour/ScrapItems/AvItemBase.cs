using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

using Microsoft.EntityFrameworkCore;

using CefSharp;
using HtmlAgilityPack;

using HappyHour.Model;
using HappyHour.Spider;

namespace HappyHour.ScrapItems
{
    class AvItemBase : ItemBase
    {
        readonly protected AvDbContext _context;

        protected AvItem _avItem;
        protected List<Tuple<string, string>> _links
            = new List<Tuple<string, string>>();

        protected string PosterPath
        {
            get => $"{_spider.Media.MediaFolder}\\{_spider.Media.Pid}_poster";
        }

        public bool OverwriteImage { set; get; } = false;

        public AvItemBase(SpiderBase spider) : base(spider)
        { 
            _avItem = new AvItem
            {
                Pid = spider.Media.Pid,
                Path = spider.Media.MediaFolder,
                IsCensored = true,
            };
            _context = App.DbContext;

            if (bool.TryParse(App.GConf["spider"]["overwrite_image"], out bool bOverwrite))
                OverwriteImage = bOverwrite;
        }

        protected override void OnDownloadUpdated(object sender, DownloadItem e)
        {
            if (e.IsComplete)
            {
                Log.Print($"{_spider.Media.Pid} download completed: {e.FullPath}");
                CheckCompleted();
            };
        }

        protected void CheckCompleted()
        {
            Interlocked.Increment(ref _numScrapedItem);
            if (_numScrapedItem != NumItemsToScrap)
                return;

            Log.Print($"items : {_numScrapedItem}/{NumItemsToScrap}" +
                $", Link Count:{_links.Count}");

            if (_numScrapedItem != NumItemsToScrap)
                return;

            lock (_links)
            {
                Log.Print($"Num valid items: {_numValidItems}");
                if (_links.Count > 0)
                {
                    var link = _links[0];
                    _links.RemoveAt(0);
                    _spider.Navigate(link.Item1, link.Item2);
                }
                else
                {
                    if (_numValidItems > 0)
                        UdpateAvItem();
                    _spider.OnScrapCompleted(_numValidItems > 0);
                    Clear();
                }
            }
        }

        protected void UpdateTitle(string title)
        {
            title = HtmlEntity.DeEntitize(title.Trim());
            _avItem.Title = title;
        }

        List<AvGenre> _genres;
        protected void UpdateGenre(List<object> items)
        {
            _genres = new List<AvGenre>();
            foreach (string item in items)
            {
                if (string.IsNullOrEmpty(item)) continue;

                var genre = HtmlEntity.DeEntitize(item.Trim());
                UiServices.Invoke(delegate
                {
                    var entity = _context.Genres.FirstOrDefault(
                        x => x.Name.ToLower() == genre.ToLower());
                    if (entity == null)
                        entity = _context.Genres.Add(new AvGenre { Name = genre }).Entity;
                    _genres.Add(entity);
                });
            }
        }

        AvStudio _studio;
        protected void UpdateStudio(string studio)
        {
            if (string.IsNullOrEmpty(studio)) return;

            UiServices.Invoke(delegate
            {
                studio = HtmlEntity.DeEntitize(studio.Trim());
                studio = NameMap.StudioName(studio);
                _studio = _context.Studios.FirstOrDefault(
                    x => x.Name.ToLower() == studio.ToLower());
                if (_studio == null)
                {
                    _studio = _context.Studios.Add(new AvStudio { Name = studio }).Entity;
                }
            });
        }

        AvSeries _series;
        protected void UpdateSeries(string series)
        {
            if (string.IsNullOrEmpty(series)) return;
            UiServices.Invoke(delegate
            {
                var seires = HtmlEntity.DeEntitize(series);
                _series = _context.Series.FirstOrDefault(x => x.Name == series);
                if (_series == null)
                    _series = _context.Series.Add(new AvSeries { Name = series }).Entity;
            });
        }

        List<AvActor> _actors;
        void UpdateActorInternal(List<List<AvActorName>> listOfNameList)
        {
            foreach (var list in listOfNameList)
            {
                AvActorName aan = null;
                foreach (var aname in list)
                {
                    aan = _context.ActorNames
                        .Include("Actor")
                        .Where(n => n.Name.ToLower() == aname.Name.ToLower())
                        .FirstOrDefault();
                    if (aan != null) break;
                }

                if (aan != null)
                {
                    var dbActor = aan.Actor;
                    foreach (var aname in list)
                    {
                        if (!dbActor.Names
                            .Any(n => n.Name.ToLower() == aname.Name.ToLower()))
                        {
                            dbActor.Names.Add(aname);
                        }
                        aname.Actor = dbActor;
                    }
                    _actors.Add(dbActor);
                }
                else
                {
                    var actor = new AvActor();
                    list.ForEach(i => i.Actor = actor);
                    actor.Names = list;

                    _context.Actors.Add(actor);
                    _actors.Add(actor);
                }
            }
        }

        protected void UpdateActor2(List<List<AvActorName>> listOfNameList)
        {
            _actors = new List<AvActor>();
            UiServices.Invoke(delegate
            {
                UpdateActorInternal(listOfNameList);
            });
        }

        protected virtual void UdpateAvItem()
        {
            UiServices.Invoke(delegate
            {
                var item = _context.Items
                    .Include("Studio")
                    .Include("Actors")
                    .Include("Genres")
                    .Include("Series")
                    .FirstOrDefault(i => i.Pid == _avItem.Pid);

                if (item != null) _avItem = item;

                if (item == null || (_series != null && item.Series == null))
                    _avItem.Series = _series;
                if (item == null || (_studio != null && item.Studio == null))
                    _avItem.Studio = _studio;
                if (item == null || (_actors != null && item.Actors.Count == 0))
                    _avItem.Actors = _actors;
                if (item == null || (_genres != null && item.Genres.Count == 0))
                    _avItem.Genres = _genres;

                try
                {
                    if (item == null)
                        _context.Items.Add(_avItem);
                    _context.SaveChanges();
                }
                catch (ValidationException e)
                {
                    Log.Print(e.Message);
#if false
                    foreach (var eve in e.EntityValidationErrors)
                    {
                        Log.Print("Entity of type \"{0}\" in state \"{1}\"" +
                            " has the following validation errors:",
                            eve.Entry.Entity.GetType().Name, eve.Entry.State);
                        foreach (var ve in eve.ValidationErrors)
                        {
                            Log.Print("- Property: \"{0}\", Error: \"{1}\"",
                                ve.PropertyName, ve.ErrorMessage);
                        }
                    }
#endif
                }
                finally
                {
                    _series = null;
                    _studio = null;
                    _actors = null;
                    _genres = null;
                }
            });
        }
    }
}
