using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Serialization;

namespace Scoresheet.Model
{
    /// <summary>
    /// Represents a Team in the competition
    /// </summary>
    public class Team : ObservableObject
    {
        /// <summary>
        /// Gets or sets the name of the team
        /// </summary>
        [XmlAttribute]
        public string Name { get; set; } = ""; 
        
        private Color _Colour;
        /// <summary>
        /// Gets or sets the highlight colour of this project
        /// </summary>
        [XmlIgnore]
        public Color Colour
        {
            get => _Colour;
            set => _Colour = value;
        }

        private double _Points = 0;
        /// <summary>
        /// Gets or sets 
        /// </summary>
        [XmlIgnore]
        public double Points
        {
            get => _Points;
            set => SetProperty(ref _Points, value);
        }

        /// <summary>
        /// Gets or sets the highlight colour of this project as a four channel hexadecimal string
        /// </summary>
        [XmlAttribute(AttributeName = "Colour")]
        public string HexColour
        {
            get
            {
                return _Colour.ToString();
            }
            set
            {
                _Colour = (Color)ColorConverter.ConvertFromString(value);
            }
        }

        /// <summary>
        /// Returns the name of the team
        /// </summary>
        public override string ToString() => Name;
    }
}
