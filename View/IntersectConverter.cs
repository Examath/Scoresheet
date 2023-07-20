using Scoresheet.Model;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using static System.Formats.Asn1.AsnWriter;

namespace Scoresheet.View
{
    public class IntersectConverter : IMultiValueConverter
    {
        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is ObservableCollection<Score> scores && values[1] is Participant participant)
            {
                if (parameter is bool ShowDetails && ShowDetails)
                {
                    Score? score = scores.Where(s => s.IsOf(participant)).FirstOrDefault();
                    if (score != null) return $"{string.Join(',', score.Marks)} = {score.AverageMarks} ({Place.AddOrdinal(score.Place ?? 0)})";
                    else return null;
                }
                else
                {
                    return scores.Where(s => s.IsOf(participant)).FirstOrDefault()?.AverageMarks.ToString();
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
