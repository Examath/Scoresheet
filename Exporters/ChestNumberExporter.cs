using CommunityToolkit.Mvvm.ComponentModel;
using Examath.Core.Environment;
using Scoresheet.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Scoresheet.Exporters
{
    public class ChestNumberExporter : FlowDocumentExporter
    {
        public override string Name => "Chest Numbers";

        private string _Footer = "${Level} - ${CompetitionName}\r\n${Organization}";
        /// <summary>
        /// Gets or sets 
        /// </summary>
        public string Footer
        {
            get => _Footer;
            set { if (SetProperty(ref _Footer, value)) UpdatePreview(); }
        }

        private double _ChestNumberFontSize = 72;
        /// <summary>
        /// Gets or sets the font size of the chest number
        /// </summary>
        public double ChestNumberFontSize
        {
            get => _ChestNumberFontSize;
            set { if (SetProperty(ref _ChestNumberFontSize, value)) UpdatePreview(); }
        }

        private double _FooterFontSize = 10;
        /// <summary>
        /// Gets or sets the font size of the footer
        /// </summary>
        public double FooterFontSize
        {
            get => _FooterFontSize;
            set { if (SetProperty(ref _FooterFontSize, value)) UpdatePreview(); }
        }

        public List<IndividualParticipant>? SelectedParticipants { get; set; }

        public ChestNumberExporter(ScoresheetFile scoresheetFile) : base(scoresheetFile) { }

        public override bool Initialise()
        {
            if (base.Initialise() && SelectedParticipants != null && SelectedParticipants.Count > 0)
            {
                return true;
            }
            else
            {
                Messager.Out("Select participants to export", "No participants selected", ConsoleStyle.WarningBlockStyle);
                return false;
            }
        }

        protected override FlowDocument GeneratePreview()
        {
            FlowDocument flowDocument = base.GeneratePreview();

            if (SelectedParticipants != null)
            {
                Style footerStyle = new(typeof(Paragraph));
                footerStyle.Setters.Add(new Setter(TextElement.FontSizeProperty, FooterFontSize));
                flowDocument.Resources.Add(nameof(footerStyle), footerStyle);

                foreach (IndividualParticipant individualParticipant in SelectedParticipants)
                {
                    Paragraph chestNumber = new(new Run(individualParticipant.ChestNumber.ToString()))
                    {
                        FontSize = ChestNumberFontSize,
                        FontWeight = FontWeights.Bold,
                    };
                    flowDocument.Blocks.Add(chestNumber);

                    string footerString = Footer
                        .Replace("${Level}", individualParticipant.Level?.Name ?? "Null Level")
                        .Replace("${CompetitionName}", _ScoresheetFile.CompetitionName)
                        .Replace("${Organization}", _ScoresheetFile.Organization);

                    Paragraph footer = new(new Run(footerString));
                    footer.SetResourceReference(FrameworkElement.StyleProperty, nameof(footerStyle));
                    flowDocument.Blocks.Add(footer);
                }
            }

            return flowDocument;
        }
    }
}
