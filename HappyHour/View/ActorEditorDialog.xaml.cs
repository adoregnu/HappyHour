using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace HappyHour.View
{
    /// <summary>
    /// Interaction logic for ActorEditorDialog.xaml
    /// </summary>
    public partial class ActorEditorDialog : Window
    {
        public ActorEditorDialog()
        {
            InitializeComponent();
        }
        void Close(object sender, EventArgs e)
        {
            Close();
        }
    }
}
