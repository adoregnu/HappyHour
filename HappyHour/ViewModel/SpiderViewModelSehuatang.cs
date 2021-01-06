using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using HappyHour.Spider;

namespace HappyHour.ViewModel
{
    partial class SpiderViewModel : Pane
    {
        public int NumPage
        {
            set
            {
                if (_selectedSpider is SpiderSehuatang ss)
                    ss.NumPage = value;
            }
            get
            {
                if (_selectedSpider is SpiderSehuatang ss)
                    return ss.NumPage;
                return 0;
            }
        }

        public List<string> Boards
        {
            get
            {
                if (_selectedSpider is SpiderSehuatang ss)
                {
                    return ss.Boards;
                }
                return null;
            }
        }
        public string SelectedBoard
        {
            get
            {
                if (_selectedSpider is SpiderSehuatang ss)
                    return ss.SelectedBoard;
                return "";
            }
            set
            {
                if (_selectedSpider is SpiderSehuatang ss)
                {
                    ss.SelectedBoard = value;
                }
            }
        }

        public bool StopOnExistingId { get; set; } = true;
    }
}
