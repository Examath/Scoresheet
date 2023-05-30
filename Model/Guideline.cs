using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Scoresheet.Model
{
    public class Guideline
    {
        public List<Team> Teams { get; set; } = new();

        public List<LevelDefinition> LevelDefinitions { get; set; } = new();

        public List<CompetitionItem> CompetitionItems { get; set; } = new();

        public override string ToString()
        {
            return $"Teams: {string.Join(", ", Teams)}\n" +
                $"Levels: {string.Join(", ", LevelDefinitions)}\n" +
                $"Items: {string.Join(", ", CompetitionItems)}";
        }
    }
}
