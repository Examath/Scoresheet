using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Scoresheet.Model
{
    public class ScoringCriteria : ObservableObject
    {
        private double _MaximumScore = 1;
        /// <summary>
        /// Gets or sets the maximum score for this criteria
        /// </summary>
        [XmlAttribute]
        public double MaximumScore
        {
            get => _MaximumScore;
            set => SetProperty(ref _MaximumScore, value);
        }

        private string _Description = string.Empty;
        /// <summary>
        /// Gets or sets the description for this criteria
        /// </summary>
        [XmlText]
        public string Description
        {
            get => _Description;
            set => SetProperty(ref _Description, value);
        }


    }
}
