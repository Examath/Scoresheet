using CommunityToolkit.Mvvm.ComponentModel;
using Examath.Core.Environment;
using Scoresheet.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Scoresheet.Formatter
{
    public partial class FormatterVM : ObservableObject
    {
        public bool IsGuidelineLoaded { get; set; } = false;
        public bool IsTeamsListLoaded { get; set; } = false;

        public FormatterVM()
        {
            LoadGuidelineAndTeamList();
        }

        private async void LoadGuidelineAndTeamList()
        {
            while (!IsGuidelineLoaded)
            {
                System.Windows.Forms.OpenFileDialog openFileDialog = new()
                {
                    Title = "Open Guideline file",
                    Filter = "Guideline (.ssgl)|*.ssgl|All|*.*",
                };

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        Guideline? data = await Examath.Core.Utils.XML.LoadAsync<Guideline>(openFileDialog.FileName);
                        if (data == null)
                        {
                            if (Messager.Out("Want to try again", "Guideline is null", yesButtonText: "Try Again", isCancelButtonVisible: true) == DialogResult.Yes)
                                continue;
                            else
                                return;
                        }
                        DialogResult dialogResult = Messager.Out(data.ToString() ?? "null", $"Check Guideline",
                            isCancelButtonVisible: true, noButtonText: "Try Again", yesButtonText: "Continue");
                        switch (dialogResult)
                        {
                            case DialogResult.Yes:
                                Guideline = data;
                                IsGuidelineLoaded = true;
                                break;
                            case DialogResult.No:
                                continue;
                            default:
                                return;
                        }
                    }
                    catch (Exception e)
                    {
                        if (Messager.OutException(e, yesButtonText: "Try Again", isCancelButtonVisible: true) == DialogResult.Yes)
                            continue;
                        else
                            return;
                    }
                }
                else return;
            }

            while (!IsTeamsListLoaded)
            {
                System.Windows.Forms.OpenFileDialog openFileDialog = new()
                {
                    Title = "Open Teams List file",
                    Filter = "Teams List (.sstl)|*.sstl|All|*.*",
                };

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {

                        List<IndividualParticipant> individualParticipants = new();
                        string[] rdata = await File.ReadAllLinesAsync(openFileDialog.FileName);
                        string[] buffer = new string[3];

                        foreach (string line in rdata)
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
                        if (individualParticipants.Count < 1)
                        {
                            if (Messager.Out("Want to try again", "Teams list is empty", yesButtonText: "Try Again", isCancelButtonVisible: true) == DialogResult.Yes)
                                continue;
                            else
                                return;
                        }
                        DialogResult dialogResult = Messager.Out(individualParticipants.Count + "\n" + individualParticipants[0], $"Check Teams List",
                        isCancelButtonVisible: true, noButtonText: "Try Again", yesButtonText: "Continue");
                        switch (dialogResult)
                        {
                            case DialogResult.Yes:
                                IndividualParticipants = individualParticipants;
                                IsTeamsListLoaded = true;
                                break;
                            case DialogResult.No:
                                continue;
                            default:
                                return;
                        }
                    }
                    catch (Exception e)
                    {
                        if (Messager.OutException(e, yesButtonText: "Try Again", isCancelButtonVisible: true) == DialogResult.Yes)
                            continue;
                        else
                            return;
                    }
                }
                else return;
            }
        }

        #region Guideline Object

        public Guideline Guideline { get; set; } = new();

        #endregion

        #region Participant List Object

        private List<IndividualParticipant> _IndividualParticipants = new();
        /// <summary>
        /// Gets or sets the list of individual participants
        /// </summary>
        public List<IndividualParticipant> IndividualParticipants
        {
            get => _IndividualParticipants;
            set => SetProperty(ref _IndividualParticipants, value);
        }

        #endregion
    }
}
