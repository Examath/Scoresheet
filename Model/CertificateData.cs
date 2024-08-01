using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scoresheet.Model
{
    internal class CertificateData
    {
        public IndividualParticipant IndividualParticipant { get; private set; }

        public List<ScoreRecord> Items { get; set; } = new();

        public CertificateData(IndividualParticipant individualParticipant)
        {
            IndividualParticipant = individualParticipant;
        }
    }

    internal class ScoreRecord
    {
        /// <summary>
        /// gets the competition item this score was gained for
        /// </summary>
        public CompetitionItem CompetitionItem { get; private set; }

        /// <summary>
        /// <inheritdoc cref="Score.AverageMarks"/>
        /// </summary>
        public double Marks { get; private set; }

        /// <summary>
        /// <inheritdoc cref="Score.Place"/>
        /// </summary>
        /// <remarks>
        /// <inheritdoc cref="Score.Place"/>
        /// </remarks>
        public int? Place { get; private set; }

        /// <summary>
        /// Gets the grade (! not fully implemented)
        /// </summary>
        public string Grade { get; private set; }

        public ScoreRecord(CompetitionItem competitionItem, Score score)
        {
            CompetitionItem = competitionItem;
            Marks = score.AverageMarks;
            Place = score.Place;

            double relativeScore = Marks / competitionItem.MaximumScore;

            if (relativeScore >= 0.8) Grade = "A";
            else if (relativeScore >= 0.6) Grade = "B";
            else Grade = "C";
        }
    }
}
