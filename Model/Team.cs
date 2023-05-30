using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Scoresheet.Model
{
    public class Team
    {
        [XmlAttribute]
        public string Name { get; set; }

        public override string ToString() => Name;
    }
}
