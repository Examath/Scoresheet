using Examath.Core.Environment;
using Scoresheet.Formatter;
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
                    else break; // Close app
                }

                // Try Load data
                try
                {
                    // Load Scoresheet XML
                    Model.ScoresheetFile? scoresheet = await Examath.Core.Utils.XML.LoadAsync<Model.ScoresheetFile>(fileLocation);

                    // Check if null
                    if (scoresheet == null)
                    {
                        if (Messager.Out("Want to try again", "Scoresheet is null, an error may have occurred",
                            yesButtonText: "Try Again", isCancelButtonVisible: true) == System.Windows.Forms.DialogResult.Yes)
                            continue; // Restart
                        else
                            break; // Close app
                    }

                    // Check if not formatted
                    if (!scoresheet.IsFormatted)
                    {
                        System.Windows.Forms.DialogResult dialogResult = Messager.Out(scoresheet.ToString() ?? "null", $"New Scoresheet - Please Check Guideline:",
                            isCancelButtonVisible: true, noButtonText: "Try Again", yesButtonText: "Continue");

                        switch (dialogResult)
                        {                            
                            case System.Windows.Forms.DialogResult.Yes: // Guideline is checked
                                if (!scoresheet.IsFormatted)
                                {
                                    FormatterVM formatterVM = new(scoresheet);
                                    if (!formatterVM.IsLoaded) break; // If teams list is not loaded, close app

                                    FormatterDialog formatterDialog = new(scoresheet)
                                    {
                                        Owner = this,
                                        DataContext = formatterVM
                                    };
                                    formatterDialog.ShowDialog();
                                    if (!scoresheet.IsFormatted) break; // if formatting cancelled, close app
                                    else; // Load scoresheet into main window
                                }
                                break;
                            case System.Windows.Forms.DialogResult.No: // Try Again
                                continue;
                            default: // Close App
                                return;
                        }
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

