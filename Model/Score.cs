using Scoresheet.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Scoresheet.Model
{
    public class Score : IComparable<Score>
    {
        #region Data
        [XmlIgnore]
        public Participant? Participant { get; private set; }

        private int _XMLParticipantChestNumber = 0;
        [XmlAttribute(attributeName: "For")]
        public int ParticipantChestNumber
        {
            get => Participant?.ChestNumber ?? _XMLParticipantChestNumber;
            set => _XMLParticipantChestNumber = value;
        }

        [XmlIgnore]
        public List<double> Marks { get; private set; } = new();

        [XmlAttribute("Marks")]
        public string XMLMarks
        {
            get
            {
                return string.Join(',', Marks);
            }
            set
            {
                string[] data = value.Split(',');
                foreach (string str in data) Marks.Add(double.Parse(str));
            }
        }

        [XmlAttribute]
        public string Author { get; set; } = "";

        [XmlAttribute]
        public DateTime Created { get; set; } = DateTime.Now;

        #endregion

        #region Init

        public Score()
        {

        }

        public Score(Participant participant, List<double> marks, string author)
        {
            Participant = participant;
            ParticipantChestNumber = participant.ChestNumber;
            Marks = marks;
            Author = author;
        }

        /// <summary>
        /// Initialises <see cref="Participant"/> from <see cref="ParticipantChestNumber"/>
        /// </summary>
        public void Initialize(IEnumerable<Participant> participants)
        {
            Participant = participants.Where((p) => p.ChestNumber == ParticipantChestNumber).FirstOrDefault();
        }

        #endregion

        #region Output

        public double AverageMarks { get => Math.Round(Marks.Average(), Settings.Default.MarksPrecision); }

        public override string ToString() => $"{string.Join(", ", Marks)}\nFor #{ParticipantChestNumber} by {Author} at {Created:g}";

        public int CompareTo(Score? other)
        {
            if (other == null) return 1;
            else return Marks.Average().CompareTo(other.Marks.Average());
        }

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
