using CommunityToolkit.Mvvm.ComponentModel;
using Scoresheet.Model;
using System;
using System.Collections.Generic;
using System.IO;

namespace Scoresheet.Formatter
{
    public partial class FormatterVM : ObservableObject
    {
        public FormatterVM() { }

        #region Guideline Object

        private string _GuidelineLocation = "";
        /// <summary>
        /// Gets or sets the file location of the guideline file
        /// </summary>
        public string GuidelineLocation
        {
            get => _GuidelineLocation;
            set
            {
                if (SetProperty(ref _GuidelineLocation, value)) LoadGuideline();
            }
        }

        private async void LoadGuideline()
        {
            Guideline = await Examath.Core.Utils.XML.TryLoad<Guideline>(_GuidelineLocation);
            if (Guideline != null)
            {
                Guideline.Initialise();
                LoadTeamList();
            }
        }

        private Guideline? _Guideline = null;
        /// <summary>
        /// Gets or sets 
        /// </summary>
        public Guideline? Guideline
        {
            get => _Guideline;
            set => SetProperty(ref _Guideline, value);
        }

        #endregion

        #region Participant List Object

        private string _ParticipantListFileLocation = "";
        /// <summary>
        /// Gets or sets the location of the <see cref="Participant"/> list file
        /// </summary>
        public string ParticipantListFileLocation
        {
            get => _ParticipantListFileLocation;
            set
            {
                if (SetProperty(ref _ParticipantListFileLocation, value)) LoadTeamList();
            }
        }

        private List<IndividualParticipant> _IndividualParticipants = new();
        /// <summary>
        /// Gets or sets the list of individual participants
        /// </summary>
        public List<IndividualParticipant> IndividualParticipants
        {
            get => _IndividualParticipants;
            set => SetProperty(ref _IndividualParticipants, value);
        }

        private async void LoadTeamList()
        {
            if (Guideline == null || !Path.Exists(_ParticipantListFileLocation)) return;
            List<IndividualParticipant> individualParticipants = new();

            try
            {
                string[] data = await File.ReadAllLinesAsync(ParticipantListFileLocation);
                string[] buffer = new string[3];

                foreach (string line in data)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string[] entry = line.Split('\t');

                    for (int i = 0; i < Math.Min(buffer.Length, entry.Length); i++)
                    {
                        if (!string.IsNullOrWhiteSpace(entry[i])) buffer[i] = entry[i];
                    }

                    if (!string.IsNullOrWhiteSpace(entry[0]))
                    {
                        individualParticipants.Add(new(buffer, Guideline.Teams, Guideline.LevelDefinitions));
                    }
                }

                IndividualParticipants = individualParticipants;
            }
            catch (Exception)
            {

            }
        }

        #endregion
    }
}
