using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Examath.Core.Environment;
using Scoresheet.Model;
using System.Diagnostics;
using System.IO;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows;
using System;
using System.Windows.Media;

namespace Scoresheet.Exporters
{
    public partial class ParticipantListExporter
    {
        public ScoresheetFile ScoresheetFile { get; set; }

        public string SaveLocation { get; set; } = "C:\\temp\\doc.rtf";
        private FilePickerInput SaveLocationI;

        public bool AddChestNumbers { get; set; } = false;
        private CheckBoxInput AddChestNumbersI;

        public bool OpenAutomatically { get;set; } = true;
        private CheckBoxInput OpenAutomaticallyI;

        private AskerOptions _AskerOptions = new("Export Participant-Items List", canCancel: true);

        public ParticipantListExporter(ScoresheetFile scoresheetFile)
        {
            ScoresheetFile = scoresheetFile;
            SaveLocationI = new(this, nameof(SaveLocation), "Location to Export to") { ExtensionFilter = "Rich Text Document|*.rtf", UseSaveFileDialog = true };
            AddChestNumbersI = new(this, nameof(AddChestNumbers), "Add chest numbers");
            OpenAutomaticallyI = new(this, nameof(OpenAutomatically), "Open file when complete");
        }

        public void Export()
        {
            if (Asker.Show(_AskerOptions, SaveLocationI, AddChestNumbersI, OpenAutomaticallyI))
            {
                FlowDocument flowDocument = new(new Paragraph(new Run("Generated at " + DateTime.Now.ToString())))
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
                        string participantRep = (AddChestNumbers) ? individualParticipant.ToString() : individualParticipant.FullName;
                        personsRow.Cells[teamIndex].Blocks.Add(new Paragraph(new Run(participantRep)));
                    }

                    rowGroup.Rows.Add(headerRow);
                    rowGroup.Rows.Add(personsRow);
                    table.RowGroups.Add(rowGroup);

                    section.Blocks.Add(table);
                    flowDocument.Blocks.Add(section);
                }

                using FileStream fileStream = new(SaveLocation, FileMode.Create);
                TextRange textRange = new(flowDocument.ContentStart, flowDocument.ContentEnd);
                textRange.Save(fileStream, System.Windows.DataFormats.Rtf);
                
                if (OpenAutomatically)
                {
                    ProcessStartInfo processStartInfo = new()
                    {
                        UseShellExecute = true,
                        FileName = SaveLocation,
                    };
                    try
                    {
                        Process.Start(processStartInfo);
                    }
                    catch (Exception ee)
                    {
                        Messager.OutException(ee, "Opening Document");
                    }
                }

            }
        }
    }
}
