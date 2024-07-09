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
    public sealed class ParticipantListExporter : FlowDocumentExporter
    {
        public override string Name { get => "Participant-Items List"; }

        private bool _DisplayGroupLeader = true;
        /// <summary>
        /// Gets or sets whether to Show Group Leader
        /// </summary>
        public bool DisplayGroupLeader
        {
            get => _DisplayGroupLeader;
            set { if (SetProperty(ref _DisplayGroupLeader, value)) UpdatePreview(); }
        }

        private bool _AddChestNumbers = false;
        /// <summary>
        /// Gets or sets whether to add chest numbers
        /// </summary>
        public bool AddChestNumbers
        {
            get => _AddChestNumbers;
            set { if (SetProperty(ref _AddChestNumbers, value)) UpdatePreview(); }
        }

        public ParticipantListExporter():base(new())
        {

        }

        public ParticipantListExporter(ScoresheetFile scoresheetFile):base(scoresheetFile)
        {
            
        }

        protected override FlowDocument GeneratePreview()
        {
            FlowDocument flowDocument = base.GeneratePreview();

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

                double tableWidth = flowDocument.PageWidth / Columns - 200;

                double tableColumnWidth = tableWidth / _ScoresheetFile.Teams.Count;

                foreach (Team team in _ScoresheetFile.Teams)
                {
                    table.Columns.Add(new TableColumn() { Width = new GridLength(tableColumnWidth) });
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
                        Run note = new($"Not in any group: {string.Join(", ", notInGroup.Select(p => (AddChestNumbers) ? p.ToString() : p.FullName).ToList())}")
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

            return flowDocument;
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
            if (groupParticipant.Leader != null && DisplayGroupLeader) participantRep += $" Leader: {groupParticipant.Leader.FullName}";
            Run run = new Run(participantRep);
            run.FontWeight = FontWeights.Bold;
            return new Paragraph(run);
        }
    }
}
