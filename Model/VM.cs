using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scoresheet.Formatter;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media;
using Examath.Core.Environment;
using System.Threading.Tasks;
using Examath.Core.Utils;
using System.ComponentModel.DataAnnotations;
using Scoresheet.Properties;

namespace Scoresheet.Model
{
    public partial class VM : ObservableObject
    {
        #region File Properties

        private string _UserName = "X";
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

            _UserNameI = new(this, nameof(UserName), label: "Editor Name") { IsFocused = true, HelpText = "Enter your name (or initials) for tracing purposes" };
        }

        public VM(ScoresheetFile scoresheetFile, string fileLocation)
        {
            ScoresheetFile = scoresheetFile;
            ScoresheetFile.Modified += NotifyChange;
            FileLocation = fileLocation;
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
                await XML.SaveAsync(_FileLocation, ScoresheetFile);
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

        #region Participant List

        [RelayCommand]
        public void ExportParticipantList()
        {
            SaveFileDialog saveFileDialog = new()
            {
                Title = "Export Participant-Items List",
                Filter = "Rich Text Document |*.rtf"
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                FlowDocument flowDocument = new(new Paragraph(new Run(DateTime.Now.ToString())))
                {
                    FontFamily = new FontFamily("Arial"),
                };

                foreach (CompetitionItem competitionItem in ScoresheetFile.CompetitionItems)
                {
                    Section section = new();
                    string subTitle = "";

                    if (competitionItem is SoloItem soloItem)
                    {
                        subTitle = soloItem.Level.ToString() + ", " +
                            (soloItem.IsOnStage ? "Stage" : "Non-Stage") + ": ";
                    }

                    Paragraph header = new(new Run(subTitle))
                    {
                        Margin = new Thickness(0, 16, 0, 4),
                    };


                    header.Inlines.Add(new Run(competitionItem.Name) { FontWeight = FontWeights.Bold });
                    header.FontSize = 22;
                    section.Blocks.Add(header);

                    Table table = new() { };
                    TableRowGroup rowGroup = new TableRowGroup();

                    TableRow headerRow = new();
                    TableRow personsRow = new();

                    foreach (Team team in ScoresheetFile.Teams)
                    {
                        table.Columns.Add(new TableColumn() { Width = new GridLength(250) });
                        headerRow.Cells.Add(new TableCell(new Paragraph(new Run(
                            team.ToString()
                            )
                        { FontWeight = FontWeights.Bold })));
                        personsRow.Cells.Add(new TableCell());
                    }

                    foreach (IndividualParticipant individualParticipant in competitionItem.IndividualParticipants)
                    {
                        int teamIndex = (individualParticipant.Team != null) ? ScoresheetFile.Teams.IndexOf(individualParticipant.Team) : 0;
                        personsRow.Cells[teamIndex].Blocks.Add(new Paragraph(new Run(individualParticipant.FullName)));
                    }

                    rowGroup.Rows.Add(headerRow);
                    rowGroup.Rows.Add(personsRow);
                    table.RowGroups.Add(rowGroup);

                    section.Blocks.Add(table);
                    flowDocument.Blocks.Add(section);
                }

                using FileStream fileStream = new(saveFileDialog.FileName, FileMode.Create);
                TextRange textRange = new(flowDocument.ContentStart, flowDocument.ContentEnd);
                textRange.Save(fileStream, System.Windows.DataFormats.Rtf);
            }
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
        public async Task Format()
        {
            FormatterVM formatterVM = new(ScoresheetFile);

            FormatterDialog formatterDialog = new()
            {
                DataContext = formatterVM
            };

            formatterDialog.ShowDialog();

            await TrySaveAsync();
        }

        private readonly AskerOptions _EditParticipantAskerOptions = new(title: "Edit Participant", canCancel: true);

        [RelayCommand]
        public void EditParticipant(IndividualParticipant individualParticipant)
        {
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
                individualParticipant.SubmissionTimeStamp = DateTime.Now;
            }
        }

        #endregion

        #region MarkingView

        #endregion
    }
}
