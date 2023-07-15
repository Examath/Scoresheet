using CommunityToolkit.Mvvm.ComponentModel;
using Examath.Core.Environment;
using System;
using System.Collections.Generic;
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
        public async Task InitialiseAsync()
        {
            ProgressWindowTask participantInit = new("Participants", IndividualParticipants.Count);
            ProgressWindowTask competitionItemInit = new("Competition Items", CompetitionItems.Count);

            ProgressWindow progressWindow = new(participantInit, competitionItemInit);
            progressWindow.Show();

            foreach (IndividualParticipant individualParticipant in IndividualParticipants) 
            { 
                await Task.Run(() => individualParticipant.Initialize(this));
                participantInit.Increment();
            }

            foreach (CompetitionItem competitionItem in CompetitionItems)
            {
                await Task.Run(() => competitionItem.Initialize(this));
                competitionItem.ScoreAdded += CompetitionItem_ScoreAdded;
                competitionItemInit.Increment();
            }

            UpdateTeamTotals();

            await Task.Delay(500);
            progressWindow.Close();
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

        #region ChestNumbers

        // This should always be a power of 10
        public const int CATEGORY_CAPACITY = 100;

        /// <summary>
        /// Returns the valid starting index for chest numbers given the <paramref name="levelIndex"/> and <paramref name="teamIndex"/>
        /// </summary>
        /// <param name="levelIndex">The ID of the level</param>
        /// <param name="teamIndex">The ID of the team</param>
        /// <param name="teamCount">The number of teams in the competition</param>
        /// <returns>
        ///     <c>
        ///         (<paramref name="levelIndex"/> * <paramref name="teamCount"/> + <paramref name="teamIndex"/>) * <see cref="CATEGORY_CAPACITY"/>
        ///     </c>
        /// </returns>
        public static int GetChessNumberBase(int levelIndex, int teamIndex, int teamCount) => (levelIndex * teamCount + teamIndex + 1) * CATEGORY_CAPACITY;

        /// <summary>
        /// Returns the valid starting index for chest numbers given the <paramref name="level"/> and <paramref name="team"/>
        /// </summary>
        /// <param name="level">The level</param>
        /// <param name="team">The team</param>
        /// <returns><inheritdoc cref="GetChessNumberBase(int, int, int)"/></returns>
        public int GetChessNumberBase(LevelDefinition? level, Team? team)
        {
            int levelIndex = (level != null) ? LevelDefinitions.IndexOf(level) : LevelDefinitions.Count;
            if (levelIndex == -1) levelIndex = LevelDefinitions.Count;

            int teamIndex = (team != null) ? Teams.IndexOf(team) : 0;
            if (teamIndex == -1) teamIndex = 0;

            return GetChessNumberBase(levelIndex, teamIndex, Teams.Count);
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
