using CommunityToolkit.Mvvm.ComponentModel;
using Scoresheet.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Scoresheet.Formatter
{
    public partial class FormSubmission : ObservableObject
    {
        #region Fields and Properties

        const int SEARCH_CLIP = 20;
        const double SEARCH_LEVEL_FACTOR = 0.4;

        public string[] Data { get; set; }

        /// <summary>
        /// Gets a list of abbreviations for the items this participant seeks to join
        /// </summary>
        public string Details { get; private set; }

        private const int TimeStampI = 0;
        /// <summary>
        /// The time this form was submitted
        /// </summary>
        public DateTime TimeStamp { get; set; }

        private const int EmailI = 1;
        /// <summary>
        /// The email used to submit this form
        /// </summary>
        public string Email { get; set; }

        private const int PhoneNumberI = 2;
        /// <summary>
        /// The phone number provided as contact
        /// </summary>
        public string PhoneNumber { get; set; }

        private const int FullNameI = 3;
        /// <summary>
        /// The full name, combined from Family name and Given name entries
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Gets the <see cref="string.ToUpperInvariant"/> form of <see cref="FullName"/>
        /// </summary>
        public string SearchName { get; private set; }

        private const int YearLevelI = 4;
        /// <summary>
        /// The year level of entrant
        /// </summary>
        public int YearLevel { get; private set; }

        /// <summary>
        /// Gets the level definition associated with this entrant's claimed year level
        /// </summary>
        public LevelDefinition? Level { get; private set; }

        private const int ItemsStartI = 5;
        private const int ItemsEndI = 8;

        /// <summary>
        /// Possible or actual match
        /// </summary>
        public IndividualParticipant? Match { get; set; }

        public double MatchScore { get; private set; } = 0;

        private const int GroupItemsI = 9;

        private SubmissionStatus _SubmissionStatus;
        /// <summary>
        /// Gets or sets 
        /// </summary>
        public SubmissionStatus SubmissionStatus
        {
            get => _SubmissionStatus;
            set => SetProperty(ref _SubmissionStatus, value);
        }

        #endregion

        #region Init

        public static async System.Threading.Tasks.Task<FormSubmission> CreateFormSubmissionAndApplyMatch(string rawData, ScoresheetFile scoresheetFile)
        {
            FormSubmission formSubmission = await Task.Run<FormSubmission>(() => new(rawData, scoresheetFile));

            // Apply match
            if (formSubmission.Match != null && formSubmission.MatchScore >= 1)
            {
                if (formSubmission.IsValidMatch(formSubmission.Match)) // Validity of Submission
                {
                    formSubmission.ApplyMatch(formSubmission.Match, scoresheetFile);
                }
                else
                {
                    formSubmission._SubmissionStatus = SubmissionStatus.Invalid;
                }
            }
            else
            {
                formSubmission._SubmissionStatus = SubmissionStatus.Mismatch;
            }

            return formSubmission;
        }

        /// <summary>
        /// Formats the form submission entry and attempts to find the matching <see cref="ScoresheetFile.IndividualParticipants"/>
        /// </summary>
        /// <param name="rawData">The raw csv line for this submission</param>
        /// <param name="scoresheetFile">the scoresheet file for reference and to modify if a match is found</param>
        private FormSubmission(string rawData, ScoresheetFile scoresheetFile)
        {
            // text processing
            string[] rawArray = rawData.Split(',');
            Data = new string[rawArray.Length];
            Details = "";
            for (int i = 0; i < rawArray.Length; i++)
            {
                Data[i] = rawArray[i].Trim(new char[] { '"', ' ' });

                // For Details property
                if (i >= ItemsStartI && i <= ItemsEndI) // Column that may store items then
                {
                    Details += string.Join(' ',                     // Join with space
                        Data[i].Split(';')                          //  Items separated by semicolon
                        .Select((x) =>                              //   Select:
                        string.Join("",                             //    Join with nothing
                        x.Split(' ')                                //     Words split by space
                        .Select((y) => y[..Math.Min(y.Length, 2)])  //      Select: first two chars                        
                        ))) + " ";                                  // Then Append Space
                }
            }

            // Independent properties 5/12/2024 14:59:01
            TimeStamp = DateTime.ParseExact(Data[TimeStampI].Replace("GMT", ""), "M/d/yyyy H:mm:ss", CultureInfo.InvariantCulture);
            Email = Data[EmailI].ToLower();
            PhoneNumber = Data[PhoneNumberI].ToLower();
            FullName = Data[FullNameI];
            SearchName = FullName.ToUpperInvariant();

            // Dependent properties
            if (int.TryParse(NotDigitsRegex().Replace(Data[YearLevelI], ""), out int yearLevel))
            {
                YearLevel = yearLevel;
                Level = scoresheetFile.LevelDefinitions.Find(x => x.Within(YearLevel));
            }

            // Find match
            double searchDistance = SEARCH_CLIP;
            foreach (IndividualParticipant individualParticipant in scoresheetFile.IndividualParticipants)
            {
                if (individualParticipant.SearchName == SearchName)
                {
                    Match = individualParticipant;
                    searchDistance = 0;
                    break;
                }
                else
                {
                    double currentSearchDistance = DamerauLevenshteinDistance(individualParticipant.SearchName, SearchName, SEARCH_CLIP);

                    // Prefer better matches
                    if (individualParticipant.YearLevel == YearLevel) currentSearchDistance *= SEARCH_LEVEL_FACTOR;

                    if (currentSearchDistance < searchDistance)
                    {
                        Match = individualParticipant;
                        searchDistance = currentSearchDistance;
                    }
                }
            }

            MatchScore = Math.Round(1 - (searchDistance / 15), 2);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Checks whether this <see cref="FormSubmission"/> claims to be in the same year level 
        /// and the same <see cref="Model.Team"/> as the provided <paramref name="match"/>
        /// </summary>
        /// <param name="match">The <see cref="IndividualParticipant"/> to compare to</param>
        /// <returns>True if they are a valid match, otherwise false</returns>
        public bool IsValidMatch(IndividualParticipant match) => match.YearLevel == YearLevel;

        /// <summary>
        /// Applies the data from this <see cref="FormSubmission"/> to the <paramref name="match"/>
        /// </summary>
        /// <param name="match">The <see cref="IndividualParticipant"/> to apply to</param>
        /// <remarks>
        /// This only applies if this <see cref="FormSubmission"/> is newer than the curent
        /// <see cref="IndividualParticipant.SubmissionTimeStamp"/>
        /// </remarks>
        public void ApplyMatch(IndividualParticipant match, ScoresheetFile scoresheetFile)
        {
            if (Level == null)
            {
                SubmissionStatus = SubmissionStatus.Error;
            }
            else if (match.SubmissionTimeStamp < TimeStamp)
            {
                if (match.IsFormSubmitted)
                {
                    SubmissionStatus = SubmissionStatus.Edited;
                    match.UnjoinAllCompetitions();
                }
                else
                {
                    SubmissionStatus = SubmissionStatus.Assigned;
                }

                // Prevent older requests from overriding
                match.SubmissionTimeStamp = TimeStamp;

                match.SubmissionEmail = Email;
                match.SubmissionPhoneNumber = PhoneNumber;
                match.SubmissionFullName = FullName;

                // The csv file has several columns with individual items
                // Join solo items from only two columns
                for (int i = ItemsStartI; i <= ItemsEndI; i++)
                {                    
                    match.JoinCompetitions(Data[i].Split(';'), appendLevelToCode: true);
                }

                // Join group items
                match.JoinCompetitions(Data[GroupItemsI].Split(';'));
            }
            else
            {
                SubmissionStatus = SubmissionStatus.Ignored;
            }
        }

        public override string ToString() => $"{TimeStamp:dd/MM} {FullName}";

        #endregion

        #region Comparison Algorithm

        /// <summary>
        /// Computes the Damerau-Levenshtein Distance between two strings, represented as arrays of
        /// integers, where each integer represents the code point of a character in the source string.
        /// Includes an optional threshhold which can be used to indicate the maximum allowable distance.
        /// </summary>
        /// <param name="source">An array of the code points of the first string</param>
        /// <param name="target">An array of the code points of the second string</param>
        /// <param name="threshold">Maximum allowable distance</param>
        /// <returns>Int.MaxValue if threshhold exceeded; otherwise the Damerau-Leveshteim distance between the strings</returns>
        /// <remarks>From https://stackoverflow.com/a/9454016/10701111</remarks>
        public static int DamerauLevenshteinDistance(string source, string target, int threshold)
        {

            int length1 = source.Length;
            int length2 = target.Length;

            // Return trivial case - difference in string lengths exceeds threshhold
            if (Math.Abs(length1 - length2) > threshold) { return int.MaxValue; }

            // Ensure arrays [i] / length1 use shorter length 
            if (length1 > length2)
            {
                Swap(ref target, ref source);
                Swap(ref length1, ref length2);
            }

            int maxi = length1;
            int maxj = length2;

            int[] dCurrent = new int[maxi + 1];
            int[] dMinus1 = new int[maxi + 1];
            int[] dMinus2 = new int[maxi + 1];
            int[] dSwap;

            for (int i = 0; i <= maxi; i++) { dCurrent[i] = i; }

            int jm1 = 0, im1 = 0, im2 = -1;

            for (int j = 1; j <= maxj; j++)
            {

                // Rotate
                dSwap = dMinus2;
                dMinus2 = dMinus1;
                dMinus1 = dCurrent;
                dCurrent = dSwap;

                // Initialize
                int minDistance = int.MaxValue;
                dCurrent[0] = j;
                im1 = 0;
                im2 = -1;

                for (int i = 1; i <= maxi; i++)
                {

                    int cost = source[im1] == target[jm1] ? 0 : 1;

                    int del = dCurrent[im1] + 1;
                    int ins = dMinus1[i] + 1;
                    int sub = dMinus1[im1] + cost;

                    //Fastest execution for min value of 3 integers
                    int min = (del > ins) ? (ins > sub ? sub : ins) : (del > sub ? sub : del);

                    if (i > 1 && j > 1 && source[im2] == target[jm1] && source[im1] == target[j - 2])
                        min = Math.Min(min, dMinus2[im2] + cost);

                    dCurrent[i] = min;
                    if (min < minDistance) { minDistance = min; }
                    im1++;
                    im2++;
                }
                jm1++;
                if (minDistance > threshold) { return int.MaxValue; }
            }

            int result = dCurrent[maxi];
            return (result > threshold) ? int.MaxValue : result;
        }
        static void Swap<T>(ref T arg1, ref T arg2)
        {
            T temp = arg1;
            arg1 = arg2;
            arg2 = temp;
        }

        [GeneratedRegex("[^0-9]+")]
        private static partial Regex NotDigitsRegex();

        #endregion
    }

    public enum SubmissionStatus
    {
        Mismatch,
        Invalid,
        Edited,
        Assigned,
        Ignored,
        Error,
    }
}
