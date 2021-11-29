using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;

using Microsoft.EntityFrameworkCore;

using HappyHour.Model;
using HappyHour.Spider;
using HappyHour.Extension;

namespace HappyHour.ScrapItems
{
    class ItemBase2
    {
        readonly AvDbContext _context;
        readonly SpiderBase _spider;

        protected AvItem _avItem;
        AvStudio _studio;
        AvSeries _series;
        List<AvActor> _actors = new();
        List<AvGenre> _genres;

        public ItemBase2(SpiderBase spider)
        {
            _spider = spider;
            _context = App.DbContext;
            _avItem = new AvItem
            {
                Pid = spider.Keyword,
                Path = spider.DataPath,
                IsCensored = true,
            };
        }

        void UpdateTitle(string title)
        {
            _avItem.Title = title;
        }

        void UpdatePlot(string plot)
        {
            _avItem.Plot = plot;
        }

        void UpdateRating(string rating)
        {
            try
            {
                _avItem.Rating = float.Parse(rating);
            }
            catch (Exception ex)
            {
                Log.Print($"ItemBase2::UpdateRating: {ex.Message}");
            }
        }

        protected virtual void UpdateDate(string date)
        {
            try
            {
                string[] patterns = new string[] {
                    "yyyy-MM-dd", "yyyy/MM/dd", "yyyyy.MM.dd", "MMM d yyyy"
                };
                foreach (string pattern in patterns)
                {
                    try
                    {
                        _avItem.DateReleased = DateTime.ParseExact(
                            date, pattern, App.enUS);
                        break;
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Log.Print($"ItemBase2::UpdateDate : {ex.Message}");
            }
        }

        void UpdateGenre(List<object> genres)
        {
            _genres = new List<AvGenre>();
            foreach (string genre in genres)
            {
                AvGenre entity = _context.Genres.FirstOrDefault(
                    x => x.Name.ToLower() == genre.ToLower());
                if (entity == null)
                {
                    entity = _context.Genres.Add(new AvGenre { Name = genre }).Entity;
                }
                _genres.Add(entity);
            }
        }

        void UpdateStudio(string studio)
        {
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
        }

        void UpdateSeries(string series)
        {
             _series = _context.Series.FirstOrDefault(x => x.Name == series);
            if (_series == null)
            {
                _series = _context.Series.Add(new AvSeries { Name = series }).Entity;
            }
        }

        void UpdateActor(List<object> actors)
        {
            foreach (IDictionary<string, object> actor in actors)
            {
                AvActorName dbName = null;
                actor.ForEach(val => {
                    dbName = _context.ActorNames
                        .Include(n => n.Actor)
                        .Where(n => n.Name== val)
                        .Where(n => n.Actor != null)
                        .FirstOrDefault();
                    return dbName != null;
                });

                AvActor dbActor;
                if (dbName != null)
                {
                    dbActor = dbName.Actor;
                    actor.ForEach(name => {
                        if (!dbActor.Names.Any(n => n.Name == name))
                        {
                            dbActor.Names.Add(new AvActorName {
                                Name = name, Actor = dbActor });
                        }
                        return false;
                    });
                }
                else
                {
                    dbActor = new();
                    List<AvActorName> list = new();

                    actor.ForEach(name => {
                        list.Add(new AvActorName { Name = name, Actor = dbActor });
                        return false;
                    });

                    dbActor.Names = list;
                    dbActor.DateAdded = DateTime.Now;

                    _context.Actors.Add(dbActor);
                }
                if (actor.ContainsKey("thumb"))
                {
                    dbActor.PicturePath = actor["thumb"].ToString();
                    _actors.Add(dbActor);
                }
             }
        }

        public void UpdateItems(IDictionary<string, object> items)
        {
            var updater = new Dictionary<string, Delegate>
            {
                { "title",  (Action<string>)UpdateTitle },
                { "genre", (Action<List<object>>)UpdateGenre },
                { "studio",  (Action<string>)UpdateStudio },
                { "series",  (Action<string>)UpdateSeries },
                { "actor", (Action<List<object>>)UpdateActor},
                { "date",  (Action<string>)UpdateDate },
                { "plot",  (Action<string>)UpdatePlot },
                { "rating",  (Action<string>)UpdateRating },

                //{ "cover",  (Action<string>)UpdateCover },
            };
 
            foreach (var item in items)
            {
                Log.Print($"{item.Key} : {item.Value?.ToString()}");

                if (updater.ContainsKey(item.Key) && item.Value != null)
                {
                    updater[item.Key].DynamicInvoke(item.Value);
                }
            }

            UpdateDb();
        }

        void UpdateDb()
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
                    _context.Items.Add(_avItem);
                }
                _context.SaveChanges();
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
        }
    }
}
