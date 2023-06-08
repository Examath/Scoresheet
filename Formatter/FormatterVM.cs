using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Examath.Core.Environment;
using Scoresheet.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Scoresheet.Formatter
{
    public partial class FormatterVM : ObservableObject
    {
        #region Properties

        public bool IsLoaded { get; set; }

        private double _Progress = 0;
        /// <summary>
        /// Gets or sets the progress whilst doing a task
        /// </summary>
        public double Progress
        {
            get => _Progress;
            private set => SetProperty(ref _Progress, value);
        }

        #endregion

        #region Initializers

        public FormatterVM()
        {
            ScoresheetFile = new();
        }

        public FormatterVM(ScoresheetFile scoresheetFile)
        {
            ScoresheetFile = scoresheetFile;

            // Todo if contains participants return

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
                                individualParticipants.Add(new(buffer, chestNumberMatrix, ScoresheetFile.Teams, ScoresheetFile.LevelDefinitions));
                            }
                        }
                        if (individualParticipants.Count < 1)
                        {
                            if (Messager.Out("Want to try loading the teams list again", "Teams list is empty", yesButtonText: "Try Again", isCancelButtonVisible: true) == DialogResult.Yes)
                                continue;
                            else
                                return;
                        }
                        DialogResult dialogResult = Messager.Out($"Count: {individualParticipants.Count}\n\n{string.Join('\n', individualParticipants)}", $"Check Teams List",
                        isCancelButtonVisible: true, noButtonText: "Try Again", yesButtonText: "Continue");
                        switch (dialogResult)
                        {
                            case DialogResult.Yes:
                                ScoresheetFile.IndividualParticipants = individualParticipants;
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
                int[,] chestNumberMatrix = new int[ScoresheetFile.LevelDefinitions.Count, ScoresheetFile.Teams.Count];

                for (int levelIndex = 0; levelIndex < chestNumberMatrix.GetLength(0); levelIndex++)
                {
                    for (int teamIndex = 0; teamIndex < chestNumberMatrix.GetLength(1); teamIndex++)
                    {
                        chestNumberMatrix[levelIndex, teamIndex] = (levelIndex * ScoresheetFile.Teams.Count + teamIndex + 1) * 100;
                    }
                }

                return chestNumberMatrix;
            }
        }

        #endregion

        #region Scoresheet Object

        public Model.ScoresheetFile ScoresheetFile { get; set; }

        #endregion

        #region Importer

        [RelayCommand]
        public async Task Import()
        {
            Progress = 0.01;

            OpenFileDialog openFileDialog = new()
            {
                Title = "Open submissions from Google forms",
                Filter = "csv|*.csv|All|*.*",
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string[] data = await File.ReadAllLinesAsync(openFileDialog.FileName);

                Progress = 0.1;

                for (int i = 1; i < data.Length; i++)
                {
                    Progress = 0.9 * i / data.Length + 0.1;
                    FormSubmission formSubmission = await Task.Run<FormSubmission>(() => new(data[i], ScoresheetFile));
                    PendingFormSubmissions.Add(formSubmission);
                }
            }

            Progress = 0;
        }

        private ObservableCollection<FormSubmission> _PendingFormSubmissions = new();
        /// <summary>
        /// Gets or sets the list of invalid submissions that need to be corrected
        /// </summary>
        public ObservableCollection<FormSubmission> PendingFormSubmissions
        {
            get => _PendingFormSubmissions;
            set { if (SetProperty(ref _PendingFormSubmissions, value)) return; }
        }

        #endregion
    }
}
