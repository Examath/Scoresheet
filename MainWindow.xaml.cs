using Examath.Core.Environment;
using Examath.Core.Utils;
using Scoresheet.Model;
using Scoresheet.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Scoresheet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private VM? _VM;
        private readonly string _Version = System.Reflection.Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString(2) ?? "?";
        private bool _IsLoaded = false;

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
            string fileLocation = string.Empty;

            if (((App)Application.Current).Args?.Length >= 1) // App triggered fromfile explorer
            {
                fileLocation = ((App)Application.Current).Args[0];
            }
            else if (Messager.Out(
                "Would you like to open an existing scoresheet or create a new one from the default template?",
                $"Welcome to Scoresheet v{_Version}",
                yesButtonText: "Open Existing...",
                isNoButtonVisible: true,
                noButtonText: "Create New"
                ) == System.Windows.Forms.DialogResult.Yes)
            {
                System.Windows.Forms.OpenFileDialog openFileDialog = new()
                {
                    Title = "Open Scoresheet file",
                    Filter = "Scoresheet (.ssf)|*.ssf|All|*.*",
                };

                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) fileLocation = openFileDialog.FileName;
            }
            else
            {
                Messager.Out("Sorry creating new is not yet supported. Please open an existing template",
                        yesButtonText: "Exit");
                Close();
                return;
            }

            // Try Load data
            try
            {
                // Load Scoresheet XML
                ScoresheetFile? scoresheet = await XML.LoadAsync<ScoresheetFile>(fileLocation);

                // Check if null
                if (scoresheet == null)
                {
                    Messager.Out("Scoresheet is null, an error may have occurred.", "Could Not Load Scoresheet",
                        messageStyle: ConsoleStyle.ErrorBlockStyle,
                        yesButtonText: "Exit");
                    Close();
                    return;
                }

                // Initialise Scoresheet
                await scoresheet.InitialiseAsync();

                // Now, Scoresheet is formatted.
                // Hence, Load into MainWindow
                LoadVM(scoresheet, fileLocation);
            }
            catch (Exception e)
            {
                Messager.OutException(e, "Loading",
                        yesButtonText: "Exit");
                Close();
                return;
            }

            if (_VM == null)
            {
                Close();
                return;
            }

            _IsLoaded = true;
        }


        private void LoadVM(ScoresheetFile scoresheet, string fileLocation)
        {
            _VM = new(scoresheet, fileLocation);
#if DEBUG
            _VM.UserName = "DEBUG EXCEMPTED";
#else
            _VM.UserName = Env.Default.In("Please enter your name");
#endif
            DataContext = _VM;
            _VM.PropertyChanged += VM_PropertyChanged;
            _VM.ScoreChanged += _VM_ScoreChanged;
        }

        private void _VM_ScoreChanged(object? sender, ScoreChangedEventArgs e)
        {
            ResetNewScore();
        }

        private void VM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {

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
            if (_VM != null && _VM.IsModified)
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
                        _VM.ScoresheetFile.LastSavedTime = DateTime.Now;
                        _VM.ScoresheetFile.LastAuthor = _VM.UserName;
                        XML.Save(_VM.FileLocation, _VM.ScoresheetFile);
                        _VM.NotifyChange(this);
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
                if (_VM != null)
                {
                    XML.Save(_VM.FileLocation + ".crash", _VM.ScoresheetFile);

#if DEBUG
                    return;
#endif

                    Exception e = (Exception)args.ExceptionObject;
                    MessageBox.Show($"{e.GetType().Name}: {e.Message}\nThe scoresheet was saved to {_VM.FileLocation + ".crash"}. Rename and re-open this to restore. See crash-info.txt fore more info.", " An Unhandled Exception Occurred", MessageBoxButton.OK, MessageBoxImage.Error);
                    System.IO.File.AppendAllLines(System.IO.Path.GetDirectoryName(_VM.FileLocation) + "\\crash-info.txt",
                        new string[]
                        {
                            "______________________________________________________",
                            $"An unhandled exception occurred at {DateTime.Now:g}",
                            $"A backup of Scoresheet was saved at {_VM.FileLocation}.crashed",
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

        private void SearchBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (_VM != null &&
                _VM.MarkingCompetitionItem != null &&
                e.Key == Key.Enter &&
                int.TryParse(SearchBox.Text, out int chestNumber))
            {
                Participant? participant = _VM.MarkingCompetitionItem.Participants.FirstOrDefault((p) => p.ChestNumber == chestNumber);
                if (participant != null)
                {
                    _VM.MarkingParticipant = participant;
                    SearchBox.Text = "";
                    NewScoreTextBox.SelectAll();
                    NewScoreTextBox.Focus();
                }
            }
        }

        private void MarkingListBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                NewScoreTextBox.SelectAll();
                NewScoreTextBox.Focus();
            }
        }

        private void NewScoreTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!IsKeyADigit(e.Key) && !(e.Key == Key.Escape))
            {
                e.Handled = true;
                int pos = NewScoreTextBox.SelectionStart;

                if ((IsKeyAJudgeSeparator(e.Key) || IsKeyACriteriaSeparator(e.Key))
                    && pos >= 1
                    && NewScoreTextBox.Text[pos - 1] != ','
                    && NewScoreTextBox.Text[pos - 1] != '+')
                {
                    NewScoreTextBox.Text = NewScoreTextBox.Text.Insert(NewScoreTextBox.SelectionStart,
                        (IsKeyAJudgeSeparator(e.Key) ? "," : "+"));
                    NewScoreTextBox.SelectionStart = pos + 1;
                }
            }
        }

        private static bool IsKeyADigit(Key key)
        {
            return (key >= Key.D0 && key <= Key.D9) || (key == Key.OemPeriod) || (key >= Key.NumPad0 && key <= Key.NumPad9) || key == Key.Delete || key == Key.Back || key == Key.Left || key == Key.Right;
        }

        private static bool IsKeyAJudgeSeparator(Key key)
        {
            return key == Key.Tab || key == Key.OemComma || key == Key.Divide;
        }

        private static bool IsKeyACriteriaSeparator(Key key)
        {
            return key == Key.Add || key == Key.OemPlus || key == Key.OemMinus;
        }

        private void NewScoreTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (_VM != null && _VM.MarkingParticipant != null)
            {
                Score? newScore = Score.TryParse(_VM.MarkingParticipant, NewScoreTextBox.Text, _VM.UserName);
                if (newScore != null)
                {
                    _VM.EnteredScore = newScore;
                    _VM.IsLastEnteredScoreValid = true;
                }
                else
                {
                    _VM.IsLastEnteredScoreValid = false;
                }

                if (e.Key == Key.Enter && _VM != null && _VM.CanApplyScore())
                {
                    _VM.ApplyScore();
                }
            }

            if (e.Key == Key.Escape)
            {
                ResetNewScore();
            }
        }

        private void ResetNewScore()
        {
            NewScoreTextBox.Text = "";
            if (_VM != null) _VM.EnteredScore = null;

            if (SearchMode.IsChecked == false)
            {
                ListBoxItem listBoxItem =
                   (ListBoxItem)MarkingParticipantsListBox
                     .ItemContainerGenerator
                       .ContainerFromItem(MarkingParticipantsListBox.SelectedItem);

                listBoxItem.Focus();
            }
            else
            {
                SearchBox.Focus();
            }
        }

        #endregion

        #region Collection Views

        private void SelectRegisteredParticipantsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_VM != null)
            {
                foreach (IndividualParticipant individualParticipant in _VM.ScoresheetFile.IndividualParticipants)
                {
                    if (individualParticipant.IsRegistered && !ParticipantsListBox.SelectedItems.Contains(individualParticipant))
                    {
                        ParticipantsListBox.SelectedItems.Add(individualParticipant);
                    }
                }
            }

        }

        private void ParticipantsViewSortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_IsLoaded)
            {
                string newSortProperty = (ParticipantsViewSortComboBox.SelectedItem as ComboBoxItem)?.Tag.ToString() ?? "";
                ParticipantsListBox.Items.SortDescriptions.Clear();
                ParticipantsListBox.Items.SortDescriptions.Add(new SortDescription(newSortProperty, ListSortDirection.Ascending));
            }
        }

        #endregion
    }
}

