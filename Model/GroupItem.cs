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
        public void CreateGroupParticipant(object param)
        {
            System.Collections.IList items = (System.Collections.IList)param;
            List<IndividualParticipant> individualParticipants = items.Cast<IndividualParticipant>().ToList();

            if (individualParticipants.Count() == 0) return;

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
                IndividualParticipants = new ObservableCollection<IndividualParticipant>(individualParticipants),
                ChestNumber = GroupParticipants.Where(gp => gp.Team == team).LastOrDefault()?.ChestNumber + 1 ?? _ScoresheetFile?.GetChessNumberBase(null, team) ?? 1000,
            };

            _ScoresheetFile?.OnModified(this);
            GroupParticipants.Add(groupParticipant);

            EditGroupParticipant(groupParticipant);
        }

        [RelayCommand]
        public void AddToExistingGroupParticipant(object param)
        {
            System.Collections.IList items = (System.Collections.IList)param;
            List<IndividualParticipant> individualParticipants = items.Cast<IndividualParticipant>().ToList();
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

                _ScoresheetFile?.OnModified(groupParticipant);
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
                _ScoresheetFile?.OnModified(groupParticipant) ;
            }

        }

        [RelayCommand]
        public void RemoveGroupParticipant(GroupParticipant? groupParticipant)
        {
            if (groupParticipant != null)
            {
                if (Scores.Where(s => s.ParticipantChestNumber == groupParticipant.ChestNumber).Any()) // Score given
                {
                    Messager.Out("This group participant has already been marked and so cannot be removed", "Cannot remove group", ConsoleStyle.WarningBlockStyle);
                    return;
                }

                GroupParticipants.Remove(groupParticipant);
                _ScoresheetFile?.OnModified(this);
            }
        }

        public override void Initialize(ScoresheetFile scoresheetFile)
        {
            base.Initialize(scoresheetFile);

            foreach(GroupParticipant participant in GroupParticipants) participant.Initialize(scoresheetFile);
        }
    }
}
