using Examath.Core.Environment;
using Examath.Core.Utils;
using Scoresheet.Formatter;
using Scoresheet.Model;
using System;
using System.ComponentModel;
using System.Windows;

namespace Scoresheet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private VM? VM;

        public MainWindow()
        {
            // Crash Handler
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(CrashHandler);
            InitializeComponent();
        }

        #region Loading

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDataAsync();
        }
        public async void LoadDataAsync()
        {
            bool isLoadingFromAppSelect = false;
            string fileLocation = string.Empty;

            if (((App)Application.Current).Args?.Length >= 1) // File selected
            {
                fileLocation = ((App)Application.Current).Args[0];
                isLoadingFromAppSelect = true;
            }

            while (VM == null)
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

                        if (dialogResult == System.Windows.Forms.DialogResult.Yes) // If Guideline is checked, then format
                        {
                            FormatterVM formatterVM = new(scoresheet);
                            if (!formatterVM.IsLoaded) break; // If teams list is not loaded, close app

                            FormatterDialog formatterDialog = new()
                            {
                                Owner = this,
                                DataContext = formatterVM
                            };
                            formatterDialog.ShowDialog();
                            if (!scoresheet.IsFormatted) break; // if formatting cancelled, close app
                            if (formatterDialog.FormattedScoresheetFileLocation != null) fileLocation = formatterDialog.FormattedScoresheetFileLocation;
                        }
                        else if (dialogResult == System.Windows.Forms.DialogResult.No) continue; // Try Again
                        else break; // Close App
                    }
                    else
                    {
                        scoresheet.Initialise();
                    }

                    // Now, Scoresheet is formatted.
                    // Hence, Load into MainWindow

                    VM = new(scoresheet, fileLocation);
                    DataContext = VM;
                }
                catch (Exception e)
                {
                    if (Messager.OutException(e, yesButtonText: "Try Again", isCancelButtonVisible: true) == System.Windows.Forms.DialogResult.Yes)
                        continue;
                    else
                        break;
                }
            }

            if (VM == null) Close();
        }

        #endregion

        #region Closing

        private bool _IsReadyToClose = false;

        protected override void OnClosing(CancelEventArgs e)
        {
            // Avoid Refire
            if (_IsReadyToClose) return;
            base.OnClosing(e);

            // If dirty
            if (VM != null && VM.IsModified)
            {
                // Temp cancel Closing
                e.Cancel = true;

                // Ask to save
                System.Windows.Forms.DialogResult dialogResult = Messager.Out(
                    "Would you like to save the scoresheet before closing?",
                    "Unsaved changes",
                    ConsoleStyle.WarningBlockStyle,
                    isCancelButtonVisible: true,
                    isNoButtonVisible: true);

                if (dialogResult == System.Windows.Forms.DialogResult.Yes)
                {
                    try
                    {
                        VM.ScoresheetFile.LastSavedTime = DateTime.Now;
                        VM.ScoresheetFile.LastAuthor = VM.UserName;
                        XML.Save(VM.FileLocation, VM.ScoresheetFile);
                        VM.NotifyChange(this);
                    }
                    catch (Exception ee)
                    {
                        Messager.OutException(ee, "Saving");
                        return; // Abort closing
                    }
                }
                else if (dialogResult == System.Windows.Forms.DialogResult.Cancel) return; // 'Cancel' pressed - Abort closing

                // Restart closing
                _IsReadyToClose = true;
                Application.Current.Shutdown();
            }
        }

        #endregion

        #region Crash Handler

        private void CrashHandler(object sender, UnhandledExceptionEventArgs args)
        {
#pragma warning disable CS0162 // Unreachable code detected when DEBUG config
            try
            {
                if (VM != null)
                {
                    XML.Save(VM.FileLocation + ".crash", VM.ScoresheetFile);

#if DEBUG
                    return;
#endif

                    Exception e = (Exception)args.ExceptionObject;
                    MessageBox.Show($"{e.GetType().Name}: {e.Message}\nThe scoresheet was saved to {VM.FileLocation + ".crash"}. Rename and re-open this to restore. See crash-info.txt fore more info.", " An Unhandled Exception Occurred", MessageBoxButton.OK, MessageBoxImage.Error);
                    System.IO.File.AppendAllLines(System.IO.Path.GetDirectoryName(VM.FileLocation) + "\\crash-info.txt",
                        new string[] 
                        {
                            "______________________________________________________",
                            $"An unhandled exception occurred at {DateTime.Now:g}",
                            $"A backup of Scoresheet was saved at {VM.FileLocation}.crashed",
                            $"Error Message:\t{e.Message}",
                            $"Stack Trace:\n{e.StackTrace}",
                        }
                    );
                }

            }
            catch (Exception)
            {
                MessageBox.Show($"An exception occurred in the crash-handler. The scoresheet is unlikely to have been saved. Backups of the Scoresheet may be found in the Backups subfolder.", "Dual Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }
#pragma warning restore CS0162 // Unreachable code detected
        }
        #endregion

    }
}

