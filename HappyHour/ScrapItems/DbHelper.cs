using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;

using Microsoft.EntityFrameworkCore;

using HappyHour.Model;
using HappyHour.Interfaces;

namespace HappyHour.ScrapItems
{
    internal static class DbHelper
    {
        //private readonly AvDbContext _context;
        //protected AvItem _avInfo;

        public static bool OverwriteActorPicture { get; set; }

        public static AvItem GetAvItem(IAvMedia avm)
        {
            var _avInfo = App.DbContext.Items
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
                _avInfo = App.DbContext.Items.Add(_avInfo).Entity;
            }
            return _avInfo;
        }

        public static void UpdateRating(AvItem _avInfo, string rating)
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

        public static void UpdateDate(AvItem _avInfo,string date)
        {
            string[] patterns = new string[]
            {
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

        public static void UpdateGenre(AvItem _avInfo, List<object> genres)
        {
            foreach (string genre in genres.Cast<string>())
            {
                if (NameMap.SkipGenre(genre))
                {
                    continue;
                }
                var entity = App.DbContext.Genres.FirstOrDefault(
                    x => x.Name.ToLower() == genre.ToLower());
                if (entity == null)
                {
                    entity = App.DbContext.Genres.Add(new AvGenre { Name = genre }).Entity;
                }
                if (!_avInfo.Genres.Any(g => g.Name.ToLower() == entity.Name.ToLower()))
                {
                    _avInfo.Genres.Add(entity);
                }
            }
        }

        public static void UpdateStudio(AvItem _avInfo, string studio)
        {
            studio = NameMap.StudioName(studio);
            var entity = App.DbContext.Studios.FirstOrDefault(x => x.Name.ToLower() == studio.ToLower());
            if (entity == null)
            {
                entity = App.DbContext.Studios.Add(new AvStudio { Name = studio }).Entity;
            }
            _avInfo.Studio = entity;
        }

        public static void UpdateSeries(AvItem _avInfo, string series)
        {
            var entity = App.DbContext.Series.FirstOrDefault(
                 x => x.Name.ToLower() == series.ToLower());
            if (entity == null)
            {
                entity = App.DbContext.Series.Add(new AvSeries { Name = series }).Entity;
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
                    foreach (string a in alias.Cast<string>())
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

        public static void UpdateActor(AvItem _avInfo, List<object> actors)
        {
            foreach (var actor in actors.Cast<IDictionary<string, object>>())
            {
                AvActorName dbName = null;
                ForEachActor(actor, val =>
                {
                    dbName = App.DbContext.ActorNames
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
                    dbActor = App.DbContext.Actors.Add(dbActor).Entity;
                }
                if (_avInfo.Actors.Count == 0 ||
                    !_avInfo.Actors.Any(a => a.Id != 0 && a.Id == dbActor.Id))
                {
                    _avInfo.Actors.Add(dbActor);
                }
                if (actor.ContainsKey("thumb"))
                {
                    if (string.IsNullOrEmpty(dbActor.PicturePath) || OverwriteActorPicture)
                    {
                        dbActor.PicturePath = actor["thumb"].ToString();
                    }
                }
            }
        }

        public static void UpdateItems(IAvMedia avm, IDictionary<string, object> items)
        {
            AvItem avitem = GetAvItem(avm);
            Dictionary<string, Action<AvItem, object>> updater = new()
            {
                { "title", (_item, title) => _item.Title = title as string },
                { "genre", (_item, list) => UpdateGenre(_item, list as List<object>) }, //(Action<List<object>>)UpdateGenre },
                { "studio", (_item, studio) => UpdateStudio(_item, studio as string) }, //(Action<string>)UpdateStudio },
                { "series", (_item, series) => UpdateSeries(_item, series as string) }, //(Action<string>)UpdateSeries },
                { "actor", (_item, list) => UpdateActor(_item, list as List<object>) }, //(Action<List<object>>)UpdateActor },
                { "date", (_item, date) => UpdateDate(_item, date as string) }, //(Action<string>)UpdateDate },
                { "plot", (_item, plot) => _item.Plot = plot as string}, //(Action<string>)UpdatePlot },
                { "rating", (_item, rating) => UpdateRating(_item, rating as string) } //(Action<string>)UpdateRating },

                //{ "cover",  (Action<string>)UpdateCover },
            };

            foreach (var item in items)
            {
                //Log.Print($"{item.Key} : {item.Value?.ToString()}");
                if (updater.ContainsKey(item.Key) && item.Value != null)
                {
                    //updater[item.Key].DynamicInvoke(item.Value);
                    updater[item.Key](avitem, item.Value);
                }
            }

            _ = App.DbContext.SaveChanges();
        }
    }
}
