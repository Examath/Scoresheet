using CommunityToolkit.Mvvm.Messaging;
using Examath.Core.Utils;
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
        FormatterVM? FormatterVM;

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
            if (FormatterVM.ScoresheetFile.IsScoring) CreateButton.IsEnabled = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //if (!FormatterVM.IsLoaded) DialogResult = false;
        }

        private async void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            CreateButton.IsEnabled = false;
            if (FormatterVM == null) return;

            System.Windows.Forms.SaveFileDialog saveFileDialog = new()
            {
                Title = "Choose a location to save formatted scoresheet. Do not replace the original.",
                Filter = "Scoresheet XML File|*.ssf",
            };
            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FormattedScoresheetFileLocation = saveFileDialog.FileName;
                FormatterVM.ScoresheetFile.IsScoring = true;
                FormatterVM.ScoresheetFile.LastSavedTime = DateTime.Now;
                FormatterVM.ScoresheetFile.LastAuthor = "Formatter";
                try
                {
                    await XML.SaveAsync(FormattedScoresheetFileLocation, FormatterVM.ScoresheetFile);
                }
                catch (Exception ee)
                {
                    Examath.Core.Environment.Messager.OutException(ee, "Exporting scoresheet");
                    CreateButton.IsEnabled = true;
                    return;
                }
                DialogResult = true;
            }
            else
            {
                CreateButton.IsEnabled = true;
            }
        }
    }
}
