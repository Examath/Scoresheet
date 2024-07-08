using Scoresheet.Model;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using static System.Formats.Asn1.AsnWriter;

namespace Scoresheet.View
{
    /// <summary>
    /// Represents a converter that finds the last <see cref="Score"/> 
    /// in a list of scores that applies to a specified <see cref="Participant"/>
    /// </summary>
    public class IntersectConverter : IMultiValueConverter
    {
        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is ObservableCollection<Score> scores && values[1] is Participant participant)
            {
                Score? score = scores.Where(s => s.IsOf(participant)).LastOrDefault();
                // If null
                if (score == null) return null;

                // Format as string
                else if (targetType == typeof(string))
                {
                    if (parameter is bool ShowDetails && ShowDetails)
                    {
                        return $"{score.MarksToString()} = {score.AverageMarks} ({Place.AddOrdinal(score.Place ?? 0)})";
                    }
                    else
                    {
                        return score.AverageMarks.ToString();
                    }
                }

                // Return binding
                else
                {
                    return score;
                }
            }
            else return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
