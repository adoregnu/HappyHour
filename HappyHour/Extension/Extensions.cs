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
        public static void InsertInPlace<TItem, TKey>(
            this ObservableCollection<TItem> collection,
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
        public static int BinarySearch<TItem, TKey>(
            this IList<TItem> collection,
            TKey keyToFind,
            IComparer<TKey> comparer,
            Func<TItem, TKey> keyGetter,
            bool isAccending = false)
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
                int comparisonResult = comparer.Compare(keyToFind,
                    keyGetter.Invoke(collection[middle]));
                if (comparisonResult == 0)
                {
                    return middle;
                }
                //else if (comparisonResult > 0)
                else if (isAccending ? (comparisonResult <= 0) : (comparisonResult >= 0))
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
