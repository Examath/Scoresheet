using Examath.Core.Environment;
using Scoresheet.Formatter;
using Scoresheet.Model;
using System;
using System.Windows;

namespace Scoresheet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadDataAsync();
        }

        public async void LoadDataAsync()
        {
            bool isLoaded = false;
            bool isLoadingFromAppSelect = false;
            string fileLocation = string.Empty;

            if (((App)Application.Current).Args?.Length >= 1) // File selected
            {
                fileLocation = ((App)Application.Current).Args[0];
                isLoadingFromAppSelect = true;
            }

            while (!isLoaded)
            {
                // Pick file location
                if (isLoadingFromAppSelect)
                {
                    isLoadingFromAppSelect = false; // So that runs only once
                }
                else
                {
                    System.Windows.Forms.OpenFileDialog openFileDialog = new()
                    {
                        Title = "Open Scoresheet file",
                        Filter = "Scoresheet (.ssf)|*.ssf|All|*.*",
                    };

                    if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) fileLocation = openFileDialog.FileName;
                    else break;
                }

                // Try Load data
                try
                {
                    Model.ScoresheetFile? guideline = await Examath.Core.Utils.XML.LoadAsync<Model.ScoresheetFile>(fileLocation);

                    if (guideline == null)
                    {
                        if (Messager.Out("Want to try again", "Scoresheet is null, an error may have occurred",
                            yesButtonText: "Try Again", isCancelButtonVisible: true) == System.Windows.Forms.DialogResult.Yes)
                            continue;
                        else
                            break;
                    }

                    System.Windows.Forms.DialogResult dialogResult = Messager.Out(guideline.ToString() ?? "null", $"Check Guideline",
                        isCancelButtonVisible: true, noButtonText: "Try Again", yesButtonText: "Continue");

                    switch (dialogResult)
                    {
                        case System.Windows.Forms.DialogResult.Yes:
                            if (!guideline.IsFormatted)
                            {
                                FormatterVM formatterVM = new(guideline);
                                if (!formatterVM.IsLoaded) break;
                                FormatterDialog formatterDialog = new(guideline)
                                {
                                    Owner = this,
                                    DataContext = formatterVM
                                };
                                formatterDialog.ShowDialog();
                                if (!guideline.IsFormatted) break;
                            }

                            break;
                        case System.Windows.Forms.DialogResult.No:
                            continue;
                        default:
                            return;
                    }

                }
                catch (Exception e)
                {
                    if (Messager.OutException(e, yesButtonText: "Try Again", isCancelButtonVisible: true) == System.Windows.Forms.DialogResult.Yes)
                        continue;
                    else
                        break;
                }
            }

            if (!isLoaded) Close();
        }

    }
}

