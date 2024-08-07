﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Scoresheet.Model
{
    /// <summary>
    /// Represents an age category
    /// </summary>
    public class LevelDefinition
    {
        /// <summary>
        /// Lowest inclusive allowed year level
        /// </summary>
        [XmlAttribute]
        public int LowerBound { get; set; } = 0;

        /// <summary>
        /// Highest inclusive allowed year level
        /// </summary>
        [XmlAttribute]
        public int UpperBound { get; set; } = 0;

        /// <summary>
        /// Abbreviation for this level
        /// </summary>
        [XmlAttribute]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Display name of this level
        /// </summary>
        [XmlAttribute]
        public string Name { get;set; } = string.Empty;

        /// <summary>
        /// Returns a string representation of this level
        /// </summary>
        public override string ToString() => $"{Code}: {Name} (Year {LowerBound} - {UpperBound})";

        internal bool Within(int yearLevel)
        {
            return LowerBound <= yearLevel && UpperBound >= yearLevel;
        }

        public static LevelDefinition All { get; private set; } = new() { LowerBound = 0, UpperBound = 12, Code = "A", Name = "All" };
    }
}
