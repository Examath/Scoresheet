using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Examath.Core.Environment;
using Examath.Core.Utils;
using Scoresheet.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Scoresheet.Formatter
{
    public partial class FormatterVM : ObservableObject
    {
        #region Properties

        /// <summary>
        /// Set to true when the teams list is loaded into an unformatted scoresheet
        /// </summary>
        public bool IsLoaded { get; set; }

        private double _Progress = 0;
        /// <summary>
        /// Gets or sets the progress whilst doing a task
        /// </summary>
        public double Progress
        {
            get => _Progress;
            private set => SetProperty(ref _Progress, value);
        }

        #endregion

        #region Initializers

        public FormatterVM()
        {
            ScoresheetFile = new();
        }

        public FormatterVM(ScoresheetFile scoresheetFile)
        {
            ScoresheetFile = scoresheetFile;

            // Todo if contains participants return

            while (!IsLoaded)
            {
                OpenFileDialog openFileDialog = new()
                {
                    Title = "Open Teams List file",
                    Filter = "Teams List (.sstl)|*.sstl|All|*.*",
                };

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        List<IndividualParticipant> individualParticipants = new();
                        string[] rdata = File.ReadAllLines(openFileDialog.FileName);
                        string[] buffer = new string[3];

                        int[,] chestNumberMatrix = getChestNumberMatrix();

                        foreach (string line in rdata)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            string[] entry = line.Split('\t');

                            for (int i = 0; i < Math.Min(buffer.Length, entry.Length); i++)
                            {
                                if (!string.IsNullOrWhiteSpace(entry[i])) buffer[i] = entry[i];
                            }

                            if (!string.IsNullOrWhiteSpace(entry[0]))
                            {
                                individualParticipants.Add(new(buffer, chestNumberMatrix, ScoresheetFile));
                            }
                        }
                        if (individualParticipants.Count < 1)
                        {
                            if (Messager.Out("Want to try loading the teams list again", "Teams list is empty", yesButtonText: "Try Again", isCancelButtonVisible: true) == DialogResult.Yes)
                                continue;
                            else
                                return;
                        }
                        DialogResult dialogResult = Messager.Out($"Count: {individualParticipants.Count}\n\n{string.Join('\n', individualParticipants)}", $"Check Teams List",
                        isCancelButtonVisible: true, noButtonText: "Try Again", yesButtonText: "Continue");
                        switch (dialogResult)
                        {
                            case DialogResult.Yes:
                                ScoresheetFile.IndividualParticipants = individualParticipants;
                                IsLoaded = true;
                                break;
                            case DialogResult.No:
                                continue;
                            default:
                                return;
                        }
                    }
                    catch (Exception e)
                    {
                        if (Messager.OutException(e, yesButtonText: "Try Again", isCancelButtonVisible: true) == DialogResult.Yes)
                            continue;
                        else
                            return;
                    }
                }
                else return;
            }

            int[,] getChestNumberMatrix()
            {
                int[,] chestNumberMatrix = new int[ScoresheetFile.LevelDefinitions.Count, ScoresheetFile.Teams.Count];

                for (int levelIndex = 0; levelIndex < chestNumberMatrix.GetLength(0); levelIndex++)
                {
                    for (int teamIndex = 0; teamIndex < chestNumberMatrix.GetLength(1); teamIndex++)
                    {
                        chestNumberMatrix[levelIndex, teamIndex] = (levelIndex * ScoresheetFile.Teams.Count + teamIndex + 1) * 100;
                    }
                }

                return chestNumberMatrix;
            }
        }

        #endregion

        #region Scoresheet Object

        public Model.ScoresheetFile ScoresheetFile { get; set; }

        #endregion

        #region Importer

        [RelayCommand]
        public async Task Import()
        {
            Progress = 0.01;

            OpenFileDialog openFileDialog = new()
            {
                Title = "Open submissions from Google forms",
                Filter = "csv|*.csv|All|*.*",
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string[] data = await File.ReadAllLinesAsync(openFileDialog.FileName);

                Progress = 0.1;

                for (int i = 1; i < data.Length; i++)
                {
                    Progress = 0.9 * i / data.Length + 0.1;
                    FormSubmission formSubmission = await Task.Run<FormSubmission>(() => new(data[i], ScoresheetFile));
                    FormSubmissions.Add(formSubmission);
                }
            }

            Progress = 0;
        }

        private ObservableCollection<FormSubmission> _FormSubmissions = new();
        /// <summary>
        /// Gets or sets the list of invalid submissions that need to be corrected
        /// </summary>
        public ObservableCollection<FormSubmission> FormSubmissions
        {
            get => _FormSubmissions;
            set { if (SetProperty(ref _FormSubmissions, value)) return; }
        }

        #endregion

        #region Fixer

        private bool _CanFix { get; set; } = false;

        private string _FixSuggestion = "----";
        /// <summary>
        /// Gets or sets 
        /// </summary>
        public string FixSuggestion
        {
            get => _FixSuggestion;
            set => SetProperty(ref _FixSuggestion, value);
        }

        private void UpdateFixState(bool canFix, string fixSuggestion)
        {
            _CanFix = canFix;
            fixCommand?.NotifyCanExecuteChanged();
            if (SelectedParticipant != null && SelectedSubmission != null && !SelectedSubmission.IsValidMatch(SelectedParticipant))
            {
                FixSuggestion = fixSuggestion + " *";
            }
            else
            {
                FixSuggestion = fixSuggestion;
            }
        }

        private IndividualParticipant? _SelectedParticipant;
        /// <summary>
        /// Gets or sets the selected <see cref="IndividualParticipant"/>
        /// </summary>
        public IndividualParticipant? SelectedParticipant
        {
            get => _SelectedParticipant;
            set { if (SetProperty(ref _SelectedParticipant, value)) SelectedParticipant_Changed(); }
        }

        private void SelectedParticipant_Changed()
        {
            FindCurrentMatchingSubmissionCommand?.NotifyCanExecuteChanged();
            if (SelectedParticipant != null && SelectedSubmission != null) // Then activiate fixer
            {
                if (SelectedParticipant.SubmissionTimeStamp == SelectedSubmission.TimeStamp) // Currently set
                {
                    UpdateFixState(false, "Applied");
                }
                else if (SelectedParticipant.SubmissionTimeStamp > SelectedSubmission.TimeStamp)
                {
                    UpdateFixState(true, "Ignore");
                }
                else if (SelectedParticipant.SearchName == SelectedSubmission.SearchName) // Fix invalid
                {
                    UpdateFixState(true, "Edit");
                }
                else // Fix mismatch
                {
                    UpdateFixState(true, "Assign");
                }
            }
            else
            {
                UpdateFixState(false, "----");
            }
        }

        private FormSubmission? _SelectedSubmission;
        /// <summary>
        /// Gets or sets the selected <see cref="FormSubmission"/>
        /// </summary>
        public FormSubmission? SelectedSubmission
        {
            get => _SelectedSubmission;
            set { if (SetProperty(ref _SelectedSubmission, value)) SelectedSubmission_Changed(); }
        }

        /// <summary>
        /// If the selected <see cref="FormSubmission"/> is changed, then select the matching <see cref="IndividualParticipant"/>
        /// </summary>
        private void SelectedSubmission_Changed()
        {
            if (SelectedSubmission != null) // Then find matching individual
            {
                if (SelectedParticipant == null || SelectedSubmission.Match != SelectedParticipant) // Only if match not selected already
                {
                    foreach (IndividualParticipant individualParticipant in ScoresheetFile.IndividualParticipants)
                    {
                        if (SelectedSubmission.Match == individualParticipant)
                        {
                            SelectedParticipant = individualParticipant;
                            return;
                        }
                    }

                    // If not found
                    UpdateFixState(false, "----");
                }
            }
        }

        /// <summary>
        /// Fixes mismatches and invalid and stuff
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanFix))]
        public void Fix()
        {
            if (SelectedParticipant != null && SelectedSubmission != null)
            {
                bool ok = true;

                if (!SelectedSubmission.IsValidMatch(SelectedParticipant))
                {
                    ok = Messager.Out("Invalid submission ... merrging is currently not suppored",
                        "Invalid Submission?",
                        ConsoleStyle.WarningBlockStyle,
                        isCancelButtonVisible: true,
                        yesButtonText: "Apply anyway") == DialogResult.Yes;
                }

                if (ok)
                {
                    SelectedSubmission.ApplyMatch(SelectedParticipant, ScoresheetFile);
                    SelectedParticipant_Changed();

                    // Find next mismatch to solve
                    foreach (FormSubmission submission in _FormSubmissions)
                    {
                        if (submission.SubmissionStatus == SubmissionStatus.Mismatch || submission.SubmissionStatus == SubmissionStatus.Invalid)
                        {
                            SelectedSubmission = submission;
                        }
                    }
                }
            }
        }

        private bool CanFindCurrentMatchingSubmission() => SelectedParticipant != null && SelectedParticipant.IsFormSubmitted;

        /// <summary>
        /// Selects the curent matching submission of the current selected <see cref="IndividualParticipant"/>
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanFindCurrentMatchingSubmission))]
        public void FindCurrentMatchingSubmission()
        {
            if (SelectedParticipant != null && SelectedParticipant.IsFormSubmitted) // Then find matching submission
            {
                if (SelectedSubmission == null || SelectedSubmission.Match != SelectedParticipant) // Only if match not selected already
                {
                    foreach (FormSubmission formSubmission in FormSubmissions)
                    {
                        if (formSubmission.TimeStamp == SelectedParticipant.SubmissionTimeStamp)
                        {
                            SelectedSubmission = formSubmission;
                            break;
                        }
                    }
                }
                UpdateFixState(false, "Applied");
            }
        }

        #endregion

        #region Create

        #endregion
    }
}
