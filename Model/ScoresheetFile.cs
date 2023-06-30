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
    /// Represents the guideline, participant list and scoresheet for a competition
    /// </summary>
    /// <remarks>
    /// Must be initialised when loaded from XML
    /// </remarks>
    [Serializable]
    public partial class ScoresheetFile : ObservableObject
    {
        public const string Extension = ".ssf";

        public bool IsFormatted { get; set; }

        public DateTime LastSavedTime { get; set; } = DateTime.Now;

        public string LastAuthor { get; set; } = "Null";

        private bool _IsOpen;
        /// <summary>
        /// Gets or sets whether this scoresheet is open for adding scores
        /// </summary>
        public bool IsOpen
        {
            get => _IsOpen;
            set => SetProperty(ref _IsOpen, value);
        }

        public bool CanAddScores() => IsOpen;

        /// <summary>
        /// Gets or sets the Hash of parts of this object
        /// saved whenever scores are added
        /// </summary>
        public string Hash { get; set; } = "";

        #region DefinitionObjects

        /// <summary>
        /// Gets or sets the list of <see cref="Team"/>s in this competition
        /// </summary>
        public List<Team> Teams { get; set; } = new();

        /// <summary>
        /// Gets or sets a list of <see cref="LevelDefinition"/> in this competition
        /// </summary>
        public List<LevelDefinition> LevelDefinitions { get; set; } = new();

        /// <summary>
        /// Gets or sets a list of <see cref="CompetitionItem"/>s in this competition
        /// </summary>
        [XmlArrayItem(typeof(SoloItem)), XmlArrayItem(typeof(GroupItem))]
        public List<CompetitionItem> CompetitionItems { get; set; } = new();

        #endregion

        #region Participant List Object

        private List<IndividualParticipant> _IndividualParticipants = new();
        /// <summary>
        /// Gets or sets the list of individual participants
        /// </summary>
        public List<IndividualParticipant> IndividualParticipants
        {
            get => _IndividualParticipants;
            set => SetProperty(ref _IndividualParticipants, value);
        }

        #endregion

        /// <summary>
        /// Call after loading from XML. Matches the correct <see cref="LevelDefinition"/>s to each <see cref="CompetitionItem"/>
        /// </summary>
        public void Initialise()
        {
            foreach (CompetitionItem competitionItem in CompetitionItems) competitionItem.Initialize(LevelDefinitions);
            foreach (IndividualParticipant individualParticipant in IndividualParticipants) individualParticipant.Initialize(this);
        }

        public override string ToString()
        {
            return $"Teams: {string.Join(", ", Teams)}\n" +
                $"Levels: {string.Join(", ", LevelDefinitions)}\n" +
                $"Items: {string.Join(", ", CompetitionItems)}";
        }

        public event EventHandler? Modified;

        internal void OnModified(object? sender = null)
        {
            EventHandler? eventHandler = Modified;

            if (eventHandler != null)
            {
                eventHandler(sender ?? this, EventArgs.Empty);
            }
        }
    }
}
