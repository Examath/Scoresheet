using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Examath.Core.Environment;
using Scoresheet.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Scoresheet.Formatter
{
    public partial class FormatterVM : ObservableObject
    {
        #region Basic Properties

        private int _Progress = 0;
        /// <summary>
        /// Gets or sets the number of registrations processed
        /// </summary>
        public int Progress
        {
            get => _Progress;
            private set => SetProperty(ref _Progress, value);
        }

        private List<FormSubmissionColumn> _DataColumns = new();
        /// <summary>
        /// Gets or sets the columns of the imported TSV document.
        /// </summary>
        public List<FormSubmissionColumn> DataColumns
        {
            get => _DataColumns;
            set => SetProperty(ref _DataColumns, value);
        }


        private List<string[]> _Data = new();
        /// <summary>
        /// Gets or sets the lines of the loaded TSV file
        /// </summary>
        public List<string[]> Data
        {
            get => _Data;
            set => SetProperty(ref _Data, value);
        }

        #endregion

        #region Initializers

        public FormatterVM()
        {
            ScoresheetFile = new();
        }

        /// <summary>
        /// Creates a new Formatter, with the specified <see cref="ScoresheetFile"/>,
        /// and asks the user to select the TSV file to sync
        /// </summary>
        /// <param name="scoresheetFile"></param>
        public FormatterVM(ScoresheetFile scoresheetFile)
        {
            ScoresheetFile = scoresheetFile;
        }

        #endregion

        #region Scoresheet Object

        public Model.ScoresheetFile ScoresheetFile { get; set; }

        #endregion

        #region Import Form Submissions

        public bool ImportData(string fileLocation)
        {
            try
            {
                using StreamReader reader = new(fileLocation);
                string? line;

                // Read header row
                line = reader.ReadLine();
                if (line == null)
                {
                    Messager.Out("Please open a .tsv file with the registrations.", "File is empty", messageStyle: ConsoleStyle.FormatBlockStyle);
                    return false;
                }
                DataColumns = line.Split('\t').Select(s => new FormSubmissionColumn(s)).ToList();
                if (DataColumns.Count <= 1)
                {
                    Messager.Out("Please open a Tab-Separated Value (.tsv) file with the registrations.", "File format not valid", messageStyle: ConsoleStyle.FormatBlockStyle);
                    return false;
                }

                // Read all rows
                while ((line = reader.ReadLine()) != null)
                {
                    string[] lineArray = line.Split('\t');
                    if (DataColumns.Count != lineArray.Length)
                    {
                        Messager.Out($"Line {Data.Count + 1} does not have the same number of columns as the rest of the .tsv file", 
                            "File format not valid", messageStyle: ConsoleStyle.FormatBlockStyle);
                        return false;
                    }
                    // Preformat
                    for (int i = 0; i < lineArray.Length; i++)
                    {
                        lineArray[i] = lineArray[i].Trim();
                    }
                    // Add
                    Data.Add(lineArray);
                }
                return true;
            }
            catch (Exception ex)
            {
                Messager.OutException(ex, $"Importing the TSV, line {Data.Count + 1}");
                return false;
            }
        }

        #endregion

        #region Synchronise

        [RelayCommand()]
        public async Task Synchronise()
        {
            // Read data first
            for (int i = 0; i < Data.Count; i++)
            {
                try
                {
                    FormSubmission formSubmission = await Task.Run<FormSubmission>(() => new(Data[i], DataColumns, ScoresheetFile));
                    FormSubmissions.Add(formSubmission);
                    Progress++;
                }
                catch (FormatException fe)
                {
                    Messager.OutException(fe, $"Reading submission #{i}");
                    Progress = 0;
                    FormSubmissions.Clear();
                }
            }

            Progress = 0;

            foreach (FormSubmission formSubmission in FormSubmissions)
            {
                // Apply match
                if (formSubmission.Match != null && formSubmission.MatchScore >= 1)
                {
                    if (formSubmission.IsValidMatch(formSubmission.Match)) // Validity of Submission
                    {
                        try
                        {
                            formSubmission.ApplyMatch(formSubmission.Match, ScoresheetFile);
                            Progress++;
                        }
                        catch (Examath.Core.Model.ObjectLinkingException ole)
                        {
                            Messager.OutException(ole, $"Applying {formSubmission.Match.FullName}");
                            break;
                        }
                    }
                    else
                    {
                        formSubmission.SubmissionStatus = SubmissionStatus.Invalid;
                    }
                }
                else
                {
                    formSubmission.SubmissionStatus = SubmissionStatus.Mismatch;
                }
            }
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
            if (SelectedParticipant != null && SelectedSubmission != null)
            {
                if (!SelectedSubmission.IsValidMatch(SelectedParticipant))
                {
                    FixSuggestion = $"{fixSuggestion} ({SelectedSubmission.MatchScore:P0}**)";
                }
                else
                {
                    FixSuggestion = $"{fixSuggestion} ({SelectedSubmission.MatchScore:P0})";
                }
            }
            else
            {
                FixSuggestion = $"----";
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
                    UpdateFixState(false, "Currently Applied");
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
                    if (SelectedParticipant.Level?.Within(SelectedSubmission.YearLevel) ?? false)
                    {
                        ok = Messager.Out(
                            $"{SelectedParticipant.FullName} is a year {SelectedParticipant.YearLevel}, per the teams list," +
                            $"but the year level selected ({SelectedSubmission.YearLevel}) in the form does not match. " +
                            $"The competition items the participant wants to join ({SelectedSubmission.Details}) may not apply correctly." +
                            $"Do you want to try to apply anyway?",
                            "Invalid Submission",
                            ConsoleStyle.WarningBlockStyle,
                            isCancelButtonVisible: true,
                            yesButtonText: "Apply") == DialogResult.Yes;
                    }
                }

                // Add more conditions here

                if (ok)
                {
                    try
                    {
                        SelectedSubmission.ApplyMatch(SelectedParticipant, ScoresheetFile);
                    }
                    catch (Examath.Core.Model.ObjectLinkingException ole)
                    {
                        Messager.OutException(ole, $"Applying {SelectedSubmission.Match?.FullName}");
                        return;
                    }

                    if (SelectedSubmission.IsProcessed) Progress++;
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

        private bool CanFindCurrentMatchingSubmission() => SelectedParticipant != null && SelectedParticipant.IsRegistered;

        /// <summary>
        /// Selects the curent matching submission of the current selected <see cref="IndividualParticipant"/>
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanFindCurrentMatchingSubmission))]
        public void FindCurrentMatchingSubmission()
        {
            if (SelectedParticipant != null && SelectedParticipant.IsRegistered) // Then find matching submission
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
