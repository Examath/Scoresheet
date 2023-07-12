using Examath.Core.Environment;
using Examath.Core.Utils;
using Scoresheet.Formatter;
using Scoresheet.Model;
using Scoresheet.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Input;

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
                    VM.PropertyChanged += VM_PropertyChanged;
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

        private void VM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "MarkingCompetitionItem" || e.PropertyName == "MarkingParticipant")
            {
                ApplyScoreButton.IsEnabled = CanApplyScore();
            }
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

        #region Marking Tab Input

        private void MarkingTab_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Insert)
            {
                NewScoreTextBox.SelectAll();
                NewScoreTextBox.Focus();
            }
        }

        private void NewScoreTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!IsKeyADigit(e.Key))
            {
                e.Handled = true;
                int pos = NewScoreTextBox.SelectionStart;

                if (IsKeyASeparator(e.Key) && pos >= 1 && NewScoreTextBox.Text[pos - 1] != ',')
                {
                    NewScoreTextBox.Text = NewScoreTextBox.Text.Insert(NewScoreTextBox.SelectionStart, ",");
                    NewScoreTextBox.SelectionStart = pos + 1;
                }
            }
        }

        private static bool IsKeyADigit(Key key)
        {
            return (key >= Key.D0 && key <= Key.D9) || (key == Key.OemPeriod) || (key >= Key.NumPad0 && key <= Key.NumPad9) || key == Key.Delete || key == Key.Back || key == Key.Left || key == Key.Right;
        }

        private static bool IsKeyASeparator(Key key)
        {
            return key == Key.Tab || key == Key.Space || key == Key.OemComma || key == Key.Add || key == Key.OemPlus;
        }

        private double newScoreAverage = 0;

        private void NewScoreTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            string[] markStrings = NewScoreTextBox.Text.Split(',');
            double total = 0;
            int length = 0;

            foreach (string markString in markStrings)
            {
                if (string.IsNullOrWhiteSpace(markString)) continue;
                else if (double.TryParse(markString.Trim(), out double mark))
                {
                    total += mark;
                    length++;
                }
                else
                {
                    total = double.NaN;
                    break;
                }
            }

            if (length == 0)
            {
                newScoreAverage = 0;
                ApplyScoreButton.IsEnabled = false;
            }
            else 
            {
                newScoreAverage = Math.Round(total / length, Settings.Default.MarksPrecision);
                ApplyScoreButton.IsEnabled = CanApplyScore();
            }
            NewTotalScoreLabel.Content = newScoreAverage;

            if (e.Key == Key.Enter && CanApplyScore())
            {
                ApplyScore();
            }
        }

        private bool CanApplyScore() => 
            VM.MarkingCompetitionItem != null && 
            VM.MarkingParticipant != null && 
            !string.IsNullOrWhiteSpace(NewScoreTextBox.Text) && 
            !double.IsNaN(newScoreAverage);

        private void ApplyScoreButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyScore();
        }

        private void ApplyScore()
        {
            if (VM == null || VM.MarkingCompetitionItem == null || VM.MarkingParticipant == null || double.IsNaN(newScoreAverage)) return;

            string[] markStrings = NewScoreTextBox.Text.Split(',');
            List<double> marks = new();

            foreach (string markString in markStrings)
            {
                if (string.IsNullOrWhiteSpace(markString)) continue;
                else if (double.TryParse(markString.Trim(), out double mark))
                {
                    marks.Add(mark);
                }
                else
                {
                    Messager.Out("Score should be expressed as numbers separated by '+' characters. The numbers may include decimals. Nothing has been applied", "Score format incorrect", ConsoleStyle.FormatBlockStyle);
                    return;
                }
            }

            if (marks.Count > 0)
            {
                VM.MarkingCompetitionItem.AddScore(VM.MarkingParticipant, marks, VM.UserName);
                VM.UpdateIntersection();
                NewScoreTextBox.Text = "";
                NewTotalScoreLabel.Content = 0;
                ApplyScoreButton.IsEnabled = false;
            }
        }

        #endregion

    }
}

