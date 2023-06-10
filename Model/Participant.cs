using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Scoresheet.Model
{
    /// <summary>
    /// Represents a participant (either <see cref="IndividualParticipant"/> or <see cref="GroupParticipant"/>) in this competition
    /// </summary>
    /// <remarks>
    /// Must be initialized when loading from XML
    /// </remarks>
    public partial class Participant : ObservableObject
    {
        #region Identity

        private int _ChestNumber = 0;
        /// <summary>
        /// Gets or sets the unique chest number of this participant
        /// </summary>
        /// <remarks>
        /// Is 0 if unset
        /// </remarks>
        [XmlAttribute]
        public int ChestNumber
        {
            get => _ChestNumber;
            set => SetProperty(ref _ChestNumber, value);
        }

        private Team? _Team = null;
        /// <summary>
        /// Gets or sets the team of this participant
        /// </summary>
        [XmlIgnore]
        public Team? Team
        {
            get => _Team;
            set { if (SetProperty(ref _Team, value) && value != null) Team_Name = value.Name; }
        }

        [XmlAttribute]
        public string Team_Name { get; set; } = "";

        #endregion

        /// <summary>
        /// Find the participant's <see cref="Team"/> from <paramref name="teams"/>
        /// </summary>
        /// <param name="teams"></param>
        public virtual void Initialize(ScoresheetFile scoresheetFile)
        {
            Team = scoresheetFile.Teams.Find((x) => x.Name == Team_Name);
        }
    }
}
