using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Scoresheet.Model
{

    public class LevelDefinition
    {
        [XmlAttribute]
        public int LowerBound { get; set; } = 0;

        [XmlAttribute]
        public int UpperBound { get; set; } = 0;

        [XmlAttribute]
        public string Code { get; set; } = string.Empty;

        [XmlAttribute]
        public string Name { get;set; } = string.Empty;

        public override string ToString() => Name;
    }
}
