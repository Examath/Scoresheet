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
        public string TemplateLocation { get; set; } = "C:\\temp\\doc.docx";
        private FilePickerInput TemplateLocationI;

        public string SaveLocation { get; set; } = "C:\\temp\\doc.docx";
        private FilePickerInput SaveLocationI;

        public bool OpenAutomatically { get; set; } = true;
        private CheckBoxInput OpenAutomaticallyI;

        private AskerOptions _AskerOptions = new("Export Certificates", canCancel: true);

        public CertificateExporter(ScoresheetFile scoresheetFile)
        {
            _ScoresheetFile = scoresheetFile;
            TemplateLocationI = new(this, nameof(TemplateLocation), "Template to use")
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
                    using var template = WordprocessingDocument.Open(TemplateLocation, false);
                    using var gen = (WordprocessingDocument)template.Clone(SaveLocation, true);
                    if (gen?.MainDocumentPart?.Document.Body is Body body &&
                        body.Descendants<Text>().FirstOrDefault(t => t.Text.Contains(NAME_FIELD)) is Text name &&
                        body.Descendants<Paragraph>().FirstOrDefault(p => p.InnerText.Contains(ITEMS_FIELD)) is Paragraph itemTemplate)
                    {
                        string nameFormat = name.Text;
                        itemTemplate.RemoveAllChildren();
                        List<OpenXmlElement> source = body.ChildElements.ToList();
                        body.RemoveAllChildren();
                        foreach (CertificateData certificateData in certificates)
                        {
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
                                        Paragraph element = (Paragraph)itemTemplate.Clone();
                                        body.AppendChild(element);
                                        Run run = element.AppendChild(new Run());
                                        run.AppendChild(new Text(scoreRecord.CompetitionItem.Name));
                                        if (scoreRecord.Place <= Scoresheet.Properties.Settings.Default.NumberOfPlaces)
                                        {
                                            Run plrun = element.AppendChild(new Run());
                                            plrun.AppendChild(new Text($" ({Place.AddOrdinal(scoreRecord.Place ?? 0)})"));
                                        }
                                    }
                                }
                            }
                        }

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
