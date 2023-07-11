using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scoresheet.Model
{
    public class GroupParticipant : Participant
    {
        public ObservableCollection<IndividualParticipant> IndividualParticipants { get; set; } = new();
    }
}
