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
        private const string ITEMS_NAME_FIELD = "${Items}";
        private const string PLACE_FIELD = "${Place}";
        private const string GRADE_FIELD = "${Grade}";

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

        public List<string> PlaceEnumeration { get; set; } = new() { "1st Place", "2nd Place", "3rd Place" };

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
                    Text? nameTemplate = body.Descendants<Text>().FirstOrDefault(t => t.Text.Contains(NAME_FIELD));
                    if (nameTemplate == null)
                    {
                        Messager.Out($"{NAME_FIELD} could not be found in template. Check that it exists and formatting has been cleared.", "Template error", ConsoleStyle.FormatBlockStyle);
                        return;
                    }
                    string nameFormat = nameTemplate.Text;

                    // Year
                    Text? yearTemplate = body.Descendants<Text>().FirstOrDefault(t => t.Text.Contains(YEAR_LEVEL_FIELD));
                    string yearFormat = yearTemplate?.Text ?? "";

                    // Items
                    Paragraph? itemTemplate = body.Descendants<Paragraph>().FirstOrDefault(p => p.InnerText.Contains(ITEMS_NAME_FIELD));
                    if (itemTemplate == null)
                    {
                        Messager.Out($"{ITEMS_NAME_FIELD} paragraph not found in template.", "Template error", ConsoleStyle.WarningBlockStyle);
                        return;
                    }

                    // Other Blocks
                    List<OpenXmlElement> templateElements = body.ChildElements.ToList();
                    body.RemoveAllChildren();

                    foreach (CertificateData certificateData in _CertificateData)
                    {
                        // Set Name
                        nameTemplate.Text = nameFormat.Replace(NAME_FIELD, certificateData.IndividualParticipant.FullName);

                        // Set Year
                        if (yearTemplate != null) yearTemplate.Text = yearFormat.Replace(YEAR_LEVEL_FIELD, certificateData.IndividualParticipant.YearLevel.ToString());

                        foreach (var templateElement in templateElements)
                        {
                            if (templateElement != itemTemplate)
                            {
                                OpenXmlElement element = (OpenXmlElement)templateElement.Clone();
                                body.AppendChild(element);
                            }
                            else
                            {
                                foreach (ScoreRecord scoreRecord in certificateData.Items)
                                {
                                    Paragraph item = (Paragraph)(itemTemplate?.Clone() ?? new Paragraph());

                                    Text? itemNameTemplate = item?.Descendants<Text>().FirstOrDefault(t => t.Text.Contains(ITEMS_NAME_FIELD));
                                    if (itemNameTemplate != null) 
                                        itemNameTemplate.Text = itemNameTemplate.Text.Replace(ITEMS_NAME_FIELD, scoreRecord.CompetitionItem.Name);

                                    Text? itemPlaceTemplate = item?.Descendants<Text>().FirstOrDefault(t => t.Text.Contains(PLACE_FIELD));
                                    if (itemPlaceTemplate != null)
                                    {
                                        string placeText = string.Empty;                                    
                                        if (scoreRecord.Place <= PlaceEnumeration.Count)
                                        {
                                            placeText = " (" + PlaceEnumeration[scoreRecord.Place - 1 ?? 0] + ")";
                                        }
                                        itemPlaceTemplate.Text = itemPlaceTemplate.Text.Replace(PLACE_FIELD,placeText);
                                    }                                       

                                    Text? itemGradeTemplate = item?.Descendants<Text>().FirstOrDefault(t => t.Text.Contains(GRADE_FIELD));
                                    if (itemGradeTemplate != null)
                                    {
                                        itemGradeTemplate.Text = itemGradeTemplate.Text.Replace(GRADE_FIELD, scoreRecord.Grade);
                                    }

                                    body.AppendChild(item);
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

