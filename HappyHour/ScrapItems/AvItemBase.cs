using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.ComponentModel.DataAnnotations;

using Microsoft.EntityFrameworkCore;

using CefSharp;
using HtmlAgilityPack;

using HappyHour.Model;
using HappyHour.Spider;

namespace HappyHour.ScrapItems
{
    abstract class AvItemBase : ItemBase
    {
        protected readonly AvDbContext _context;

        protected AvItem _avItem;
        protected List<Tuple<string, string>> _links = new ();
        protected string PosterPath => $"{_spider.DataPath}\\{_spider.Keyword}_poster";

        public bool OverwriteImage { set; get; }

        public AvItemBase(SpiderBase spider) : base(spider)
        {
            _avItem = new AvItem
            {
                Pid = spider.Keyword,
                Path = spider.DataPath,
                IsCensored = true,
            };
            if (spider.SaveDb)
            {
                _context = App.DbContext;
            }
            if (bool.TryParse(App.GConf["spider"]["overwrite_image"], out bool bOverwrite))
            {
                OverwriteImage = bOverwrite;
            }
        }

        protected override void OnDownloadUpdated(object sender, DownloadItem e)
        {
            if (e.IsComplete)
            {
                Log.Print($"{_spider.Keyword} download completed: {e.FullPath}");
                CheckCompleted();
            };
        }

        protected void CheckCompleted()
        {
            _ = Interlocked.Increment(ref _numScrapedItem);
            if (_numScrapedItem != NumItemsToScrap)
            {
                return;
            }

            //Log.Print($"items : {_numScrapedItem}/{NumItemsToScrap}, Link Count:{_links.Count}");
            lock (_links)
            {
                Log.Print($"Num valid items: {_numValidItems}");
                if (_links.Count > 0)
                {
                    Tuple<string, string> link = _links[0];
                    _links.RemoveAt(0);
                    _spider.Navigate(link.Item1, link.Item2);
                }
                else
                {
                    if (_numValidItems > 0)
                    {
                        UdpateAvItem();
                    }
                    Clear();
                }
            }
        }

        protected void UpdateTitle(string title)
        {
            title = HtmlEntity.DeEntitize(title.Trim());
            _avItem.Title = title;
        }

        private List<AvGenre> _genres;
        protected void UpdateGenre(List<object> items)
        {
            if (_context == null)
            {
                return;
            }

            _genres = new List<AvGenre>();
            foreach (string item in items)
            {
                if (string.IsNullOrEmpty(item))
                {
                    continue;
                }

                string genre = HtmlEntity.DeEntitize(item.Trim());
                UiServices.Invoke(delegate
                {
                    AvGenre entity = _context.Genres.FirstOrDefault(
                        x => x.Name.ToLower() == genre.ToLower());
                    if (entity == null)
                    {
                        entity = _context.Genres.Add(new AvGenre { Name = genre }).Entity;
                    }
                    _genres.Add(entity);
                });
            }
        }

        private AvStudio _studio;
        protected void UpdateStudio(string studio)
        {
            if (_context == null)
            {
                return;
            }
            if (string.IsNullOrEmpty(studio))
            {
                return;
            }

            UiServices.Invoke(delegate
            {
                studio = HtmlEntity.DeEntitize(studio.Trim());
                if (_spider.Name == "Javlibrary")
                {
                    studio = NameMap.StudioName(studio);
                }
                _studio = _context.Studios.FirstOrDefault(
                    x => x.Name.ToLower() == studio.ToLower());
                if (_studio == null)
                {
                    _studio = _context.Studios.Add(new AvStudio { Name = studio }).Entity;
                }
            });
        }

        private AvSeries _series;
        protected void UpdateSeries(string series)
        {
            if (_context == null)
            {
                return;
            }
            if (string.IsNullOrEmpty(series))
            {
                return;
            }
            UiServices.Invoke(delegate
            {
                string seires = HtmlEntity.DeEntitize(series);
                _series = _context.Series.FirstOrDefault(x => x.Name == series);
                if (_series == null)
                {
                    _series = _context.Series.Add(new AvSeries { Name = series }).Entity;
                }
            });
        }

        private List<AvActor> _actors = new List<AvActor>();
        private void UpdateActorInternal(List<List<AvActorName>> listOfNameList)
        {
            foreach (List<AvActorName> list in listOfNameList)
            {
                AvActorName aan = null;
                foreach (AvActorName aname in list)
                {
                    aan = _context.ActorNames
                        .Include(n => n.Actor)
                        .Where(n => n.Name.ToLower() == aname.Name.ToLower())
                        .Where(n => n.Actor != null)
                        .FirstOrDefault();
                    if (aan != null)
                    {
                        break;
                    }
                }

                if (aan != null)
                {
                    AvActor dbActor = aan.Actor;
                    foreach (AvActorName aname in list)
                    {
                        if (!dbActor.Names.Any(n => n.Name.ToLower() == aname.Name.ToLower()))
                        {
                            dbActor.Names.Add(aname);
                        }
                        aname.Actor = dbActor;
                    }
                    _actors.Add(dbActor);
                }
                else
                {
                    AvActor actor = new();
                    list.ForEach(i => i.Actor = actor);
                    actor.Names = list;
                    actor.DateAdded = DateTime.Now;

                    _context.Actors.Add(actor);
                    _actors.Add(actor);
                }
            }
        }

        protected void UpdateActor2(List<List<AvActorName>> listOfNameList)
        {
            if (_context == null)
            {
                return;
            }
            UiServices.Invoke(() => UpdateActorInternal(listOfNameList));
        }

        protected virtual void UdpateAvItem()
        {
            if (_context == null)
            {
                return;
            }

            UiServices.Invoke(delegate
            {
                AvItem item = _context.Items
                    .Include("Studio")
                    .Include("Actors")
                    .Include("Genres")
                    .Include("Series")
                    .FirstOrDefault(i => i.Pid == _avItem.Pid);

                if (item != null)
                {
                    _avItem = item;
                    _avItem.DateModifed = DateTime.Now;
                }
                else
                {
                    _avItem.DateAdded = _avItem.DateModifed = DateTime.Now;
                }

                if (item == null || (_series != null && item.Series == null))
                {
                    _avItem.Series = _series;
                }
                if (item == null || (_studio != null && item.Studio == null))
                {
                    _avItem.Studio = _studio;
                }
                if (item == null || (_actors != null && item.Actors.Count == 0))
                {
                    _avItem.Actors = _actors;
                }
                if (item == null || (_genres != null && item.Genres.Count == 0))
                {
                    _avItem.Genres = _genres;
                }

                try
                {
                    if (item == null)
                    {
                        _ = _context.Items.Add(_avItem);
                    }
                    _ = _context.SaveChanges();
                }
                catch (ValidationException e)
                {
                    Log.Print(e.Message);
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
