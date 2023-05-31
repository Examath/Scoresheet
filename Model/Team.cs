using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Scoresheet.Model
{
    /// <summary>
    /// Represents a Team in the competition
    /// </summary>
    public class Team
    {
        /// <summary>
        /// Gets or sets the name of the team
        /// </summary>
        [XmlAttribute]
        public string Name { get; set; } = "";

        /// <summary>
        /// Returns the name of the team
        /// </summary>
        public override string ToString() => Name;
    }
}
