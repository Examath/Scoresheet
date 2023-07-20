using CommunityToolkit.Mvvm.ComponentModel;
using Scoresheet.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        protected ScoresheetFile? _ScoresheetFile;

        #region Details

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
            set => Duration = new TimeSpan(0, value, 0);
        }

        /// <summary>
        /// Gets the <see cref="LevelDefinition"/> of this item, if any.
        /// </summary>
        [XmlIgnore]
        public LevelDefinition Level { get; set; } = LevelDefinition.All;

        #endregion

        #region Participants

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

        #endregion

        #region Scoring

        /// <summary>
        /// Gets or sets a list of scores assigned to this competition item
        /// </summary>
        [XmlElement(ElementName = "Score")]
        public ObservableCollection<Score> Scores { get; set; } = new();

        /// <summary>
        /// Gets the latest newScore assigned by this <see cref="CompetitionItem"/> to this <paramref name="participant"/>
        /// </summary>
        /// <param name="participant">The participant to get assigned scores for</param>
        /// <returns>The latest newScore assigned to this <paramref name="participant"/>, or null if there aren't any</returns>
        public Score? GetIntersection(Participant participant)
        {
            return Scores.Where((s) => s.IsOf(participant)).FirstOrDefault();
        }

        internal void AddScore(Participant participant, List<double> marks, string author)
        {
            RemoveScoresOf(participant);
            Score newScore = new(participant, marks, author);
            Scores.Add(newScore);
            CalculateWinners();
            ScoreChanged?.Invoke(this, new(this, participant, newScore));
        }

        internal void ClearScore(Participant participant)
        {
            RemoveScoresOf(participant);
            CalculateWinners();
            ScoreChanged?.Invoke(this, new(this, participant, null));
        }

        private void RemoveScoresOf(Participant participant)
        {
            List<Score> oldScores = Scores.Where((s) => s.Participant == participant).ToList();
            foreach (Score oldScore in oldScores)
            {
                Scores.Remove(oldScore);
            }
        }

        public event EventHandler<ScoreChangedEventArgs>? ScoreChanged;

        private void CalculateWinners()
        {
            List<Place> winners = new();
            int place = 0;
            double currentMinMark = double.MaxValue;

            foreach (Score score in Scores.OrderDescending())
            {
                if (score.Participant != null)
                {
                    double mark = score.AverageMarks;
                    
                    // New place
                    if (mark < currentMinMark)
                    {
                        place++;
                        currentMinMark = mark;
                        score.Place = place;
                        if (place <= Settings.Default.NumberOfPlaces) winners.Add(new(place));
                    }

                    // Tie
                    if (place <= Settings.Default.NumberOfPlaces) winners.Last().Participants.Add(score.Participant);
                }
            }

            Winners = winners;
        }

        private List<Place> _Winners = new();
        /// <summary>
        /// Gets the top three scoring participants
        /// </summary>
        [XmlIgnore]
        public List<Place> Winners
        {
            get => _Winners;
            private set => SetProperty(ref _Winners, value);
        }

        #endregion

        /// <summary>
        /// Initialises <see cref="Level"/> and <see cref="Scores"/>
        /// </summary>
        public virtual void Initialize(ScoresheetFile scoresheetFile)
        {
            _ScoresheetFile = scoresheetFile;
            string[] codeParameters = Code.Split('/');

            if (codeParameters.Length >= 2)
            {
                Level = _ScoresheetFile.LevelDefinitions.Find((x) => x.Code == codeParameters[1]) ?? LevelDefinition.All;
            }

            foreach (Score score in Scores) score.Initialize(Participants);
            CalculateWinners();
        }

        /// <summary>
        /// Gets the <see cref="ShortCode"/> of this competition item
        /// </summary>
        public override string ToString() => ShortCode;

    }

    public class ScoreChangedEventArgs : EventArgs
    {
        public string Message { get; private set; }

        public ScoreChangedEventArgs(CompetitionItem competitionItem, Participant participant, Score? score)
        {
            if (score != null)
            {
                Message = $"#{participant.ChestNumber} scored {score.AverageMarks} in {competitionItem.ShortCode}";
            }
            else
            {
                Message = $"#{participant.ChestNumber}'s score for {competitionItem.ShortCode} cleared";
            }
        }

        public override string ToString() => Message;
    }
}
