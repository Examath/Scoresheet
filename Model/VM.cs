using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media;

namespace Scoresheet.Model
{
    public partial class VM : ObservableObject
    {
        #region Properties

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
                }
            }
        }

        public string FileName { get; private set; } = "Empty";

        #endregion

        #region Initializers

        public VM()
        {
            ScoresheetFile = new ScoresheetFile();
        }

        public VM(ScoresheetFile scoresheetFile, string fileLocation)
        {
            ScoresheetFile = scoresheetFile;
            _FileLocation = fileLocation;
        }

        #endregion

        #region Saving



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
                        Margin = new Thickness(0,16,0,4),
                    };


                    header.Inlines.Add(new Run(competitionItem.Name) { FontWeight = FontWeights.Bold});
                    header.FontSize = 22;
                    section.Blocks.Add(header);

                    Table table = new() { };
                    TableRowGroup rowGroup = new TableRowGroup();

                    TableRow headerRow = new();
                    TableRow personsRow = new();

                    foreach (Team team in ScoresheetFile.Teams)
                    {
                        table.Columns.Add(new TableColumn() { Width = new GridLength(250)});
                        headerRow.Cells.Add(new TableCell(new Paragraph(new Run(
                            team.ToString()
                            ) { FontWeight = FontWeights.Bold })));
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

        #endregion

        #region MarkingView

        #endregion
    }
}
