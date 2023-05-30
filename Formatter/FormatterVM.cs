using CommunityToolkit.Mvvm.ComponentModel;
using Scoresheet.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scoresheet.Formatter
{
    public partial class FormatterVM : ObservableObject
    {
        public FormatterVM() { }

        private string _GuidelineLocation = "";
        /// <summary>
        /// Gets or sets 
        /// </summary>
        public string GuidelineLocation
        {
            get => _GuidelineLocation;
            set
            {
                if (SetProperty(ref _GuidelineLocation, value)) LoadGuideline();
            }
        }

        private async void LoadGuideline()
        {
            Guideline = await Examath.Core.Utils.XML.TryLoad<Guideline>(_GuidelineLocation);
        }

        private Guideline? _Guideline = null;
        /// <summary>
        /// Gets or sets 
        /// </summary>
        public Guideline? Guideline
        {
            get => _Guideline;
            set => SetProperty(ref _Guideline, value);
        }


        private string _TeamListFileLocation = "";
        /// <summary>
        /// Gets or sets 
        /// </summary>
        public string TeamListFileLocation
        {
            get => _TeamListFileLocation;
            set => SetProperty(ref _TeamListFileLocation, value);
        }
    }
}
