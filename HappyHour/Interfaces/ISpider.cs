using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyHour.Interfaces
{
    public delegate void ScrapCompletedHandler(object sender, object userData);

    interface ISpider
    {
        ScrapCompletedHandler ScrapCompleted { get; set; }
        void Scrap(string kw, string path, object userData);
    }
}
