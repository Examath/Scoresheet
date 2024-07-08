using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Scoresheet.Model
{
    /// <summary>
    /// Represents an item entered into by <see cref="IndividualParticipant"/>s
    /// </summary>
    public class SoloItem : CompetitionItem
    {
        protected override double[] _PlacePoints => new double[] { 10, 8, 5, 0 };
    }
}
