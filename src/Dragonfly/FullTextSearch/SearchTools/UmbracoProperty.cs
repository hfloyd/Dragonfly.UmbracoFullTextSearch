namespace Dragonfly.FullTextSearch.SearchTools
{
    using Umbraco.Core;

    /// <summary>
    /// Some data indicating how to process a given document property from umbraco in search
    /// </summary>
    public class UmbracoProperty 
    {
        public string PropertyName { get; private set; }
        public double BoostMultiplier { get; private set; }
        public double FuzzyMultiplier { get; private set; }
        public bool Wildcard { get; set; }
        public UmbracoProperty(string propertyName, double boostMultipler = 1.0, double fuzzyMultipler = 1.0, bool wildcard = false)
        {
            PropertyName = CleanName(propertyName);
            BoostMultiplier = boostMultipler;
            FuzzyMultiplier = fuzzyMultipler;
            Wildcard = wildcard;
        }
        private string CleanName(string name)
        {
            return name.ToSafeAlias();
        }
    }
}