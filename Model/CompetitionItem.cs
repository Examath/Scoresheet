using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public partial class CompetitionItem : ObservableObject
    {
        #region MyRegion

        private string _Code = "";

        /// <summary>
        /// The unique code for this item.
        /// </summary>
        /// <remarks>
        /// Use the format <c>{FullName}/{LevelDefinition.Code}</c>
        /// </remarks>
        [XmlAttribute]
        public string Code
        {
            get => _Code;
            set
            {
                _Code = value;
                string[] parameters = value.Split('/');
                Name = parameters[0];
                foreach (string word in parameters[0].Split(' '))
                {
                    ShortCode += word[..Math.Min(word.Length, 2)];
                };
                if (parameters.Length >= 2)
                {

                    ShortCode += "/" + parameters[1];
                }
            }
        }

        /// <summary>
        /// Gets the name of this item, as specified by <see cref="Code"/>
        /// </summary>
        [XmlIgnore]
        public string Name { get; private set; } = "";

        /// <summary>
        /// Gets a shortened version of the <see cref="Code"/> of this item
        /// </summary>
        [XmlIgnore]
        public string ShortCode { get; private set; } = "";

        /// <summary>
        /// Gets or sets the time limit (excluding changeover) for each attempt at this item
        /// </summary>
        [XmlIgnore]
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Gets or sets the time limit (excluding changeover) for each attempt at this item in whole minutes
        /// </summary>
        /// <remarks>
        /// For XML serialization and deserialization. Use <see cref="Duration"/> for
        /// most cases instead.
        /// </remarks>
        [XmlAttribute("Duration")]
        public int DurationInMinutes
        {
            get => (int)Duration.TotalMinutes;
            set => Duration = new TimeSpan(0,value,0);
        }

        /// <summary>
        /// Gets or sets the list of <see cref="IndividualParticipants"/> participating
        /// in this item
        /// </summary>
        [XmlIgnore]
        public ObservableCollection<IndividualParticipant> IndividualParticipants { get; set; } = new();

        /// <summary>
        /// Gets or sets the participants to be given scores by this item
        /// </summary>
        [XmlIgnore]
        public virtual IEnumerable<Participant> Participants => IndividualParticipants;

        /// <summary>
        /// Gets the <see cref="LevelDefinition"/> of this item, if any.
        /// </summary>
        [XmlIgnore]
        public LevelDefinition Level { get; set; } = LevelDefinition.All;

        #endregion

        /// <summary>
        /// Initialises <see cref="Level"/>
        /// </summary>
        public void Initialize(List<LevelDefinition> levelDefinitions)
        {
            string[] codeParameters = Code.Split('/');

            if (codeParameters.Length >= 2)
            {
                Level = levelDefinitions.Find((x) => x.Code == codeParameters[1]) ?? LevelDefinition.All;
            }
        }

        /// <summary>
        /// Gets the <see cref="ShortCode"/> of this competition item
        /// </summary>
        public override string ToString() => ShortCode;
    }
}
