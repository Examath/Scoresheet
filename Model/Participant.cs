﻿using CommunityToolkit.Mvvm.ComponentModel;
using Examath.Core.Utils;
using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Scoresheet.Model
{
    /// <summary>
    /// Represents a participant (either <see cref="IndividualParticipant"/> or <see cref="GroupParticipant"/>) in this competition
    /// </summary>
    /// <remarks>
    /// Must be initialized when loading from XML
    /// </remarks>
    public abstract partial class Participant : ObservableValidator
    {
        protected ScoresheetFile? _ScoresheetFile;

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
            set {
                if (SetProperty(ref _ChestNumber, value))
                {
                    UniqueColour = HSV.ToColor((value % 100) * 818f, Random.Shared.NextSingle() * 0.5f + 0.5f, 1f);
                }
            }
        }

        /// <summary>
        /// Gets a unique colour for differentiation
        /// </summary>
        [XmlIgnore]
        public System.Windows.Media.Color UniqueColour { get; private set; } = System.Windows.Media.Color.FromRgb(0, 0, 0);

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

        #region Scoring



        #endregion

        /// <summary>
        /// Find the participant's <see cref="Team"/> from <paramref name="teams"/>
        /// </summary>
        /// <param name="teams"></param>
        /// <exception cref="Examath.Core.Model.ObjectLinkingException"></exception>
        public virtual void Initialize(ScoresheetFile scoresheetFile)
        {
            _ScoresheetFile = scoresheetFile;
            if (!string.IsNullOrEmpty(Team_Name))
            {
                Team = scoresheetFile.Teams.Find((x) => x.Name == Team_Name);
                if (Team == null)
                {
                    throw new Examath.Core.Model.ObjectLinkingException(this, Team_Name, typeof(Team));
                }
            }            
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            _ScoresheetFile?.NotifyChange(this);
        }
    }
}
