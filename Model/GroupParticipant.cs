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

        /// <summary>
        /// Gets a comma separated list of chestnumbers of the individualParticipants in this group
        /// </summary>
        [XmlAttribute("Participants")]
        public string IndividualParticipantsXML
        {
            get => string.Join(',', IndividualParticipants.Select((x) => x.ChestNumber));
            set => _IndividualParticipantsFromXML = value;
        }

        /// <summary>
        /// Gets the list of individualParticipants in this group
        /// </summary>
        [XmlIgnore]
        public ObservableCollection<IndividualParticipant> IndividualParticipants { get; private set; } = new();

        /// <summary>
        /// Gets a new line separated list of thee full names of the individualParticipants in this group
        /// </summary>
        [XmlIgnore]
        public string ParticipantString { get => string.Join('\n', IndividualParticipants); }

        private string _LeaderFromXML = "";

        /// <summary>
        /// Gets the chest number of the leader of this group
        /// </summary>
        [XmlAttribute("Leader")]
        public string LeaderXML
        {
            get => Leader?.ChestNumber.ToString() ?? "-1";
            set => _LeaderFromXML = value;
        }

        private IndividualParticipant? _Leader = null;
        /// <summary>
        /// Gets or sets the leader of this group
        /// </summary>
        [XmlIgnore]
        public IndividualParticipant? Leader
        {
            get => _Leader;
            set => SetProperty(ref _Leader, value);
        }

        /// <summary>
        /// Sets <paramref name="participant"/> as the leader of this group
        /// </summary>
        /// <param name="participant"></param>
        [RelayCommand]
        public void SetAsLeader(IndividualParticipant? participant)
        {
            if (participant != null)
            {
                Leader = participant;
            }
        }

        /// <summary>
        /// Removes <paramref name="participant"/> from this group, 
        /// including as leader if <paramref name="participant"/> is <see cref="Leader"/>
        /// </summary>
        /// <param name="participant"></param>
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
        /// Creates an GroupParticipant for deserialisation
        /// </summary>
        /// <remarks>
        /// Must call <see cref="Initialize(ScoresheetFile?)"/> afterward
        /// </remarks>
        public GroupParticipant()
        {

        }

        /// <summary>
        /// Creates a group participant in the specified <paramref name="scoresheetFile"/> with <paramref name="individualParticipants"/>
        /// </summary>
        /// <param name="scoresheetFile"></param>
        /// <param name="individualParticipants"></param>
        public GroupParticipant(ScoresheetFile scoresheetFile, ObservableCollection<IndividualParticipant>? individualParticipants = null)
        {
            _ScoresheetFile = scoresheetFile;
            if (individualParticipants != null) IndividualParticipants = individualParticipants;
            IndividualParticipants.CollectionChanged += IndividualParticipants_CollectionChanged;
        }

        /// <summary>
        /// Initialises the individualParticipants making up this group and their leader
        /// and finally <inheritdoc/>
        /// </summary>
        public override void Initialize(ScoresheetFile scoresheetFile)
        {
            base.Initialize(scoresheetFile);
            IndividualParticipants.CollectionChanged += IndividualParticipants_CollectionChanged;

            // Parse leader
            if(!int.TryParse(_LeaderFromXML, out int leaderChestNumber)) leaderChestNumber = -1;

            // Parse individualParticipants
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

        private void IndividualParticipants_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(ParticipantString));
        }

        public override string ToString()
        {
            return ChestNumber.ToString();
        }
    }
}
