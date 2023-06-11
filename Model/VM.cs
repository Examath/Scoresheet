using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;

namespace Scoresheet.Model
{
    public partial class VM : ObservableObject
    {
        #region Properties

        public ScoresheetFile ScoresheetFile { get; set; }

        private string _FileLocation = "";
        /// <summary>
        /// Gets or sets the location this scoresheet should save to
        /// </summary>
        public string FileLocation
        {
            get => _FileLocation;
            set
            {
                if (SetProperty(ref _FileLocation, value))
                {
                    FileName = Path.GetFileNameWithoutExtension(value);
                    OnPropertyChanged(nameof(FileName));
                }
            }
        }

        public string FileName { get; private set; } = "Empty";

        #endregion

        #region Initializers

        public VM()
        {
            ScoresheetFile = new ScoresheetFile();
        }

        public VM(ScoresheetFile scoresheetFile, string fileLocation)
        {
            ScoresheetFile = scoresheetFile;
            _FileLocation = fileLocation;
        }

        #endregion

        #region Saving



        #endregion
    }
}
