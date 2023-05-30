using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Scoresheet.Model
{
    public class CompetitionItem
    {
        private string _Code;

        [XmlAttribute]
        public ItemType ItemType { get; set; } = ItemType.NonStage;

        [XmlAttribute]
        public string Code
        {
            get => _Code; 
            set
            {
                _Code = value;
                Name = value.Split('/')[0];
            }
        }

        [XmlIgnore]
        public string Name { get; private set; } = "";

        [XmlIgnore]
        public LevelDefinition? Level { get; set; }

        public override string ToString() => Code;
    }
}
