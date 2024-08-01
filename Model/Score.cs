using Scoresheet.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Scoresheet.Model
{
    /// <summary>
    /// Represents a score given to a participant in an item for all judges and all criteria
    /// </summary>
    /// <remarks>
    /// Scores should be created using the constructor, and not edited in ay way (other than by the serializer)
    /// </remarks>
    public class Score : IComparable<Score>
    {
        #region Data
        /// <summary>
        /// Gets the participant this score applies to
        /// </summary>
        [XmlIgnore]
        public Participant? Participant { get; private set; }

        private int _XMLParticipantChestNumber = 0;
        /// <summary>
        /// Gets the participant chest this score applies to
        /// </summary>
        /// <remarks>
        /// For the serializer only
        /// </remarks>
        [XmlAttribute(attributeName: "For")]
        public int ParticipantChestNumber
        {
            get => Participant?.ChestNumber ?? _XMLParticipantChestNumber;
            set => _XMLParticipantChestNumber = value;
        }

        /// <summary>
        /// Gets the matrix of marks. The first dimension is by judge. The second is by criteria.
        /// </summary>
        [XmlIgnore]
        public List<double[]> Marks { get; private set; } = new();

        /// <summary>
        /// Represents the raw marks for a given score as a string format. 
        /// </summary>
        /// <remarks>
        /// Each judges score is separated by a comma (,), as in Scoresheet v1
        /// Each criteria is separated by a '+'
        /// </remarks>
        [XmlAttribute("Marks")]
        public string XMLMarks
        {
            get
            {
                return string.Join(',', Marks.Select(m => string.Join('+', m)));
            }
            set
            {
                string[] markStringsByJudge = value.Split(',');
                foreach (string str in markStringsByJudge)
                {
                    string[] markStrings = str.Split("+");
                    double[] marks = new double[markStrings.Length];
                    for (int i = 0; i < markStrings.Length; i++)
                    {
                        marks[i] = double.Parse(markStrings[i]);
                    }
                    Marks.Add(marks);
                }
            }
        }

        /// <summary>
        /// Gets the author who entered this score
        /// </summary>
        [XmlAttribute]
        public string Author { get; set; } = "";

        /// <summary>
        /// Gets when this score was created and applied
        /// </summary>
        [XmlAttribute]
        public DateTime Created { get; set; } = DateTime.Now;

        #endregion

        #region Init

        public Score()
        {

        }

        public Score(Participant participant, string author)
        {
            Participant = participant;
            ParticipantChestNumber = participant.ChestNumber;
            Author = author;
        }

        public Score(Participant participant, string author, List<double[]> marks):this(participant, author)
        {
            Marks = marks;
        }

        public static Score? TryParse(Participant participant, string markString, string author) 
        {
            Score score = new(participant, author);

            string[] markStringsByJudge = markString.Split(',');

            foreach (string rowString in markStringsByJudge)
            {
                if (string.IsNullOrWhiteSpace(rowString)) continue;

                string[] markStrings = rowString.Split("+");
                double[] marks = new double[markStrings.Length];

                for (int i = 0; i < markStrings.Length; i++)
                {
                    string singleMarkString = markStrings[i].Trim();
                    if (double.TryParse(singleMarkString, out double mark))
                    {
                         marks[i] = mark;
                    }
                    else
                    {
                        return null;
                    }
                }
                score.Marks.Add(marks);
            }

            if (score.Marks.Count == 0) return null;

            return score;
        }

        /// <summary>
        /// Initialises <see cref="Participant"/> from <see cref="ParticipantChestNumber"/>
        /// </summary>
        public void Initialize(IEnumerable<Participant> participants)
        {
            Participant = participants.Where((p) => p.ChestNumber == ParticipantChestNumber).FirstOrDefault();
            if (Participant == null)
            {
                throw new Examath.Core.Model.ObjectLinkingException(this, ParticipantChestNumber, typeof(Participant));
            }
        }

        #endregion

        #region Output

        /// <summary>
        /// Gets the single sum and average of the score
        /// </summary>
        public double AverageMarks { get => Math.Round(Marks.Select(m => m.Sum()).Average(), Settings.Default.MarksPrecision); }

        /// <summary>
        /// Converts the marks matrix to a human-readable string
        /// </summary>
        /// <returns>The marks matrix in the following format: 12 + 34 + 56, 78 + 90 + 12</returns>
        public string MarksToString() => string.Join(",  ", Marks.Select(m => string.Join(" + ", m)));

        /// <returns>The string representation of this score, including the marks, average, place, participant, author and created date.</returns>
        public override string ToString() => $"{MarksToString()} = {AverageMarks} ({Model.Place.AddOrdinal(Place ?? 0)})\nFor #{ParticipantChestNumber} by {Author} at {Created:g}";

        /// <summary>
        /// Compares scores using the <see cref="AverageMarks"/>
        /// </summary>
        /// <param name="other">The other score to compare to</param>
        /// <returns><inheritdoc/></returns>
        public int CompareTo(Score? other)
        {
            if (other == null) return 1;
            else return AverageMarks.CompareTo(other.AverageMarks);
        }

        /// <summary>
        /// Gets the place of this score.
        /// </summary>
        /// <remarks>
        /// This starts at 1
        /// </remarks>
        [XmlIgnore]
        public int? Place { get; set; }

        /// <summary>
        /// Gets whether this score is for <paramref name="participant"/>
        /// </summary>
        /// <param name="participant">
        /// The participant to compare. 
        /// If <paramref name="participant"/> is an <see cref="IndividualParticipant"/> and
        /// <see cref="Participant"/> IS A <see cref="GroupParticipant"/>, then the group participant is checked to see if it contains the <paramref name="participant"/>.
        /// </param>
        /// <returns></returns>
        public bool IsOf(Participant participant)
        {

            if (Participant is GroupParticipant groupParticipant && participant is IndividualParticipant individualParticipant)
            {
                return groupParticipant.IndividualParticipants.Contains(participant);
            }
            else
            {
                return Participant == participant;
            }
        }

        #endregion
    }
}
