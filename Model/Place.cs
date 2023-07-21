using System.Collections.Generic;

namespace Scoresheet.Model
{
    public class Place
    {
        public int ValueInt { get; private set; }

        public string Value { get; private set; }

        public Place(int value)
        {
            ValueInt = value;
            Value = AddOrdinal(value);
        }

        /// <summary>
        /// Gets a list of one or more participants that won this place
        /// </summary>
        public List<Participant> Participants { get; set; } = new();

        /// <summary>
        /// Returns <paramref name="num"/> with ordinals, like 1st, 2nd, 3rd ...
        /// </summary>
        /// <param name="num">The integer to convert</param>
        /// <returns>A string representation of <paramref name="num"/> with ordinals</returns>
        /// <remarks>From https://stackoverflow.com/a/20175/10701111</remarks>
        public static string AddOrdinal(int num)
        {
            if (num <= 0) return num.ToString();

            switch (num % 100)
            {
                case 11:
                case 12:
                case 13:
                    return num + "th";
            }

            switch (num % 10)
            {
                case 1:
                    return num + "st";
                case 2:
                    return num + "nd";
                case 3:
                    return num + "rd";
                default:
                    return num + "th";
            }
        }
    }
}
