﻿using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Xml.Serialization;

namespace Scoresheet.Model
{
    /// <summary>
    /// Represents an individual that may compete in solo or group items
    /// </summary>
    public partial class IndividualParticipant : Participant
    {
        #region Personal Information

        private string _FullName = "";
        /// <summary>
        /// Gets or sets the full name of this person
        /// </summary>
        [XmlAttribute]
        [Required()]
        public string FullName
        {
            get => _FullName;
            set
            {
                if (SetProperty(ref _FullName, value, true))
                {
                    SearchName = _FullName.ToUpperInvariant();
                }
            }
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
        [Range(1,12)]
        public int YearLevel
        {
            get => _YearLevel;
            set
            {
                if (SetProperty(ref _YearLevel, value, true)) InitializeLevel();
            }
        }

        /// <summary>
        /// Gets the <see cref="LevelDefinition"/> this individual is in
        /// </summary>
        private LevelDefinition? _Level;
        /// <summary>
        /// Gets or sets 
        /// </summary>
        [XmlIgnore]
        public LevelDefinition? Level
        {
            get => _Level;
            set => SetProperty(ref _Level, value);
        }


        #endregion

        #region Submission Evidence

        private DateTime _SubmissionTimeStamp;
        /// <summary>
        /// Gets or sets the time when a <see cref="Formatter.FormSubmission"/>
        /// was applied
        /// </summary>
        [XmlAttribute]
        public DateTime SubmissionTimeStamp
        {
            get => _SubmissionTimeStamp;
            set { if (SetProperty(ref _SubmissionTimeStamp, value)) OnPropertyChanged(nameof(IsFormSubmitted)); }
        }

        /// <summary>
        /// Gets or sets whether any form has been applied
        /// </summary>
        [XmlIgnore]
        public bool IsFormSubmitted { get => SubmissionTimeStamp != default; }

        private string _SubmissionEmail = "";
        /// <summary>
        /// Gets or sets the email used to submit the <see cref="Formatter.FormSubmission"/>
        /// </summary>
        [XmlAttribute]
        public string SubmissionEmail
        {
            get => _SubmissionEmail;
            set => SetProperty(ref _SubmissionEmail, value);
        }

        private string _SubmissionName = "";
        /// <summary>
        /// Gets or sets the name in the form used for submission
        /// </summary>
        [XmlAttribute]
        public string SubmissionFullName
        {
            get => _SubmissionName;
            set => SetProperty(ref _SubmissionName, value);
        }

        [XmlElement("SubmissionFullName")]
        public string SFN { get => ""; set { if(!string.IsNullOrEmpty(value)) SubmissionFullName = value; } }

        #endregion

        #region Competition items

        private ObservableCollection<CompetitionItem> _CompetitionItems = new();
        /// <summary>
        /// Gets or sets the lsit of <see cref="CompetitionItem"/>s
        /// that this participant participates in
        /// </summary>
        [XmlIgnore]
        public ObservableCollection<CompetitionItem> CompetitionItems
        {
            get => _CompetitionItems;
            set => SetProperty(ref _CompetitionItems, value);
        }

        private string _CompetitionItemsFromXML = "";

        /// <summary>
        /// Gets a comma-separated list of the codes of every <see cref="CompetitionItem"/>
        /// this <see cref="IndividualParticipant"/> participates in
        /// </summary>
        [XmlAttribute("CompetitionItems")]
        public string CompetitionItemsXML
        {
            get => string.Join(',', CompetitionItems.Select((x) => x.Code));
            set => _CompetitionItemsFromXML = value;
        }

        /// <summary>
        /// Adds <paramref name="competitionItem"/> to this participants <see cref="CompetitionItems"/> list
        /// and adds this to the <see cref="CompetitionItem.IndividualParticipants"/>
        /// </summary>
        public void JoinCompetition(CompetitionItem competitionItem)
        {
            CompetitionItems.Add(competitionItem);
            competitionItem.IndividualParticipants.Add(this);
        }

        /// <summary>
        /// Removes <paramref name="competitionItem"/> from this participants <see cref="CompetitionItems"/> list
        /// and removes this from the <see cref="CompetitionItem.IndividualParticipants"/>
        /// </summary>
        public void UnjoinCompetition(CompetitionItem competitionItem)
        {
            CompetitionItems.Remove(competitionItem);
            competitionItem.IndividualParticipants.Remove(this);
        }

        /// <summary>
        /// Initialises the <see cref="CompetitionItems"/> list
        /// </summary>
        /// <param name="codes">Codes for each unique <see cref="CompetitionItem"/></param>
        /// <exception cref="InvalidOperationException">If <see cref="Level"/> is null</exception>
        public void JoinCompetitions(string[] codes, bool appendLevelToCode = false)
        {
            if (Level == null || _ScoresheetFile == null) throw new InvalidOperationException("Level or Scoresheet parent is null");
            // join from either .ssf cross-link codes or .csv data
            foreach (string code in codes)
            {
                if (string.IsNullOrEmpty(code)) continue;
                string lvlCode = (appendLevelToCode) ? code + "/" + Level.Code : code; // Codes in .csv don't have level abbreviations
                CompetitionItem? competitionItem = _ScoresheetFile.CompetitionItems.Find((x) => x.Code == lvlCode);
                if (competitionItem != null) JoinCompetition(competitionItem);
            }
        }

        /// <summary>
        /// Resets and <see cref="UnjoinCompetition(CompetitionItem)"/> from all competitions
        /// </summary>
        public void UnjoinAllCompetitions()
        {
            foreach (CompetitionItem competitionItem in CompetitionItems) competitionItem.IndividualParticipants.Remove(this);
            CompetitionItems.Clear();
        }

        #endregion

        #region Constructor

        public IndividualParticipant()
        {

        }

        public IndividualParticipant(string[] parameters, int[,] chestNumberMatrix, ScoresheetFile scoresheetFile)
        {
            FullName = parameters[0];
            Team_Name = parameters[1];
            if (int.TryParse(parameters[2], out int yearLevel))
            {
                YearLevel = yearLevel;
            };

            Initialize(scoresheetFile);

            // Assign Chest Number
            if (Level != null && Team != null)
            {
                int levelIndex = scoresheetFile.LevelDefinitions.IndexOf(Level);
                int teamIndex = scoresheetFile.Teams.IndexOf(Team);
                ChestNumber = chestNumberMatrix[levelIndex, teamIndex] + 1;
                chestNumberMatrix[levelIndex, teamIndex] = ChestNumber;
            }
        }

        /// <summary>
        /// <inheritdoc/> then finds the matching <see cref="Level"/> and joins needed competitions
        /// </summary>
        /// <param name="scoresheetFile"></param>
        public override void Initialize(ScoresheetFile scoresheetFile)
        {
            base.Initialize(scoresheetFile);
            InitializeLevel();
            JoinCompetitions(_CompetitionItemsFromXML.Split(','));
        }

        private void InitializeLevel()
        {
            Level = _ScoresheetFile?.LevelDefinitions.Find(x => x.Within(_YearLevel));
        }

        #endregion

        public override string ToString()
        {
            return $"#{ChestNumber} {FullName}";
        }
    }
}
