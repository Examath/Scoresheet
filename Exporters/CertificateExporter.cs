using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Examath.Core.Environment;
using Scoresheet.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scoresheet.Exporters
{
    public class CertificateExporter
    {
        private const string NAME_FIELD = "${Name}";
        private const string ITEMS_FIELD = "${Items}";

        private ScoresheetFile _ScoresheetFile { get; set; }

        private FilePickerInput TemplateLocationI;

        public string SaveLocation { get; set; } = "C:\\temp\\doc.docx";
        private FilePickerInput SaveLocationI;

        public bool OpenAutomatically { get; set; } = true;
        private CheckBoxInput OpenAutomaticallyI;

        private AskerOptions _AskerOptions = new("Export Certificates", canCancel: true);

        public CertificateExporter(ScoresheetFile scoresheetFile)
        {
            _ScoresheetFile = scoresheetFile;
            TemplateLocationI = new(scoresheetFile, nameof(scoresheetFile.TemplateLocation), "Template to use")
            {
                ExtensionFilter = "Word Document (*.docx)|*.docx;*.dotx",
                HelpText = $"Select a Word document with the generation code to use as a template for the certificates.",
            };
            SaveLocationI = new(this, nameof(SaveLocation), "Location to Export to") { ExtensionFilter = "Word Document (.docx)|*.docx", UseSaveFileDialog = true };
            OpenAutomaticallyI = new(this, nameof(OpenAutomatically), "Open file when complete");

            DocumentFormat.OpenXml.Int16Value i = new();
        }

        public async Task Export(List<IndividualParticipant> selectedParticipants)
        {
            List<CertificateData> certificates = await Task.Run(() => selectedParticipants.Select(p => p.GetCertificateData()).Where(c => c.Items.Count > 0).ToList());

            AskerNote askerNote = new($"Export certificates for {certificates.Count} participants");

            if (Asker.Show(_AskerOptions, askerNote, TemplateLocationI, SaveLocationI, OpenAutomaticallyI))
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

                        foreach (CertificateData certificateData in certificates)
                        {
                            // Set Name
                            name.Text = nameFormat.Replace(NAME_FIELD, certificateData.IndividualParticipant.FullName);
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
                                        paragraphProperties.ParagraphStyleId = new ParagraphStyleId() { Val = itemStyle.Val};

                                        Run run = element.AppendChild(new Run());
                                        Text text = run.AppendChild(new Text(scoreRecord.CompetitionItem.Name));
                                        if (scoreRecord.Place <= Scoresheet.Properties.Settings.Default.NumberOfPlaces)
                                        {
                                            text.Text += $" ({Place.AddOrdinal(scoreRecord.Place ?? 0)})";
                                        }
                                    }
                                }
                            }
                        }
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
