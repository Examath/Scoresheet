using CommunityToolkit.Mvvm.Messaging;
using DocumentFormat.OpenXml.Presentation;
using Examath.Core.Utils;
using Scoresheet.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
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
        FormatterVM? FormatterVM;
        public bool IsSynchronised { get; private set; } = false;

        /// <summary>
        /// If the scoresheet was formatted using the Create button, this is set to the location of the new formatted scoresheet
        /// </summary>
        public string? FormattedScoresheetFileLocation { get; private set; }

        /// <summary>
        /// Initialises a new <see cref="FormatterDialog"/> window
        /// </summary>
        public FormatterDialog()
        {
            InitializeComponent();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            FormatterVM = (FormatterVM)DataContext;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCsvOrClose();
        }

        private readonly Examath.Core.Model.FileFilter _TsvFilter = new("Tab Separated Values", "*.tsv");

        public async void LoadCsvOrClose()
        {
            OpenFileDialog openFileDialog = new()
            {
                Title = "Open submissions from Google forms",
                Filter = _TsvFilter.ToString(),
            };

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK && FormatterVM != null)
            {
                bool result = await Task.Run(() => { return FormatterVM.ImportData(openFileDialog.FileName); });
                if (result)
                {
                    // Biding DataGrid to string array from https://stackoverflow.com/a/5582677
                    RawDataGrid.AutoGenerateColumns = false;
                    for (int i = 0; i < FormatterVM.DataColumns.Count; i++)
                    {
                        var binding = new System.Windows.Data.Binding("[" + i + "]");
                        var col = new DataGridTextColumn();
                        col.Binding = binding;
                        col.Header = FormatterVM.DataColumns[i].Header;
                        RawDataGrid.Columns.Add(col);
                    }
                    RawDataGrid.ItemsSource = FormatterVM.Data;
                    IsEnabled = true;
                }
                else
                {
                    Close();
                }
            }
            else
            {
                Close();
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RootTabControl.SelectedItem == FixTab &&
                !IsSynchronised)
            {
                Synchronise();
            }
        }

        private void SynchroniseButton_Click(object sender, RoutedEventArgs e)
        {
            RootTabControl.SelectedItem = FixTab;
        }

        private async void Synchronise()
        {
            if (FormatterVM != null)
            {
                IsEnabled = false;
                await FormatterVM.Synchronise();
                if (FormatterVM.FormSubmissions.Count > 0)
                {                    
                    IsSynchronised = true;
                    SynchroniseOptions.IsEnabled = false;
                    RawDataGrid.IsReadOnly = true;
                }
                else
                {
                    RootTabControl.SelectedItem = ImportTab;
                }
                IsEnabled = true;
            }
        }
    }
}
