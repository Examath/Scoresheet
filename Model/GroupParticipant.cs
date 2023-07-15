using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Scoresheet.Model
{
    public partial class GroupParticipant : Participant
    {
        private string _IndividualParticipantsFromXML = "";

        [XmlAttribute("Participants")]
        public string IndividualParticipantsXML
        {
            get => string.Join(',', IndividualParticipants.Select((x) => x.ChestNumber));
            set => _IndividualParticipantsFromXML = value;
        }

        [XmlIgnore]
        public ObservableCollection<IndividualParticipant> IndividualParticipants { get; set; } = new();

        private string _LeaderFromXML = "";

        [XmlAttribute("Participants")]
        public string LeaderXML
        {
            get => Leader.ChestNumber.ToString() ?? "-1";
            set => _LeaderFromXML = value;
        }

        [XmlIgnore]
        private IndividualParticipant? _Leader = null;
        /// <summary>
        /// Gets or sets the leader of this group
        /// </summary>
        public IndividualParticipant? Leader
        {
            get => _Leader;
            set => SetProperty(ref _Leader, value);
        }

        [RelayCommand]
        public void RemoveParticipant(IndividualParticipant? participant)
        {
            if (participant != null)
            {
                if (participant == Leader) Leader = null;
                IndividualParticipants.Remove(participant);
            }
        }

        /// <summary>
        /// Initialises the participants making up this group and their leader
        /// and finally <inheritdoc/>
        /// </summary>
        public override void Initialize(ScoresheetFile scoresheetFile)
        {
            base.Initialize(scoresheetFile);

            // Parse leader
            if(!int.TryParse(_LeaderFromXML, out int leaderChestNumber)) leaderChestNumber = -1;

            // Parse participants
            foreach (string chestNumberString in _IndividualParticipantsFromXML.Split(','))
            {
                if (int.TryParse(chestNumberString, out int chestNumber))
                {
                    IndividualParticipant? individualParticipant = scoresheetFile.IndividualParticipants.Find(p => p.ChestNumber == chestNumber);

                    if (individualParticipant != null) { 
                        IndividualParticipants.Add(individualParticipant);
                        if (individualParticipant.ChestNumber == leaderChestNumber) Leader = individualParticipant;
                    }
                }
            }
        }

    }
}
