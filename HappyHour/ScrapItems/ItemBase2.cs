using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;

using Microsoft.EntityFrameworkCore;

using HappyHour.Model;
using HappyHour.Interfaces;

namespace HappyHour.ScrapItems
{
    internal class ItemBase2
    {
        private readonly AvDbContext _context;
        protected AvItem _avInfo;

        public bool OverwriteActorPicture { get; set; }

        public ItemBase2(IAvMedia avm)
        {
            _context = App.DbContext;
            _avInfo = _context.Items
                .Include("Studio")
                .Include("Actors")
                .Include("Genres")
                .Include("Series")
                .FirstOrDefault(av => av.Pid == avm.Pid);

            if (_avInfo == null)
            {
                _avInfo = new AvItem
                {
                    Pid = avm.Pid,
                    Path = avm.Path,
                    IsCensored = true,
                    Genres = new List<AvGenre>(),
                    Actors = new List<AvActor>(),
                    DateAdded = DateTime.Now,
                    DateModifed = DateTime.Now,
                };
                _avInfo = _context.Items.Add(_avInfo).Entity;
            }
        }

        private void UpdateTitle(string title)
        {
            _avInfo.Title = title;
        }

        private void UpdatePlot(string plot)
        {
            _avInfo.Plot = plot;
        }

        private void UpdateRating(string rating)
        {
            try
            {
                _avInfo.Rating = float.Parse(rating, App.enUS);
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
                    "yyyy-MM-dd", "yyyy/MM/dd", "yyyy.MM.dd", "MMM d yyyy"
                };
                foreach (string pattern in patterns)
                {
                    try
                    {
                        _avInfo.DateReleased = DateTime.ParseExact(
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

        private void UpdateGenre(List<object> genres)
        {
            foreach (string genre in genres)
            {
                if (NameMap.SkipGenre(genre))
                {
                    continue;
                }
                var entity = _context.Genres.FirstOrDefault(
                    x => x.Name.ToLower() == genre.ToLower());
                if (entity == null)
                {
                    entity = _context.Genres.Add(new AvGenre { Name = genre }).Entity;
                }
                if (!_avInfo.Genres.Any(g => g.Name.ToLower() == entity.Name.ToLower()))
                {
                    _avInfo.Genres.Add(entity);
                }
            }
        }

        private void UpdateStudio(string studio)
        {
            studio = NameMap.StudioName(studio);
            var entity = _context.Studios.FirstOrDefault(x => x.Name.ToLower() == studio.ToLower());
            if (entity == null)
            {
                entity = _context.Studios.Add(new AvStudio { Name = studio }).Entity;
            }
            _avInfo.Studio = entity;
        }

        private void UpdateSeries(string series)
        {
            var entity = _context.Series.FirstOrDefault(
                 x => x.Name.ToLower() == series.ToLower());
            if (entity == null)
            {
                entity = _context.Series.Add(new AvSeries { Name = series }).Entity;
            }
            _avInfo.Series = entity;
        }

        /// <summary>
        /// dic { name : string, thumb: string, alias: List<object> }
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="action"></param>
        private static void ForEachActor(IDictionary<string, object> dict,
            Func<string, bool> action)
        {
            foreach (var item in dict)
            {
                if (item.Value == null)
                {
                    continue;
                }
                if (item.Key == "alias" && item.Value is List<object> alias)
                {
                    bool exitLoop = false;
                    foreach (string a in alias)
                    {
                        if (action(a.ToString()))
                        {
                            exitLoop = true;
                            break;
                        }
                    }
                    if (exitLoop)
                    {
                        break;
                    }
                }
                else if (item.Key == "name")
                {
                    if (action(item.Value.ToString()))
                    {
                        break;
                    }
                }
            }
        }

        private void UpdateActor(List<object> actors)
        {
            foreach (IDictionary<string, object> actor in actors)
            {
                AvActorName dbName = null;
                ForEachActor(actor, val =>
                {
                    dbName = _context.ActorNames
                        .Include(n => n.Actor).ThenInclude(a => a.Names)
                        .Where(n => n.Name.ToLower() == val.ToLower())
                        .Where(n => n.Actor != null)
                        .FirstOrDefault();
                    return dbName != null;
                });

                AvActor dbActor;
                if (dbName != null)
                {
                    dbActor = dbName.Actor;
                    ForEachActor(actor, name =>
                    {
                        if (!dbActor.Names.Any(n => n.Name.ToLower() == name.ToLower()))
                        {
                            dbActor.Names.Add(new AvActorName { Name = name, Actor = dbActor });
                        }
                        return false;
                    });
                }
                else
                {
                    dbActor = new();
                    List<AvActorName> ActorNames = new();
                    ForEachActor(actor, name =>
                    {
                        ActorNames.Add(new AvActorName { Name = name, Actor = dbActor });
                        return false;
                    });

                    dbActor.Names = ActorNames;
                    dbActor.DateAdded = DateTime.Now;
                    dbActor = _context.Actors.Add(dbActor).Entity;
                }
                if (_avInfo.Actors.Count == 0 ||
                    !_avInfo.Actors.Any(a => a.Id != 0 && a.Id == dbActor.Id))
                {
                    _avInfo.Actors.Add(dbActor);
                }
                //string update = App.GetConf("general", "always_update_actor_thumb_of_db") ?? "false";
                if (actor.ContainsKey("thumb"))
                {
                    if (string.IsNullOrEmpty(dbActor.PicturePath) || OverwriteActorPicture)
                    {
                        dbActor.PicturePath = actor["thumb"].ToString();
                    }
                }
            }
        }

        public void UpdateItems(IDictionary<string, object> items)
        {
            Dictionary<string, Delegate> updater = new()
            {
                { "title", (Action<string>)UpdateTitle },
                { "genre", (Action<List<object>>)UpdateGenre },
                { "studio", (Action<string>)UpdateStudio },
                { "series", (Action<string>)UpdateSeries },
                { "actor", (Action<List<object>>)UpdateActor },
                { "date", (Action<string>)UpdateDate },
                { "plot", (Action<string>)UpdatePlot },
                { "rating", (Action<string>)UpdateRating },

                //{ "cover",  (Action<string>)UpdateCover },
            };

            foreach (var item in items)
            {
                //Log.Print($"{item.Key} : {item.Value?.ToString()}");
                if (updater.ContainsKey(item.Key) && item.Value != null)
                {
                    updater[item.Key].DynamicInvoke(item.Value);
                }
            }

            _ = _context.SaveChanges();
        }
    }
}
