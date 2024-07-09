using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace Scoresheet.Exporters
{
    /// <summary>
    /// Interaction logic for ExporterWindow.xaml
    /// </summary>
    public partial class ExporterWindow : Window
    {
        private Exporter _Exporter; 

        public ExporterWindow(Exporter exporter)
        {
            _Exporter = exporter;
            if (_Exporter.Initialise())
            {
                _Exporter.Exported += _Exporter_Exported;
                InitializeComponent();
            }
            else
            {
                Close();
            }            
        }

        protected override void OnActivated(EventArgs e)
        {
            // Font Combo Box Items


            // Paper Size Combo Box Items
            


            DataContext = _Exporter;
            base.OnActivated(e);
        }

        private void _Exporter_Exported(object? sender, EventArgs e)
        {
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            DocumentReader.Document = null;
            base.OnClosing(e);
        }
    }
}
