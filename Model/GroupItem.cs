using CommunityToolkit.Mvvm.Input;
using Examath.Core.Environment;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        protected override double[] _PlacePoints => new double[] { 50, 20, 10, 0 };

        [RelayCommand]
        public void SearchAndAddParticipantToGroup(GroupParticipant? groupParticipant)
        {
            IndividualParticipant? result = SearchAddParticipantFunc(groupParticipant);

            if (result != null && groupParticipant != null)
            {
                groupParticipant.IndividualParticipants.Add(result);
            }
        }

        [RelayCommand]
        public void CreateGroupParticipant(object param)
        {
            if (_ScoresheetFile == null) return;
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
            GroupParticipant groupParticipant = new(_ScoresheetFile, new ObservableCollection<IndividualParticipant>(individualParticipants))
            {
                Team = team,
                ChestNumber = _ScoresheetFile?.GetNextGroupChessNumber(team) ?? 1000,
                Leader = individualParticipants.FirstOrDefault(),
            };

            _ScoresheetFile?.NotifyChange(this);
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
            if (Searcher.Select(GroupParticipants.Where((g) => g.Team == team).ToList(), "Select group to add to") is GroupParticipant groupParticipant)
            {
                foreach (IndividualParticipant individualParticipant in individualParticipants)
                {
                    if (!groupParticipant.IndividualParticipants.Contains(individualParticipant)) groupParticipant.IndividualParticipants.Add(individualParticipant);
                }

                _ScoresheetFile?.NotifyChange(groupParticipant);
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
                _ScoresheetFile?.NotifyChange(groupParticipant);
            }

        }

        [RelayCommand]
        public void RemoveGroupParticipant(GroupParticipant? groupParticipant)
        {
            if (groupParticipant != null)
            {
                if (Scores.Where(s => s.ParticipantChestNumber == groupParticipant.ChestNumber).Any()) // Score given
                {
                    Messager.Out("This group result has already been marked and so cannot be removed", "Cannot remove group", ConsoleStyle.WarningBlockStyle);
                    return;
                }

                GroupParticipants.Remove(groupParticipant);
                _ScoresheetFile?.NotifyChange(this);
            }
        }

        [RelayCommand]
        public void AutomaticallyCreateGroupParticipants()
        {
            if (_ScoresheetFile != null)
            {
                foreach (Team team in _ScoresheetFile.Teams)
                {
                    ObservableCollection<IndividualParticipant> individualParticipants = new(IndividualParticipants.Where(p => p.Team == team));
                    if (individualParticipants.Count != 0)
                    {
                        GroupParticipant groupParticipant = new(_ScoresheetFile, individualParticipants)
                        {
                            Team = team,
                            ChestNumber = _ScoresheetFile?.GetNextGroupChessNumber(team) ?? 1000,
                            Leader = individualParticipants.OrderByDescending(p => p.YearLevel).FirstOrDefault(),
                        };
                        GroupParticipants.Add(groupParticipant);
                    }
                }
                _ScoresheetFile?.NotifyChange(this);
            }
        }

        public override void Initialize(ScoresheetFile scoresheetFile)
        {
            foreach (GroupParticipant participant in GroupParticipants) participant.Initialize(scoresheetFile);
            base.Initialize(scoresheetFile);
        }
    }
}
