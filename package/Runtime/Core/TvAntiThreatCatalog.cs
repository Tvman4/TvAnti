using System;
using System.Collections.Generic;

namespace TvAnti
{
    [Serializable]
    public sealed class TvAntiThreatRule
    {
        public string Id;
        public string[] ExactAssemblyNames;
        public string[] ExactFileNames;
        public int Confidence;

        public TvAntiThreatRule(string id, int confidence, string[] exactAssemblyNames, string[] exactFileNames)
        {
            Id = id;
            Confidence = confidence;
            ExactAssemblyNames = exactAssemblyNames ?? Array.Empty<string>();
            ExactFileNames = exactFileNames ?? Array.Empty<string>();
        }
    }

    public static class TvAntiThreatCatalog
    {
        // Names supplied by the game owner. These are treated as indicators,
        // not proof by themselves unless an exact assembly/file identity matches.
        public static readonly IReadOnlyList<TvAntiThreatRule> Rules = new[]
        {
            new TvAntiThreatRule("flow_lol", 100, new[] { "flow.lol", "flow.lol22" }, new[] { "flow.lol.dll", "flow.lol22.dll" }),
            new TvAntiThreatRule("spm", 100, new[] { "SPM" }, new[] { "SPM.dll" }),
            new TvAntiThreatRule("flying_cats_v4", 100, new[] { "FlyingCatsStupidMenuV4" }, new[] { "FlyingCatsStupidMenuV4.dll" }),
            new TvAntiThreatRule("bypass_2ma", 100, new[] { "AntiCheatBypass2Ma" }, new[] { "AntiCheatBypass2Ma.dll" }),
            new TvAntiThreatRule("bypass_2022_2023", 100, new[] { "AntiCheatBypass2022.2and2023.3" }, new[] { "AntiCheatBypass2022.2and2023.3.dll" }),
            new TvAntiThreatRule("null_holdable_v2", 100, new[] { "NullHoldableV2" }, new[] { "NullHoldableV2.dll" }),
            new TvAntiThreatRule("rmh", 100, new[] { "RMH" }, new[] { "RMH.dll" }),
            new TvAntiThreatRule("bebite_cheats", 100, new[] { "BebiteCheats" }, new[] { "BebiteCheats.dll" }),
            new TvAntiThreatRule("cool_lib", 100, new[] { "cool-lib" }, new[] { "cool-lib.dll" }),
            new TvAntiThreatRule("tv_menu", 100, new[] { "TvMenu" }, new[] { "TvMenu.dll" })
        };
    }
}
