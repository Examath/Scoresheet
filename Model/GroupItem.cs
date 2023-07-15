using CommunityToolkit.Mvvm.Input;
using Examath.Core.Environment;
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

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override IEnumerable<Participant> Participants => GroupParticipants;

        [RelayCommand]
        public void CreateGroupParticipant(IList<IndividualParticipant> individualParticipants)
        {
            if (individualParticipants.Count == 0) return;

            // Check Team
            Team? team = individualParticipants.First().Team;
            foreach (IndividualParticipant individualParticipant in individualParticipants)
            {
                if (individualParticipant.Team != team)
                {
                    Messager.Out("All participants must be in the same team", "Mixed teams", ConsoleStyle.FormatBlockStyle);
                    return;
                }
            }

            // Create Group
            GroupParticipant groupParticipant = new()
            {
                Team = team,
                IndividualParticipants = (ObservableCollection<IndividualParticipant>)individualParticipants,
                ChestNumber = GroupParticipants.LastOrDefault()?.ChestNumber ?? _ScoresheetFile?.GetChessNumberBase(null, team) ?? 1000,
            };

            EditGroupParticipant(groupParticipant);

            GroupParticipants.Add(groupParticipant);
        }

        [RelayCommand]
        public void AddToExistingGroupParticipant(IList<IndividualParticipant> individualParticipants)
        {
            if (individualParticipants.Count == 0) return;

            // Check Team
            Team? team = individualParticipants.First().Team;
            foreach (IndividualParticipant individualParticipant in individualParticipants)
            {
                if (individualParticipant.Team != team)
                {
                    Messager.Out("All participants must be in the same team", "Mixed teams", ConsoleStyle.FormatBlockStyle);
                    return;
                }
            }

            // Add to Group
            if(Searcher.Select(GroupParticipants.Where((g) => g.Team == team).ToList(), "Select group to add to") is GroupParticipant groupParticipant)
            {
                foreach (IndividualParticipant individualParticipant in individualParticipants)
                {
                    groupParticipant.IndividualParticipants.Add(individualParticipant);
                }
            }
        }

        [RelayCommand]
        public void EditGroupParticipant(GroupParticipant? groupParticipant)
        {
            if (groupParticipant != null)
            {
                ComboBoxInput comboBoxI = new(groupParticipant, nameof(GroupParticipant.Leader), groupParticipant.IndividualParticipants, "Leader");
                IntQ chestNumberQ = new(groupParticipant.ChestNumber, "Chest Number");
                Asker.Show(new("Edit Group"), comboBoxI, chestNumberQ);

                if (chestNumberQ.Value != groupParticipant.ChestNumber 
                    && GroupParticipants.Where((g) => g.ChestNumber == chestNumberQ.Value).Count() == 0)
                {
                    groupParticipant.ChestNumber = chestNumberQ.Value;
                }
            }
        }
    }
}
