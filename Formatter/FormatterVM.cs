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
        public bool IsLoaded { get; set; }

        public FormatterVM()
        {
            Guideline = new();
        }

        public FormatterVM(Guideline guideline)
        {
            Guideline = guideline;

            while (!IsLoaded)
            {
                OpenFileDialog openFileDialog = new()
                {
                    Title = "Open Teams List file",
                    Filter = "Teams List (.sstl)|*.sstl|All|*.*",
                };

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        List<IndividualParticipant> individualParticipants = new();
                        string[] rdata = File.ReadAllLines(openFileDialog.FileName);
                        string[] buffer = new string[3];

                        int[,] chestNumberMatrix = getChestNumberMatrix();

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
                                individualParticipants.Add(new(buffer, chestNumberMatrix, Guideline.Teams, Guideline.LevelDefinitions));
                            }
                        }
                        if (individualParticipants.Count < 1)
                        {
                            if (Messager.Out("Want to try loading the teams list again", "Teams list is empty", yesButtonText: "Try Again", isCancelButtonVisible: true) == DialogResult.Yes)
                                continue;
                            else
                                return;
                        }
                        DialogResult dialogResult = Messager.Out($"Count: {individualParticipants.Count}\n\n{string.Join('\n',individualParticipants)}", $"Check Teams List",
                        isCancelButtonVisible: true, noButtonText: "Try Again", yesButtonText: "Continue");
                        switch (dialogResult)
                        {
                            case DialogResult.Yes:
                                IndividualParticipants = individualParticipants;
                                IsLoaded = true;
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

            int[,] getChestNumberMatrix()
            {
                int [,] chestNumberMatrix = new int[Guideline.LevelDefinitions.Count, Guideline.Teams.Count];

                for (int levelIndex = 0; levelIndex < chestNumberMatrix.GetLength(0); levelIndex++)
                {
                    for (int teamIndex = 0; teamIndex < chestNumberMatrix.GetLength(1); teamIndex++)
                    {
                        chestNumberMatrix[levelIndex,teamIndex] = (levelIndex * Guideline.Teams.Count + teamIndex + 1) * 100;
                    }
                }

                return chestNumberMatrix;
            }
        }

        #region Guideline Object

        public Guideline Guideline { get; set; }

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
