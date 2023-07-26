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
using System.Linq;

namespace Scoresheet.Exporters
{
    public class ParticipantListExporter
    {
        private ScoresheetFile _ScoresheetFile { get; set; }

        public string SaveLocation { get; set; } = "C:\\temp\\doc.rtf";
        private FilePickerInput SaveLocationI;

        public bool AddChestNumbers { get; set; } = false;
        private CheckBoxInput AddChestNumbersI;

        public bool OpenAutomatically { get;set; } = true;
        private CheckBoxInput OpenAutomaticallyI;

        public double TeamColumnWidth { get; set; } = 350;
        private TextBoxInput TeamColumnWidthI;

        private AskerOptions _AskerOptions = new("Export IndividualParticipant-Items List", canCancel: true);

        public ParticipantListExporter(ScoresheetFile scoresheetFile)
        {
            _ScoresheetFile = scoresheetFile;
            SaveLocationI = new(this, nameof(SaveLocation), "Location to Export to") { ExtensionFilter = "Rich Text Document|*.rtf", UseSaveFileDialog = true };
            AddChestNumbersI = new(this, nameof(AddChestNumbers), "Add chest numbers");
            OpenAutomaticallyI = new(this, nameof(OpenAutomatically), "Open file when complete");
            TeamColumnWidthI = new(this, nameof(TeamColumnWidth), "Team column width");
        }

        public void Export()
        {
            if (Asker.Show(_AskerOptions, SaveLocationI, AddChestNumbersI, OpenAutomaticallyI, TeamColumnWidthI))
            {
                FlowDocument flowDocument = new(new Paragraph(new Run("Generated at " + DateTime.Now.ToString())))
                {
                    FontFamily = new FontFamily("Arial"),
                };

                foreach (CompetitionItem competitionItem in _ScoresheetFile.CompetitionItems)
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

                    rowGroup.Rows.Add(headerRow);
                    rowGroup.Rows.Add(personsRow);
                    table.RowGroups.Add(rowGroup);

                    section.Blocks.Add(table);

                    foreach (Team team in _ScoresheetFile.Teams)
                    {
                        table.Columns.Add(new TableColumn() { Width = new GridLength(TeamColumnWidth) });
                        headerRow.Cells.Add(new TableCell(new Paragraph(new Run(
                            team.ToString()
                            )
                        { FontWeight = FontWeights.Bold })));
                        personsRow.Cells.Add(new TableCell());
                    }

                    if (competitionItem is GroupItem groupItem)
                    {
                        System.Collections.Generic.List<IndividualParticipant> notInGroup = groupItem.IndividualParticipants.ToList();

                        foreach (GroupParticipant groupParticipant in groupItem.GroupParticipants)
                        {
                            int teamIndex = (groupParticipant.Team != null) ? _ScoresheetFile.Teams.IndexOf(groupParticipant.Team) : 0;
                            personsRow.Cells[teamIndex].Blocks.Add(ExportParticipant(groupParticipant));

                            foreach (IndividualParticipant individualParticipant in groupParticipant.IndividualParticipants)
                            {
                                notInGroup.Remove(individualParticipant);
                                personsRow.Cells[teamIndex].Blocks.Add(ExportParticipant(individualParticipant));
                            }
                        }

                        if (notInGroup.Count > 0)
                        {
                            Run note = new($"Not in group: {string.Join(", ", notInGroup.Select(p => (AddChestNumbers) ? p.ToString() : p.FullName).ToList())}")
                            {
                                Foreground = Brushes.Tomato
                            };
                            section.Blocks.Add(new Paragraph(note));
                        }
                    }
                    else // Slo Item {
                    {
                        foreach (IndividualParticipant individualParticipant in competitionItem.IndividualParticipants)
                        {
                            int teamIndex = (individualParticipant.Team != null) ? _ScoresheetFile.Teams.IndexOf(individualParticipant.Team) : 0;
                            personsRow.Cells[teamIndex].Blocks.Add(ExportParticipant(individualParticipant));
                        }
                    }

                    flowDocument.Blocks.Add(section);
                }

                try
                {
                    using FileStream fileStream = new(SaveLocation, FileMode.Create);
                    TextRange textRange = new(flowDocument.ContentStart, flowDocument.ContentEnd);
                    textRange.Save(fileStream, System.Windows.DataFormats.Rtf);
                }
                catch (Exception ee)
                {
                    Messager.OutException(ee, "Saving Document");
                    return;
                }
                
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

        public Paragraph ExportParticipant(IndividualParticipant individualParticipant)
        {
            string participantRep = (AddChestNumbers) ? individualParticipant.ToString() : individualParticipant.FullName;
            return new Paragraph(new Run(participantRep));
        }

        public Paragraph ExportParticipant(GroupParticipant groupParticipant)
        {
            string participantRep = "Group";
            if (AddChestNumbers) participantRep += $" #{groupParticipant.ChestNumber}";
            if (groupParticipant.Leader != null) participantRep += $" Leader: {groupParticipant.Leader.FullName}";
            Run run = new Run(participantRep);
            run.FontWeight = FontWeights.Bold;
            return new Paragraph(run);
        }


    }
}
