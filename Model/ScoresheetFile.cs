﻿using CommunityToolkit.Mvvm.ComponentModel;
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
        public const string EXTENSION = ".ssf";

        #region Basic Properties

        [XmlAttribute]
        public double Version = 2.0;

        /// <summary>
        /// Gets or sets whether this Scoresheet File is locked for scoring
        /// </summary>
        [XmlAttribute]
        public bool IsScoring { get; set; } = false;

        [XmlAttribute]
        public DateTime LastSavedTime { get; set; } = DateTime.Now;

        [XmlAttribute]
        public string LastAuthor { get; set; } = "Null";

        private string _CompetitionName = "Untitled";
        /// <summary>
        /// Gets or sets the name of the competition
        /// </summary>
        [XmlAttribute]
        public string CompetitionName
        {
            get => _CompetitionName;
            set { if (SetProperty(ref _CompetitionName, value)) NotifyChange("Comp Name"); }
        }


        private string _Organization = "";
        /// <summary>
        /// Gets or sets the organisation name
        /// </summary>
        [XmlAttribute]
        public string Organization
        {
            get => _Organization;
            set { if (SetProperty(ref _Organization, value)) NotifyChange("Org Name"); }
        }

        #endregion

        #region Exports

        private string _TemplateLocation =  "C:\\temp\\doc.docx";
        /// <summary>
        /// Gets or sets the template location
        /// </summary>
        public string TemplateLocation
        {
            get => _TemplateLocation;
            set => SetProperty(ref _TemplateLocation, value);
        }

        #endregion

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

        #region Init

        /// <summary>
        /// Call after loading from XML. Matches the correct <see cref="LevelDefinition"/>s to each <see cref="CompetitionItem"/>
        /// </summary>
        public async Task InitialiseAsync()
        {
            ProgressWindowTask participantInit = new("Participants", IndividualParticipants.Count);
            ProgressWindowTask competitionItemInit = new("Competition Items", CompetitionItems.Count);

            ProgressWindow progressWindow = new(participantInit, competitionItemInit);
            progressWindow.Show();

            try
            {
                foreach (IndividualParticipant individualParticipant in IndividualParticipants)
                {
                    await Task.Run(() => individualParticipant.Initialize(this));
                    participantInit.Increment();
                }

                foreach (CompetitionItem competitionItem in CompetitionItems)
                {
                    await Task.Run(() => competitionItem.Initialize(this));
                    competitionItem.ScoreChanged += CompetitionItem_ScoreChanged;
                    competitionItemInit.Increment();
                }
            }
            catch (Exception e)
            {
                Messager.OutException(e, "Initializing Scoresheet");
                throw;
            }

            UpdateTeamTotals();

            await Task.Delay(500);
            progressWindow.Close();
        }

        #endregion

        #region Scoring

        private bool _IsPointsCalculatedByWinners = false;
        /// <summary>
        /// Gets or sets whether to award points to teams based on the winners, as opposed to direct marks-to-points
        /// </summary>
        [XmlAttribute]
        public bool IsPointsCalculatedByWinners
        {
            get => _IsPointsCalculatedByWinners;
            set { if (SetProperty(ref _IsPointsCalculatedByWinners, value)) RefreshScore(); }
        }


        private double _GroupParticipantScoreWeight = 40;
        /// <summary>
        /// Gets or sets the factor to multiply a score obtained by a group participant to get team points
        /// </summary>
        [XmlAttribute]
        public double GroupParticipantScoreWeight
        {
            get => _GroupParticipantScoreWeight;
            set { if (SetProperty(ref _GroupParticipantScoreWeight, value) && !IsPointsCalculatedByWinners) RefreshScore(); }
        }

        private double _IndividualParticipantScoreWeight = 10;
        /// <summary>
        /// Gets or sets the factor to multiply a score obtained by an individual participant to get team points
        /// </summary>
        [XmlAttribute]
        public double IndividualParticipantScoreWeight
        {
            get => _IndividualParticipantScoreWeight;
            set { if (SetProperty(ref _IndividualParticipantScoreWeight, value) && !IsPointsCalculatedByWinners) RefreshScore(); }
        }

        public event EventHandler<ScoreChangedEventArgs>? ScoreChanged;

        private void CompetitionItem_ScoreChanged(object? sender, ScoreChangedEventArgs e)
        {
            UpdateTeamTotals();
            ScoreChanged?.Invoke(this, e);
        }

        public void RefreshScore()
        {
            foreach (CompetitionItem competitionItem in CompetitionItems)
            {
                competitionItem.ReCalculateWinners();
            }

            UpdateTeamTotals();
        }

        public void UpdateTeamTotals()
        {
            double[] points = new double[Teams.Count];

            foreach (CompetitionItem competitionItem in CompetitionItems)
            {
                if (competitionItem.PointsRoundUp != null)
                {
                    for (int i = 0; i < competitionItem.PointsRoundUp.Length; i++)
                    {
                        points[i] += competitionItem.PointsRoundUp[i];
                    }
                }
            }

            for (int i = 0; i < Teams.Count; i++)
            {
                Teams[i].Points = points[i];
            }
        }

        #endregion

        #region ChestNumbers

        // The number of chest numbers per category
        public const int CATEGORY_CAPACITY = 50;
        public const int CHEST_NUMBER_START = 100;

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
        public static int GetChessNumberBase(int levelIndex, int teamIndex, int teamCount) => CHEST_NUMBER_START + (levelIndex * teamCount + teamIndex) * CATEGORY_CAPACITY;

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

        /// <summary>
        /// Returns a unique chest number for a group given the <paramref name="team"/>
        /// </summary>
        /// <param name="team">The team</param>
        /// <returns><inheritdoc cref="GetChessNumberBase(int, int, int)"/></returns>
        public int GetNextGroupChessNumber(Team? team)
        {
            int teamIndex = (team != null) ? Teams.IndexOf(team) : 0;
            if (teamIndex == -1) teamIndex = 0;

            int chestNumber = GetChessNumberBase(LevelDefinitions.Count, teamIndex, Teams.Count) + 1;

            foreach (CompetitionItem competitionItem in CompetitionItems)
            {
                if (competitionItem is GroupItem groupItem)
                {
                    foreach (GroupParticipant groupParticipant in groupItem.GroupParticipants)
                    {
                        if (groupParticipant.Team == team && groupParticipant.ChestNumber >= chestNumber)
                        {
                            chestNumber = groupParticipant.ChestNumber + 1;
                        }
                    }
                }
            }

            return chestNumber;
        }

        #endregion

        public event EventHandler? Modified;

        internal void NotifyChange(object? sender = null)
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
