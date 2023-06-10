using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Scoresheet.Model
{
    /// <summary>
    /// Represents an individual that may compete in solo or group items
    /// </summary>
    public partial class IndividualParticipant : Participant
    {
        private string _FullName = "";
        /// <summary>
        /// Gets or sets the full name of this person
        /// </summary>
        [XmlAttribute]
        public string FullName
        {
            get => _FullName;
            set => SetProperty(ref _FullName, value);
        }

        /// <summary>
        /// Gets the <see cref="string.ToUpperInvariant"/> form of <see cref="FullName"/>
        /// </summary>
        [XmlIgnore]
        public string SearchName { get; private set; } = "";

        private int _YearLevel = 0;
        /// <summary>
        /// Gets or sets the year level of this person
        /// </summary>
        [XmlAttribute]
        public int YearLevel
        {
            get => _YearLevel;
            set => SetProperty(ref _YearLevel, value);
        }

        /// <summary>
        /// Gets the <see cref="LevelDefinition"/> this individual is in
        /// </summary>
        [XmlIgnore]
        public LevelDefinition? Level { get;set; }

        private DateTime _SubmissionTimeStamp;
        /// <summary>
        /// Gets or sets 
        /// </summary>
        public DateTime SubmissionTimeStamp
        {
            get => _SubmissionTimeStamp;
            set { if (SetProperty(ref _SubmissionTimeStamp, value)) OnPropertyChanged(nameof(IsFormSubmitted)); }
        }

        public bool IsFormSubmitted { get => SubmissionTimeStamp != default; }

        public IndividualParticipant()
        {

        }

        public IndividualParticipant(string[] parameters, int[,] chestNumberMatrix, List<Team> teams, List<LevelDefinition> levelDefinitions)
        {
            _FullName = parameters[0];
            Team_Name = parameters[1];
            if (int.TryParse(parameters[2], out int yearLevel)) 
            {
                _YearLevel = yearLevel;
            };

            Initialize(teams, levelDefinitions);

            // Assign Chest Number
            if (Level != null && Team != null)
            {
                int levelIndex = levelDefinitions.IndexOf(Level);
                int teamIndex = teams.IndexOf(Team);
                ChestNumber = chestNumberMatrix[levelIndex, teamIndex] + 1;
                chestNumberMatrix[levelIndex, teamIndex] = ChestNumber;
            }
        }

        public void Initialize(List<Team> teams, List<LevelDefinition> levelDefinitions)
        {
            Initialize(teams);
            Level = levelDefinitions.Find(x => x.Within(_YearLevel));
            SearchName = _FullName.ToUpperInvariant();
        }

        public override string ToString()
        {
            return $"#{ChestNumber} {FullName}";
        }
    }
}
