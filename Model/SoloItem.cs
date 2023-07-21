using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Scoresheet.Model
{
    public class SoloItem : CompetitionItem
    {
        [XmlAttribute]
        public bool IsOnStage { get; set; }

        protected override double[] _PlacePoints => new double[] { 10, 8, 5, 0};
    }
}
