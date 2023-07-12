using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Scoresheet.Model
{
    public partial class GroupItem : CompetitionItem
    {
        /// <summary>
        /// Gets or sets the list of <see cref="GroupParticipant"/> participating
        /// in this item
        /// </summary>
        [XmlElement(elementName: "GroupParticipant")]
        public ObservableCollection<GroupParticipant> GroupParticipants { get; set; } = new();

        public override IEnumerable<Participant> Participants => GroupParticipants;
    }
}
