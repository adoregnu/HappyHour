using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyHour.Interfaces
{
    interface IDbView
    {
        string SearchText { set; get; }
        bool SelectPid(string pid);
    }
}
