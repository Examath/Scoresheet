using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Scoresheet.Model
{
    /// <summary>
    /// Represents the competition guidelines for a competition
    /// </summary>
    /// <remarks>
    /// Must be initialised when loaded from XML
    /// </remarks>
    public class Guideline
    {
        public const string Extension = ".ssgl";

        /// <summary>
        /// Gets or sets the list of <see cref="Team"/>s in this competition
        /// </summary>
        public List<Team> Teams { get; set; } = new();

        /// <summary>
        /// Gets or sets a list of <see cref="LevelDefinition"/> in this competition
        /// </summary>
        public List<LevelDefinition> LevelDefinitions { get; set; } = new();

        /// <summary>
        /// Gets or setsa list of <see cref="CompetitionItem"/>s in this competition
        /// </summary>
        public List<CompetitionItem> CompetitionItems { get; set; } = new();

        /// <summary>
        /// Call after loading from XML. Matches the correct <see cref="LevelDefinition"/>s to each <see cref="CompetitionItem"/>
        /// </summary>
        public void Initialise()
        {
            foreach (CompetitionItem CompetitionItem in CompetitionItems) CompetitionItem.Initialize(LevelDefinitions);
        }

        public override string ToString()
        {
            return $"Teams: {string.Join(", ", Teams)}\n" +
                $"Levels: {string.Join(", ", LevelDefinitions)}\n" +
                $"Items: {string.Join(", ", CompetitionItems)}";
        }
    }
}
