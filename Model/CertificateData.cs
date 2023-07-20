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
        public CompetitionItem CompetitionItem { get; private set; }

        public double Marks { get; private set; }

        public int? Place { get; private set; }

        public ScoreRecord(CompetitionItem competitionItem, Score score)
        {
            CompetitionItem = competitionItem;
            Marks = score.AverageMarks;
            Place = score.Place;
        }
    }
}
