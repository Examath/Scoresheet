using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Scoresheet.Model
{
    /// <summary>
    /// Represents a single competition item
    /// </summary>
    /// <remarks>
    /// Must be initialised when loaded from XML
    /// </remarks>
    public class CompetitionItem
    {
        private string _Code = "";

        /// <summary>
        /// The <see cref="ItemType"/> of this item, whether group, solo or non-stage.
        /// </summary>
        [XmlAttribute]
        public ItemType ItemType { get; set; } = ItemType.NonStage;

        /// <summary>
        /// The unique code for this item.
        /// </summary>
        /// <remarks>
        /// Use the format <c>{Name}/{LevelDefinition.Code}</c>
        /// </remarks>
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

        /// <summary>
        /// Gets the name of this item, as specified by <see cref="Code"/>
        /// </summary>
        [XmlIgnore]
        public string Name { get; private set; } = "";

        /// <summary>
        /// Gets the <see cref="LevelDefinition"/> of this item, if any.
        /// </summary>
        [XmlIgnore]
        public LevelDefinition? Level { get; set; }

        /// <summary>
        /// Initialises <see cref="Level"/>
        /// </summary>
        public void Initialize(List<LevelDefinition> levelDefinitions)
        {
            string[] codeParameters = Code.Split('/');
            if (codeParameters.Length >= 1)
            {
                Level = levelDefinitions.Find((x) => x.Code == codeParameters[1]);
            }
        }

        public override string ToString() => Code;
    }
}
