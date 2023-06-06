using Scoresheet.Model;
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

namespace Scoresheet.Formatter
{
    /// <summary>
    /// Interaction logic for FormatterDialog.xaml
    /// </summary>
    public partial class FormatterDialog : Window
    {
        //FormatterVM FormatterVM;

        public FormatterDialog(Guideline guideline)
        {
            
            InitializeComponent();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //if (!FormatterVM.IsLoaded) DialogResult = false;
        }
    }
}
