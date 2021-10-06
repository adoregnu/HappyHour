using CefSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyHour.ScrapItems
{
    using ElementTupleList = List<(string name, string element, ElementType type)>;

    enum ElementType
    {
        XPATH, XPATH_LAZY, XPATH_CLICK, CSS
    }

    interface IScrapItem
    {
        ElementTupleList Elements { get; set; }
        void OnJsResult(string name, List<object> items);
    }
}
