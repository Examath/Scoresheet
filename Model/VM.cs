using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Examath.Core.Environment;
using Examath.Core.Utils;
using Scoresheet.Exporters;
using Scoresheet.Formatter;
using Scoresheet.Properties;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace Scoresheet.Model
{
    public partial class VM : ObservableObject
    {
        #region File Properties

        private string _UserName = "Enter_Name";
        /// <summary>
        /// Gets or sets the name of the user currently modifying this scoresheet
        /// </summary>
        public string UserName
        {
            get => _UserName;
            set => SetProperty(ref _UserName, value);
        }

        private readonly TextBoxInput _UserNameI;

        public ScoresheetFile ScoresheetFile { get; set; }

        private string _FileLocation = "";
        /// <summary>
        /// Gets or sets the location this scoresheet should save to
        /// </summary>
        public string FileLocation
        {
            get => _FileLocation;
            set
            {
                if (SetProperty(ref _FileLocation, value))
                {
                    FileName = Path.GetFileNameWithoutExtension(value);
                    OnPropertyChanged(nameof(FileName));
                    BackupDirectory = $"{Path.GetDirectoryName(value)}\\{FileName} Backups";
                }
            }
        }

        public string FileName { get; private set; } = "Empty";

        public string? BackupDirectory { get; private set; }

        #endregion

        #region Initializers

        public VM()
        {
            ScoresheetFile = new ScoresheetFile();
            _ParticipantListExporter = new(ScoresheetFile);

            _UserNameI = new(this, nameof(UserName), label: "Editor Name") { IsFocused = true, HelpText = "Enter your name (or initials) for tracing purposes" };
        }

        public VM(ScoresheetFile scoresheetFile, string fileLocation)
        {
            ScoresheetFile = scoresheetFile;
            ScoresheetFile.Modified += NotifyChange;
            scoresheetFile.ScoreAdded += ScoresheetFile_ScoreAdded;
            FileLocation = fileLocation;
            _ParticipantListExporter = new(ScoresheetFile);
            _UserNameI = new(this, nameof(UserName), label: "Editor Name") { IsFocused = true, HelpText = "Enter your name (or initials) for tracing purposes" };
        }

        #endregion

        #region Modification Tracking

        private int _Changes = 0;
        /// <summary>
        /// Gets whether this scoresheet has been modified
        /// </summary>
        /// <remarks>
        /// To set as modified, call <see cref="NotifyChange(object?, EventArgs?)"/>
        /// </remarks>
        public bool IsModified => _Changes > 0;

        private string _LastChange = "";
        /// <summary>
        /// Gets the target of the last change
        /// </summary>
        public string LastChange => _LastChange;
        public void NotifyChange(object? label, EventArgs? e = null)
        {
            _Changes++;
            _LastChange = (label == null) ? $"{_Changes} changes" : $"{_Changes} changes: {label}";
            OnPropertyChanged(nameof(IsModified));
            OnPropertyChanged(nameof(LastChange));
            TrySaveCommand.NotifyCanExecuteChanged();
        }

        #endregion

        #region Saving

        private DateTime LastBackup = DateTime.MinValue;

        [RelayCommand(CanExecute = nameof(IsModified))]
        public async Task TrySaveAsync()
        {
            try
            {
                ScoresheetFile.LastSavedTime = DateTime.Now;
                ScoresheetFile.LastAuthor = UserName;
                await XML.SaveAsync(_FileLocation, ScoresheetFile, new() { Indent = true, });
                _Changes = -1;
                NotifyChange(null);
            }
            catch (Exception e)
            {
                Messager.OutException(e, "Saving");
            }

            if (LastBackup + Settings.Default.BackUpMinimumInterval < DateTime.Now && BackupDirectory != null)
            {
                try
                {
                    Directory.CreateDirectory(BackupDirectory);
                    await XML.SaveAsync($"{BackupDirectory}\\{DateTime.Now:yyyy-MM-dd HH-mm} {UserName}.ssf", ScoresheetFile);
                    LastBackup = DateTime.Now;
                }
                catch (Exception e)
                {
                    Messager.OutException(e, "Backing Up");
                }
            }
        }

        #endregion

        #region Exporters

        private ParticipantListExporter _ParticipantListExporter;

        [RelayCommand]
        public void ExportParticipantList()
        {
            _ParticipantListExporter.Export();
        }

        #endregion

        #region ParticipantsView

        private IndividualParticipant? _SelectedParticipant;

        /// <summary>
        /// Gets or sets the selected <see cref="IndividualParticipant"/>
        /// </summary>
        public IndividualParticipant? SelectedParticipant
        {
            get => _SelectedParticipant;
            set { SetProperty(ref _SelectedParticipant, value); }
        }

        [RelayCommand]
        public void Format()
        {
            FormatterVM formatterVM = new(ScoresheetFile);

            FormatterDialog formatterDialog = new()
            {
                DataContext = formatterVM
            };

            formatterDialog.ShowDialog();
        }

        private readonly AskerOptions _EditParticipantAskerOptions = new(title: "Edit Participant", canCancel: true);

        [RelayCommand]
        public void EditParticipant(IndividualParticipant individualParticipant)
        {
            AskerNote note = new($"This dialog creates and applies a new submission to {individualParticipant.FullName}.");

            StringQ itemsQ = new(label: "Item Codes", defaultValue: individualParticipant.CompetitionItemsXML)
            {
                HelpText = "Modify the items here. Each item has a unique code consisting of it's name followed by the level abbreviations, separated by commas. " +
                "The codes must be an exact match. The codes are case and spacing sensitive.",
            };

            if (Asker.Show(_EditParticipantAskerOptions, _UserNameI, itemsQ))
            {
                if (itemsQ.Value == individualParticipant.CompetitionItemsXML) return;

                foreach (string code in itemsQ.Value.Split(','))
                {
                    if (ScoresheetFile.CompetitionItems.FindIndex((x) => x.Code == code) == -1)
                    {
                        Messager.Out(
                            $"Competition item '{code}' was not found.\nVerify that the name, case, spacing and level-code are correct.\nNo edits will be made.",
                            title: "Mismatching Item Code",
                            messageStyle: ConsoleStyle.FormatBlockStyle);
                        return;
                    }
                }

                individualParticipant.UnjoinAllCompetitions();
                individualParticipant.JoinCompetitions(itemsQ.Value.Split(','));
                individualParticipant.SubmissionEmail = UserName;
                individualParticipant.SubmissionFullName = individualParticipant.FullName;
                individualParticipant.SubmissionTimeStamp = DateTime.Now;
            }
        }

        [RelayCommand]
        public void SearchParticipant()
        {
            if (Searcher.Select(ScoresheetFile.IndividualParticipants, "Find Participant") is IndividualParticipant individualParticipant)
            {
                SelectedParticipant = individualParticipant;
            }
        }

        #endregion

        #region MarkingView

        private CompetitionItem? _MarkingCompetitionItem = null;
        /// <summary>
        /// Gets or sets the competition item being currently marked
        /// </summary>
        public CompetitionItem? MarkingCompetitionItem
        {
            get => _MarkingCompetitionItem;
            set
            {
                if (SetProperty(ref _MarkingCompetitionItem, value))
                {
                    ScoresRef = value?.Scores;
                    UpdateIntersection();
                }
            }
        }

        private ObservableCollection<Score>? _ScoresRef;
        /// <summary>
        /// Gets the current <see cref="MarkingCompetitionItem"/> score list
        /// </summary>
        public ObservableCollection<Score>? ScoresRef
        {
            get => _ScoresRef;
            set => SetProperty(ref _ScoresRef, value);
        }


        private Participant? _MarkingParticipant = null;
        /// <summary>
        /// Gets or sets the participant being marked
        /// </summary>
        public Participant? MarkingParticipant
        {
            get => _MarkingParticipant;
            set
            {
                if (SetProperty(ref _MarkingParticipant, value)) UpdateIntersection();
            }
        }

        private Score? _CurrentScoreIntersection = null;
        /// <summary>
        /// Gets the current intersection score between <see cref="MarkingCompetitionItem"/> and <see cref="MarkingParticipant"/>
        /// </summary>
        public Score? CurrentScoreIntersection
        {
            get => _CurrentScoreIntersection;
            private set => SetProperty(ref _CurrentScoreIntersection, value);
        }

        public void UpdateIntersection()
        {
            if (MarkingCompetitionItem != null && MarkingParticipant != null)
            {
                CurrentScoreIntersection = MarkingCompetitionItem.GetIntersection(MarkingParticipant);
            }
        }

        private void ScoresheetFile_ScoreAdded(object? sender, ScoreAddedEventArgs e)
        {
            OnPropertyChanged(nameof(ScoresRef));
            NotifyChange(e);
        }

        #endregion
    }
}
