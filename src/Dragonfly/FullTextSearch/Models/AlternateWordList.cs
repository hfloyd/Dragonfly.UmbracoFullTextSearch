namespace Dragonfly.FullTextSearch.Models
{
    using System.Collections.Generic;
    using SpellChecker.Net.Search.Spell;

    public class AlternateWordList
    {
        public string OriginalWord { get; set; }
        public int OriginalWordFrequency { get; set; }
        public IEnumerable<AlternateWord> Words { get; set; }
    }

    public class AlternateWord
    {
        public string Word { get; set; }
        public int Frequency { get; set; }
        public float JaroWinkler { get; set; }
        public float Levenshtein { get; set; }
        public float NGram { get; set; }
        public int BestMatchSortOrder { get; set; }
        public float BestMatchScore { get; set; }
    }
}
