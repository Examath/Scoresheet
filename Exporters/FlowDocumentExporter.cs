using CommunityToolkit.Mvvm.ComponentModel;
using Examath.Core.Environment;
using Examath.Core.Model;
using Scoresheet.Model;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.ComponentModel.DataAnnotations;

namespace Scoresheet.Exporters
{
    public abstract class FlowDocumentExporter : Exporter
    {
        public FlowDocumentExporter(ScoresheetFile scoresheetFile) : base(scoresheetFile) 
        {
            // Magic numbered to A4
            _PaperSize = PaperSizes[1];
        }

        public override bool Initialise()
        {
            UpdatePreview();
            return true;
        }

        protected void UpdatePreview()
        {
            Document = GeneratePreview();
        }

        #region Preview properties

        public List<PaperSize> PaperSizes { get; private set; } = new()
        {
             new("A5", 148, 210),
             new("A4", 210, 297),
             new("A3", 297, 420),
        };

        private PaperSize _PaperSize;
        /// <summary>
        /// Gets or sets the paper size of the flow document
        /// </summary>
        public PaperSize PaperSize
        {
            get => _PaperSize;
            set { if (SetProperty(ref _PaperSize, value)) ApplyPreviewProperties(Document); }
        }

        private int _Columns = 1;
        /// <summary>
        /// Gets or sets the number of columns (or atleast tries to)
        /// </summary>
        [Range(1,5,ErrorMessage = "Not a valid number of columns")]
        public int Columns
        {
            get => _Columns;
            set { if (SetProperty(ref _Columns, value)) ApplyPreviewProperties(Document); }
        }

        private FontFamily _FontFamily = new("Arial");
        /// <summary>
        /// Gets or sets the font
        /// </summary>
        public FontFamily FontFamily
        {
            get => _FontFamily;
            set { if (SetProperty(ref _FontFamily, value)) ApplyPreviewProperties(Document); }
        }

        private void ApplyPreviewProperties(FlowDocument? flowDocument)
        {
            const double mm_TO_px = 96.0 / 25.4;

            if (flowDocument != null)
            {
                flowDocument.PageWidth = PaperSize.Width * mm_TO_px;
                flowDocument.PageHeight = PaperSize.Height * mm_TO_px;

                flowDocument.ColumnWidth = (flowDocument.PageWidth) / (Columns + 1);
            }
        }

        #endregion

        protected virtual FlowDocument GeneratePreview()
        {
            FlowDocument flowDocument = new(new Paragraph(new Run("Generated at " + DateTime.Now.ToString())))
            {
                Background = Brushes.White,
                FontFamily = FontFamily,
            };

            ApplyPreviewProperties(flowDocument);

            return flowDocument;
        }

        protected override FileFilter ExportFileType() => new("Rich text format", "*.rtf");

        protected override void Export()
        {
            if (Document != null)
            {
                try
                {
                    using FileStream fileStream = new(SaveLocation, FileMode.Create);
                    TextRange textRange = new(Document.ContentStart, Document.ContentEnd);
                    textRange.Save(fileStream, System.Windows.DataFormats.Rtf);
                    PostExport();
                }
                catch (Exception ee)
                {
                    Messager.OutException(ee, "Saving Document");
                    return;
                }
            }
            else
            {
                Messager.Out("The document did not generate for some reason. Please report this error.", "Cannot Export", ConsoleStyle.FormatBlockStyle);
            }
        }
    }
}
