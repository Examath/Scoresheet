using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using static System.Formats.Asn1.AsnWriter;

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

        private readonly double[] PlacePoints = { 50, 20, 10 };

        public string LastAuthor { get; set; } = "Null";

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
            foreach (IndividualParticipant individualParticipant in IndividualParticipants) individualParticipant.Initialize(this);
            foreach (CompetitionItem competitionItem in CompetitionItems)
            {
                competitionItem.Initialize(LevelDefinitions);
                competitionItem.ScoreAdded += CompetitionItem_ScoreAdded;
            }
            UpdateTeamTotals();
        }

        #region Scoring

        public event EventHandler<ScoreAddedEventArgs>? ScoreAdded;

        private void CompetitionItem_ScoreAdded(object? sender, ScoreAddedEventArgs e)
        {
            UpdateTeamTotals();
            ScoreAdded?.Invoke(this, e);
        }

        private void UpdateTeamTotals()
        {
            foreach (CompetitionItem competitionItem in CompetitionItems)
            {
                foreach (Place place in competitionItem.Winners)
                {
                    foreach (Participant participant in place.Participants)
                    {
                        if (participant.Team != null) participant.Team.PointsTray += PlacePoints[Math.Min(place.ValueInt - 1, PlacePoints.Length)];
                    }
                }
            }

            foreach (Team team in Teams) team.SetPoints();
        }

        #endregion

        public event EventHandler? Modified;

        internal void OnModified(object? sender = null)
        {
            Modified?.Invoke(sender ?? this, EventArgs.Empty);
        }

        public override string ToString()
        {
            return $"Teams: {string.Join(", ", Teams)}\n" +
                $"Levels: {string.Join(", ", LevelDefinitions)}\n" +
                $"Items: {string.Join(", ", CompetitionItems)}";
        }
    }
}
