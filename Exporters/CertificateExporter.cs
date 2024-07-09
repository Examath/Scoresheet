using CommunityToolkit.Mvvm.ComponentModel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Examath.Core.Environment;
using Examath.Core.Model;
using Scoresheet.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Scoresheet.Exporters
{
    public class CertificateExporter : Exporter
    {
        public override string Name => "Certificates (Based of Word template)";

        private const string NAME_FIELD = "${Name}";
        private const string YEAR_LEVEL_FIELD = "${YearLevel}";
        private const string ITEMS_FIELD = "${Items}";

        /// <summary>
        /// Gets or sets the location of the certificate template
        /// </summary>
        public string TemplateLocation
        {
            get => _ScoresheetFile.TemplateLocation;
            set {
                if (_ScoresheetFile.TemplateLocation != value)
                {
                    _ScoresheetFile.TemplateLocation = value;
                    OnPropertyChanged(nameof(TemplateLocation));
                }             
            }
        }


        public List<IndividualParticipant>? SelectedParticipants { get; set; }

        private List<CertificateData> _CertificateData = new();

        public List<string> PlaceEnumeration { get; set; } = new() { "First Place", "Second Place", "Third Place" };

        public CertificateExporter(ScoresheetFile scoresheetFile) : base(scoresheetFile)
        {

        }

        public override bool Initialise()
        {
            if (SelectedParticipants != null && SelectedParticipants.Count > 0)
            {
                _CertificateData = SelectedParticipants.Select(p => p.GetCertificateData())
                                                           .Where(c => c.Items.Count > 0)
                                                           .OrderBy(p => p.IndividualParticipant.YearLevel)
                                                           .ToList();
                Document = new(new System.Windows.Documents.Paragraph(
                            new System.Windows.Documents.Run("Cannot preview a word document. Export to open in word.")))
                {
                    Foreground = Brushes.Gray,
                };
                Document.Blocks.Add(new System.Windows.Documents.Paragraph(
                    new System.Windows.Documents.Run($"Exporting certificates for {_CertificateData.Count} participants")));

                return true;
            }
            else
            {
                Messager.Out("Select participants to export", "No participants selected", ConsoleStyle.WarningBlockStyle);
                return false;
            }
        }

        protected override void Export()
        {
            try
            {
                using var template = WordprocessingDocument.Open(_ScoresheetFile.TemplateLocation, false);
                using var gen = (WordprocessingDocument)template.Clone(SaveLocation, true);
                if (gen?.MainDocumentPart?.Document.Body is Body body)
                {
                    // Name
                    Text? name = body.Descendants<Text>().FirstOrDefault(t => t.Text.Contains(NAME_FIELD));
                    if (name == null)
                    {
                        Messager.Out($"{NAME_FIELD} could not be found in template. Check that it exists and formatting has been cleared.", "Template error", ConsoleStyle.FormatBlockStyle);
                        return;
                    }
                    string nameFormat = name.Text;

                    // Year
                    Text? year = body.Descendants<Text>().FirstOrDefault(t => t.Text.Contains(YEAR_LEVEL_FIELD));
                    string yearFormat = year?.Text ?? "";

                    // Items
                    Paragraph? itemTemplate = body.Descendants<Paragraph>().FirstOrDefault(p => p.InnerText.Contains(ITEMS_FIELD));
                    if (itemTemplate == null)
                    {
                        Messager.Out($"{ITEMS_FIELD} paragraph not found in template.", "Template error", ConsoleStyle.WarningBlockStyle);
                        return;
                    }

                    ParagraphStyleId? itemStyle = itemTemplate.ParagraphProperties?.ParagraphStyleId;
                    if (itemStyle == null)
                    {
                        Messager.Out($"{ITEMS_FIELD} paragraph does not have a style attached. Define a new paragraph style and assign it to the paragraph", "Template error", ConsoleStyle.WarningBlockStyle);
                        return;
                    }

                    // Other Blocks
                    List<OpenXmlElement> source = body.ChildElements.ToList();
                    body.RemoveAllChildren();

                    foreach (CertificateData certificateData in _CertificateData)
                    {
                        // Set Name
                        name.Text = nameFormat.Replace(NAME_FIELD, certificateData.IndividualParticipant.FullName);

                        // Set Year
                        if (year != null) year.Text = yearFormat.Replace(YEAR_LEVEL_FIELD, certificateData.IndividualParticipant.YearLevel.ToString());

                        foreach (var item in source)
                        {
                            if (item != itemTemplate)
                            {
                                OpenXmlElement element = (OpenXmlElement)item.Clone();
                                body.AppendChild(element);
                            }
                            else
                            {
                                foreach (ScoreRecord scoreRecord in certificateData.Items)
                                {
                                    Paragraph element = body.AppendChild(new Paragraph());
                                    ParagraphProperties paragraphProperties = element.PrependChild(new ParagraphProperties());
                                    paragraphProperties.ParagraphStyleId = new ParagraphStyleId() { Val = itemStyle.Val };

                                    Run run = element.AppendChild(new Run());
                                    Text text = run.AppendChild(new Text(scoreRecord.CompetitionItem.Name));
                                    if (scoreRecord.Place <= PlaceEnumeration.Count)
                                    {
                                        text.Text += $" ({PlaceEnumeration[scoreRecord.Place - 1 ?? 0]})";
                                    }
                                }
                            }
                        }
                    }

                    // Clean up
                    SelectedParticipants?.Clear();

                    PostExport();
                }
                else
                {
                    Messager.Out("Template body is null", "Error reading template", ConsoleStyle.FormatBlockStyle);
                }
            }
            catch (Exception e)
            {
                Messager.OutException(e);
                return;
            }
        }

        protected override FileFilter ExportFileType() => new("Word Document", "*.docx");
    }
}

