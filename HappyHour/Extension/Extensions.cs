﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyHour.Extension
{
    static class Extensions
    {
        public static int FindItem<TItem, TKey>(this IList<TItem> collection,
            TItem itemToAdd, Func<TItem, TKey> keyGetter)
        {
            return collection.BinarySearch(
                keyGetter(itemToAdd), Comparer<TKey>.Default, keyGetter);
        }
        /// <summary>
        /// Inserts an item into a list in the correct place,
        /// based on the provided key and key comparer.
        /// Use like OrderBy(o => o.PropertyWithKey).
        /// </summary>
        public static void AddInOrder<TItem, TKey>(this IList<TItem> collection,
            TItem itemToAdd, Func<TItem, TKey> keyGetter, bool isAccending = false)
        {
            int index = collection.BinarySearch(
                keyGetter(itemToAdd),
                Comparer<TKey>.Default,
                keyGetter,
                isAccending);
            collection.Insert(index, itemToAdd);
        }

        /// <summary>
        /// Binary search.
        /// </summary>
        /// <returns>Index of item in collection.</returns> 
        /// <notes>This version tops out at approximately 25% faster 
        /// than the equivalent recursive version. This 25% speedup is for list
        /// lengths more of than 1000 items, with less performance advantage 
        /// for smaller lists.</notes>
        public static int BinarySearch<TItem, TKey>(this IList<TItem> collection, TKey keyToFind,
            IComparer<TKey> comparer, Func<TItem, TKey> keyGetter, bool isAccending = false)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            int lower = 0;
            int upper = collection.Count - 1;

            while (lower <= upper)
            {
                int middle = lower + (upper - lower) / 2;
                int res = comparer.Compare(keyToFind, keyGetter.Invoke(collection[middle]));
                if (res == 0)
                {
                    return middle;
                }
                //else if (res > 0)
                else if (isAccending ? (res <= 0) : (res >= 0))
                {
                    upper = middle - 1;
                }
                else
                {
                    lower = middle + 1;
                }
            }

            // If we cannot find the item, return the item below it, so the new item will be inserted next.
            return lower;
        }

        public static int IndexOf(this byte[] src, byte[] pattern, int maxSearch)
        {
            for (int i = 0; i < maxSearch; i++)
            {
                if (src[i] != pattern[0]) // compare only first byte
                    continue;

                // found a match on first byte, now try to match rest of the pattern
                for (int j = pattern.Length - 1; j >= 1; j--)
                {
                    if (src[i + j] != pattern[j]) break;
                    if (j == 1) return i;
                }
            }
            return -1;
        }

        public static bool IsNullOrEmpty<TItem>(this IList<TItem> list)
        {
            if (list == null || list.Count == 0)
                return true;
            else
                return false;
        }

        public static List<T> ToList<T>(this object obj)
        {
            if (obj is IList<object> list)
                return list.Cast<T>().ToList();

            return null;
        }
    }
}
