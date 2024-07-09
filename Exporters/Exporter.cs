using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
using System.Windows.Documents;

namespace Scoresheet.Exporters
{
    public  abstract partial class Exporter : ObservableValidator
    {
        #region Constructor

        protected ScoresheetFile _ScoresheetFile;

        public abstract string Name { get; }

        public Exporter(ScoresheetFile scoresheetFile) 
        { 
            _SaveLocation = "C:\\temp\\temp" + ExportFileType().GetFirstExtension();
            _ScoresheetFile = scoresheetFile;
        }

        public abstract bool Initialise();

        #endregion

        #region Preview Document

        private FlowDocument? _Document = null;
        /// <summary>
        /// Gets or sets the flow document for preview
        /// </summary>
        public FlowDocument? Document
        {
            get => _Document;
            set => SetProperty(ref _Document, value);
        }

        #endregion

        #region Export

        protected abstract FileFilter ExportFileType();

        public string FileFilter { get => ExportFileType().ToString(); }

        private string _SaveLocation;
        /// <summary>
        /// Gets or sets the location to export to
        /// </summary>
        public string SaveLocation
        {
            get => _SaveLocation;
            set => SetProperty(ref _SaveLocation, value);
        }

        [RelayCommand]
        protected abstract void Export();

        #endregion

        #region Post Export

        private bool _OpenAfterExport = true;
        /// <summary>
        /// Gets or sets whether the exported file should be opened in the default application after export
        /// </summary>
        public bool OpenAfterExport
        {
            get => _OpenAfterExport;
            set => SetProperty(ref _OpenAfterExport, value);
        }

        protected virtual void PostExport()
        {
            if (OpenAfterExport)
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
                catch (Exception e)
                {
                    Messager.OutException(e, "Opening Document");
                }
            }

            Document = null;

            Exported?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? Exported;

        #endregion
    }
}
