namespace Dragonfly.FullTextSearch.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Dragonfly.FullTextSearch.HighlightTools;
    using Dragonfly.FullTextSearch.Models;
    using Dragonfly.FullTextSearch.SearchTools;
    using Examine;
    using global::Umbraco.Core;

    /// <summary>
    /// Retrieve search results as SearchResultsCollection for users to use in their own Razor 
    /// </summary>
    public static class SearchHelper
    {
        /// <summary>
        /// Quick and simple static event to allow users to modify search results
        /// before they are output
        /// </summary>
        public static event EventHandler<ResultOutputEventArgs> ResultOutput;

        /// <summary>
        /// Main Helper Search function, the laundry list of parameters are documented more fully in FullTextSearch.xslt
        /// Basically this constructs a search object and a highlighter object from the parameters, then calls another 
        /// function to return search results as SearchResultsCollection.
        /// </summary>
        /// <param name="SearchType">MultiRelevance, MultiAnd, etc.</param>
        /// <param name="SearchTerm">The search terms as entered by user</param>
        /// <param name="TitleProperties">A list of umbraco properties, comma separated, to be searched as the page title</param>
        /// <param name="BodyProperties">A list of umbraco properties, comma separated, to be searched as the page body</param>
        /// <param name="RootNodes">Return only results under these nodes, set to blank or -1 to search all nodes</param>
        /// <param name="TitleLinkProperties">Umbraco properties, comma separated, to use in forming the (optionally highlighted) title</param>
        /// <param name="SummaryProperties">Umbraco properties, comma separated, to use in forming the (optionally highlighted) summary text</param>
        /// <param name="UseHighlighting">Enable context highlighting(note this can slow things down)</param>
        /// <param name="SummaryLength">Number of characters in the summary text</param>
        /// <param name="PageNumber">Page number of results to return</param>
        /// <param name="PageLength">Number of results on each page, zero disables paging and returns all results</param>
        /// <param name="Fuzzieness">Amount 0-1 to "fuzz" the search, return non exact matches</param>
        /// <param name="Wildcard">Add wildcard to the end of search term. Doesn't work together with fuzzyness</param>
        /// <param name="AlternateSpellingSuggestions">If the AlternateSpellings Index is set up, the number of alternates to return (0 to disable)</param>
        /// <returns></returns>
        public static SearchResultsCollection Search(string SearchType, string SearchTerm, string TitleProperties, string BodyProperties, string RootNodes, string TitleLinkProperties, string SummaryProperties, int UseHighlighting, int SummaryLength, int PageNumber = 0, int PageLength = 0, string Fuzzieness = "1.0", int Wildcard = 0, int AlternateSpellingSuggestions = 0)
        {
            //Initialize
            var resultsColl = new SearchResultsCollection();
            resultsColl.KeywordsQuery = SearchTerm;
            resultsColl.ConfigResultsPerPage = PageLength;

            // Measure time taken. This could be done more neatly, but this is more accurate
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // Check search terms were actually entered
            if (string.IsNullOrEmpty(SearchTerm))
            {
                resultsColl.IsError = true;
                resultsColl.ErrorCode = "NoTerms";
                resultsColl.ErrorMsg = "You must enter a search term";

                return resultsColl;
            }

            // Setup initial search parameters
            resultsColl.SearchParameters = SetupSearchParameters(SearchTerm, TitleProperties, BodyProperties, RootNodes, Fuzzieness, Wildcard);

            //Setup summarizer parameters
            resultsColl.SummarizerParameters = SetupSummariserParameters(SearchTerm, TitleLinkProperties, SummaryProperties, SummaryLength, Fuzzieness, Wildcard);

            // Create summarizer according to highlighting option
            resultsColl.Summarizer = SetupSummariser(resultsColl.SummarizerParameters, UseHighlighting);

            //Do Search, get results
            var searchResults = GetSearchResults(resultsColl.SearchParameters, SearchType);

            //Alternate Spellings?
            var altSpellInfo = new AlternateSpellingsInfo();
            if (AlternateSpellingSuggestions > 0)
            {
                altSpellInfo.AlternateSpellingsInUse = true;
                altSpellInfo.SearchTermIsMultiWordPhrase = SearchTerm.Contains(' ');

                var alternateSpellingTool = AlternateSpellingTool.Instance;

                var checkedTerms = SearchTerm.Split(' ').Select(t => alternateSpellingTool.GetBestMatchWord(t));
                var alternatePhrase = String.Join(" ", checkedTerms);
                altSpellInfo.BestMatchWord = alternatePhrase;

                if (!altSpellInfo.SearchTermIsMultiWordPhrase)
                {
                    var allWords = alternateSpellingTool.GetAlternateWordList(SearchTerm, AlternateSpellingSuggestions);
                    altSpellInfo.AlternateWordSuggestions = allWords;
                }

                //Re-run search with best match?
                if (!searchResults.Any())
                {
                    //Do Search, get results
                    var altSummariserParams = SetupSummariserParameters(altSpellInfo.BestMatchWord, TitleLinkProperties, SummaryProperties, SummaryLength, Fuzzieness, Wildcard);
                    var altSummarizer = SetupSummariser(altSummariserParams, UseHighlighting);
                    var altSearchParams = SetupSearchParameters(altSpellInfo.BestMatchWord, TitleProperties, BodyProperties, RootNodes, Fuzzieness, Wildcard);
                    var altSearchResults = GetSearchResults(altSearchParams, SearchType);
                    altSpellInfo = UpdateAlternateResults(altSpellInfo, altSearchResults, altSummarizer, PageNumber, PageLength);
                }

            }

            //Pass ISearchResults to updater function
            resultsColl = UpdateWithResults(resultsColl, searchResults, PageNumber, stopwatch);
            resultsColl.AlternateSpellingsInfo = altSpellInfo;

            return resultsColl;
        }

        /// <summary>
        /// Add additional search results to a collection (to pass in an alternate search term, for instance)
        /// </summary>
        /// <param name="CurrentResultsCollection"></param>
        /// <param name="SearchType"></param>
        /// <param name="SearchTerm"></param>
        /// <param name="UseHighlighting"></param>
        /// <param name="PageNumber"></param>
        /// <returns></returns>
        public static SearchResultsCollection SearchAnotherTerm(this SearchResultsCollection CurrentResultsCollection, string SearchType, string SearchTerm, int UseHighlighting, int PageNumber = 0)
        {
            // Check search terms were actually entered
            if (string.IsNullOrEmpty(SearchTerm))
            {
                return CurrentResultsCollection;
            }
            else if (CurrentResultsCollection.ErrorCode == "NoTerms")
            {
                //Clear existing error, if present
                CurrentResultsCollection.IsError = false;
                CurrentResultsCollection.ErrorCode = "";
                CurrentResultsCollection.ErrorMsg = "";
            }

            //Move existing KeywordsQuery 
            var multipleKeywordsList = CurrentResultsCollection.MultipleKeywordsQuery != null ? CurrentResultsCollection.MultipleKeywordsQuery.ToList() : new List<string>();

            if (CurrentResultsCollection.KeywordsQuery != "")
            {
                multipleKeywordsList.Add(CurrentResultsCollection.KeywordsQuery);
                CurrentResultsCollection.KeywordsQuery = "";
            }
            //Append current KeywordsQuery
            multipleKeywordsList.Add(SearchTerm);
            CurrentResultsCollection.MultipleKeywordsQuery = multipleKeywordsList;

            // Measure time taken. This could be done more neatly, but this is more accurate
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            //Update SearchParameters
            CurrentResultsCollection.SearchParameters.SearchTerm = SearchTerm;

            //Update summarizer parameters
            CurrentResultsCollection.SummarizerParameters.SearchTerm = SearchTerm;

            // Update summarizer 
            CurrentResultsCollection.Summarizer = SetupSummariser(CurrentResultsCollection.SummarizerParameters, UseHighlighting);

            //Do Search, get results
            var searchResults = GetSearchResults(CurrentResultsCollection.SearchParameters, SearchType);

            //Append Results
            //Pass ISearchResults to updater function
            var finalResultsCollection = UpdateWithResults(CurrentResultsCollection, searchResults, PageNumber, stopwatch);

            return finalResultsCollection;
        }

        /// <summary>
        /// Take ISearchResults from examine, create title and body summary, and update SearchResultsCollection
        /// </summary>
        /// <param name="ResultsCollection">
        /// The Results Collection.
        /// </param>
        /// <param name="SearchResults">
        /// The search Results.
        /// </param>
        /// <param name="summariser">
        /// The summarizer.
        /// </param>
        /// <param name="PageNumber">
        /// The page Number.
        /// </param>
        /// <param name="pageLength">
        /// The page Length.
        /// </param>
        /// <param name="Stopwatch">
        /// The stopwatch.
        /// </param>
        private static SearchResultsCollection UpdateWithResults(SearchResultsCollection ResultsCollection, ISearchResults SearchResults, int PageNumber = 0, Stopwatch Stopwatch = null)
        {
            int pageLength = ResultsCollection.ConfigResultsPerPage;

            if (ResultsCollection.AllResults == null || !ResultsCollection.AllResults.Any())
            {
                //Initial Search Population - Default behavior

                //Check for empty results
                var numResults = SearchResults.TotalItemCount;
                if (numResults < 1)
                {
                    ResultsCollection.IsError = true;
                    ResultsCollection.ErrorCode = "NoResults";
                    ResultsCollection.ErrorMsg = "Your search returned no results";

                    return ResultsCollection;
                }
                else if (ResultsCollection.ErrorCode == "NoResults")
                {
                    //Clear existing error, if present
                    ResultsCollection.IsError = false;
                    ResultsCollection.ErrorCode = "";
                    ResultsCollection.ErrorMsg = "";
                }

                //Convert from Examine Results to FullText Results
                var convertedResults = ConvertSearchResultsToFullTextResults(SearchResults, ResultsCollection.Summarizer, PageNumber).ToList();
                ResultsCollection.AllResults = convertedResults;
            }
            else
            {
                //Appending Results to existing collection

                var convertedResults = ResultsCollection.AllResults.ToList();
                //Convert from Examine Results to FullText Results
                convertedResults.AddRange(ConvertSearchResultsToFullTextResults(SearchResults, ResultsCollection.Summarizer, PageNumber).ToList());

                //Remove duplicates
                ResultsCollection.AllResults = convertedResults.DistinctBy(N => N.NodeId);
            }

            //End Stopwatch
            if (Stopwatch != null)
            {
                Stopwatch.Stop();
                double millisecs = Stopwatch.ElapsedMilliseconds;
                var numSecs = Math.Round((millisecs / 1000), 3);
                ResultsCollection.TimeTakenSeconds = numSecs;
            }

            //Handle Paging
            var allResults = ResultsCollection.AllResults.ToList();
            var totalResults = allResults.Count();
            ResultsCollection.TotalResults = totalResults;
            var numPages = totalResults % pageLength == 0 ? totalResults / pageLength : totalResults / pageLength + 1;
            ResultsCollection.NumOfPages = numPages;

            var allPages = new List<SearchResultPage>();

            //divide results into pages
            if (pageLength > 0)
            {
                var toSkip = 0;
                for (int i = 0; i < numPages; i++)
                {
                    var page = new SearchResultPage();

                    page.PageNum = i + 1;
                    toSkip = i * pageLength;
                    page.Results = allResults.Skip(toSkip).Take(pageLength);
                    page.ResultsOnPage = page.Results.Count();
                    page.FirstResult = toSkip + 1;

                    var lastResult = toSkip + pageLength;
                    if (lastResult > totalResults)
                    {
                        lastResult = totalResults;
                    }
                    page.LastResult = lastResult;

                    allPages.Add(page);
                }
            }
            else
            {
                ResultsCollection.IsError = true;
                ResultsCollection.ErrorCode = "NoPage";
                ResultsCollection.ErrorMsg = "Pagination incorrectly set up, no results on page" + PageNumber;

                return ResultsCollection;
            }

            ResultsCollection.Pages = allPages;

            return ResultsCollection;
        }

        /// <summary>
        /// Take ISearchResults from examine, create title and body summary, and return a collection of SearchResultPages
        /// </summary>
        /// <param name="AltSearchResults">
        /// Collection of search results.
        /// </param>
        /// <param name="AltSummarizer">
        /// Summarizer to format results
        /// </param>
        /// <param name="PageNumber">
        /// The current page Number
        /// </param>
        /// <param name="PageLength">
        /// Num Results per page
        /// </param>
        private static AlternateSpellingsInfo UpdateAlternateResults(AlternateSpellingsInfo AltSpellingsInfo, ISearchResults AltSearchResults, Summarizer AltSummarizer, int PageNumber, int PageLength)
        {
            //Convert from Examine Results to FullText Results
            var convertedResults = ConvertSearchResultsToFullTextResults(AltSearchResults, AltSummarizer, PageNumber).ToList();
            AltSpellingsInfo.AllResults = convertedResults;

            //Handle Paging
            var allResults = convertedResults.ToList();
            var totalResults = allResults.Count();
            AltSpellingsInfo.TotalResults = totalResults;
            var numPages = totalResults % PageLength == 0 ? totalResults / PageLength : totalResults / PageLength + 1;
            AltSpellingsInfo.NumOfPages = numPages;

            var allPages = new List<SearchResultPage>();

            //divide results into pages
            if (PageLength > 0)
            {
                var toSkip = 0;
                for (int i = 0; i < numPages; i++)
                {
                    var page = new SearchResultPage();

                    page.PageNum = i + 1;
                    toSkip = i * PageLength;
                    page.Results = allResults.Skip(toSkip).Take(PageLength);
                    page.ResultsOnPage = page.Results.Count();
                    page.FirstResult = toSkip + 1;

                    var lastResult = toSkip + PageLength;
                    if (lastResult > totalResults)
                    {
                        lastResult = totalResults;
                    }
                    page.LastResult = lastResult;

                    allPages.Add(page);
                }
            }

            AltSpellingsInfo.Pages = allPages;

            return AltSpellingsInfo;
        }


        private static SearchParameters SetupSearchParameters(string SearchTerm, string TitleProperties, string BodyProperties, string RootNodes, string Fuzzieness = "1.0", int Wildcard = 0)
        {
            // Setup search parameters
            double fuzzy;
            if (string.IsNullOrEmpty(Fuzzieness) || !double.TryParse(Fuzzieness, out fuzzy))
            {
                fuzzy = 1.0;
            }
            var wildcardBool = Wildcard > 0;
            var searchParameters = new SearchParameters();
            var searchProperties = GetSearchProperties(TitleProperties, BodyProperties, fuzzy, wildcardBool);
            if (searchProperties != null)
            {
                searchParameters.SearchProperties.AddRange(searchProperties);
            }
            searchParameters.RootNodes = GetRootNotes(RootNodes);
            searchParameters.SearchTerm = SearchTerm;

            return searchParameters;
        }

        private static SummarizerParameters SetupSummariserParameters(string SearchTerm, string TitleLinkProperties, string SummaryProperties, int SummaryLength, string Fuzzieness = "1.0", int Wildcard = 0)
        {
            //Setup summarizer parameters
            double fuzzy;
            if (string.IsNullOrEmpty(Fuzzieness) || !double.TryParse(Fuzzieness, out fuzzy))
            {
                fuzzy = 1.0;
            }
            var wildcardBool = Wildcard > 0;
            var summaryParameters = new SummarizerParameters { SearchTerm = SearchTerm };
            if (SummaryLength > 0)
            {
                summaryParameters.SummaryLength = SummaryLength;
            }
            AddSummaryProperties(summaryParameters, TitleLinkProperties, SummaryProperties, fuzzy, wildcardBool);

            return summaryParameters;
        }

        private static Summarizer SetupSummariser(SummarizerParameters SumParams, int UseHighlighting)
        {
            // Create summarizer according to highlighting option
            Summarizer summarizer;
            if (UseHighlighting > 0)
            {
                summarizer = new Highlight(SumParams);
            }
            else
            {
                summarizer = new Plain(SumParams);
            }

            return summarizer;
        }

        private static ISearchResults GetSearchResults(SearchParameters SearchParams, string SearchType)
        {
            //Finally create search object and pass ISearchResults to updater function
            var search = new Search(SearchParams);
            ISearchResults searchResults;
            switch (SearchType)
            {
                case "MultiRelevance":
                    searchResults = search.ResultsMultiRelevance();
                    break;
                case "MultiAnd":
                    searchResults = search.ResultsMultiAnd();
                    break;
                case "SimpleOr":
                    searchResults = search.ResultsSimpleOr();
                    break;
                case "AsEntered":
                    searchResults = search.ResultsAsEntered();
                    break;
                default:
                    searchResults = search.ResultsMultiRelevance();
                    break;
            }

            return searchResults;
        }


        private static IEnumerable<FullTextResult> ConvertSearchResultsToFullTextResults(ISearchResults SearchResults, Summarizer Summarizer, int PageNumber = 0)
        {
            IEnumerable<SearchResult> results;
            results = SearchResults.AsEnumerable();

            var numNodesInSet = 0;
            var resultNumber = 0;
            var allResults = new List<FullTextResult>();
            var returnAllFieldsInXslt = Config.Instance.GetBooleanByKey("ReturnAllFieldsInXSLT");
            foreach (var result in results)
            {
                //var resultNumber = toSkip + numNodesInSet + 1;
                resultNumber++;
                OnResultOutput(new ResultOutputEventArgs(result, PageNumber, resultNumber, numNodesInSet + 1));

                var ftResult = new FullTextResult();
                ftResult.ExamineResult = result;
                ftResult.NodeId = result.Id;
                ftResult.Score = result.Score;
                ftResult.Number = resultNumber;

                var nodeTypeAlias = result.Fields.ContainsKey("nodeTypeAlias") ? result.Fields["nodeTypeAlias"] : "[Need to add 'nodeTypeAlias' to <IndexAttributeFields>]";
                ftResult.NodeTypeAlias = nodeTypeAlias;

                //var node = new XElement("node",
                //    new XAttribute("id", ),
                //    new XAttribute("score", ),
                //    new XAttribute("number", )
                //);

                if (returnAllFieldsInXslt)
                {
                    //Add all fields from index, you would think this would slow things
                    //down, but it doesn't (that much) really, could be useful
                    //foreach (var field in result.Fields)
                    //{
                    //    ftResult.Fields.Add(field.Key, field.Value);
                    //}
                    ftResult.Fields = result.Fields;
                }

                //Add title (optionally highlighted)
                string title;
                Summarizer.GetTitle(result, out title);
                ftResult.Title = title;

                //Add Summary(optionally highlighted)
                string summary;
                Summarizer.GetSummary(result, out summary);
                ftResult.Summary = summary;

                allResults.Add(ftResult);
                numNodesInSet++;
            }

            return allResults;
        }


        #region Search overloads using default values
        // These all just call Search with some default parameters
        public static SearchResultsCollection SearchMultiRelevance(string SearchTerm, string TitleProperties, string BodyProperties, string RootNodes, string TitleLinkProperties, string SummaryProperties, int UseHighlighting, int SummaryLength, int PageNumber = 0, int PageLength = 0, string Fuzzieness = "1.0", int Wildcard = 0)
        {
            return Search("MultiRelevance", SearchTerm, TitleProperties, BodyProperties, RootNodes, TitleLinkProperties, SummaryProperties, UseHighlighting, SummaryLength, PageNumber, PageLength, Fuzzieness, Wildcard);
        }

        public static SearchResultsCollection SearchMultiRelevance(string SearchTerm, string RootNodes, int PageNumber = 0, int PageLength = 0)
        {
            return Search("MultiRelevance", SearchTerm, "nodeName", Config.Instance.GetLuceneFtField(), RootNodes, "nodeName", Config.Instance.GetLuceneFtField(), 1, 0, PageNumber, PageLength, "0.8");
        }

        public static SearchResultsCollection SearchMultiAnd(string SearchTerm, string TitleProperties, string BodyProperties, string RootNodes, string TitleLinkProperties, string SummaryProperties, int UseHighlighting, int SummaryLength, int PageNumber = 0, int PageLength = 0, string Fuzzieness = "1.0", int Wildcard = 0)
        {
            return Search("MultiAnd", SearchTerm, TitleProperties, BodyProperties, RootNodes, TitleLinkProperties, SummaryProperties, UseHighlighting, SummaryLength, PageNumber, PageLength, Fuzzieness, Wildcard);
        }

        public static SearchResultsCollection SearchMultiAnd(string SearchTerm, string RootNodes, int PageNumber = 0, int PageLength = 0)
        {
            return Search("MultiAnd", SearchTerm, "nodeName", Config.Instance.GetLuceneFtField(), RootNodes, "nodeName", Config.Instance.GetLuceneFtField(), 1, 0, PageNumber, PageLength, "0.8");
        }

        public static SearchResultsCollection SearchSimpleOr(string SearchTerm, string TitleProperties, string BodyProperties, string RootNodes, string TitleLinkProperties, string SummaryProperties, int UseHighlighting, int SummaryLength, int PageNumber = 0, int PageLength = 0, string Fuzzieness = "1.0", int Wildcard = 0)
        {
            return Search("SimpleOr", SearchTerm, TitleProperties, BodyProperties, RootNodes, TitleLinkProperties, SummaryProperties, UseHighlighting, SummaryLength, PageNumber, PageLength, Fuzzieness, Wildcard);
        }

        public static SearchResultsCollection SearchSimpleOr(string SearchTerm, string RootNodes, int PageNumber = 0, int PageLength = 0)
        {
            return Search("SimpleOr", SearchTerm, "nodeName", Config.Instance.GetLuceneFtField(), RootNodes, "nodeName", Config.Instance.GetLuceneFtField(), 1, 0, PageNumber, PageLength, "0.8");
        }

        public static SearchResultsCollection SearchAsEntered(string SearchTerm, string TitleProperties, string BodyProperties, string RootNodes, string TitleLinkProperties, string SummaryProperties, int UseHighlighting, int SummaryLength, int PageNumber = 0, int PageLength = 0, string Fuzzieness = "1.0", int Wildcard = 0)
        {
            return Search("AsEntered", SearchTerm, TitleProperties, BodyProperties, RootNodes, TitleLinkProperties, SummaryProperties, UseHighlighting, SummaryLength, PageNumber, PageLength, Fuzzieness, Wildcard);
        }

        public static SearchResultsCollection SearchAsEntered(string SearchTerm, string RootNodes, int PageNumber = 0, int PageLength = 0)
        {
            return Search("AsEntered", SearchTerm, "nodeName", Config.Instance.GetLuceneFtField(), RootNodes, "nodeName", Config.Instance.GetLuceneFtField(), 1, 0, PageNumber, PageLength, "0.8");
        }
        #endregion

        #region Private helper methods
        /// <summary>
        /// Split up the comma separated string and return a list of UmbracoProperty objects
        /// </summary>
        /// <param name="CommaSeparated"></param>
        /// <param name="Boost"></param>
        /// <param name="Fuzzy"></param>
        /// <param name="Wildcard"></param>
        /// <returns></returns>
        static List<UmbracoProperty> GetProperties(string CommaSeparated, double Boost, double Fuzzy, bool Wildcard)
        {
            var properties = new List<UmbracoProperty>();
            if (!string.IsNullOrEmpty(CommaSeparated))
            {
                foreach (var propName in CommaSeparated.Split(','))
                {
                    if (!string.IsNullOrEmpty(propName))
                    {
                        properties.Add(new UmbracoProperty(propName, Boost, Fuzzy, Wildcard));
                    }
                }
            }

            return properties;
        }

        /// <summary>
        /// Add a list of properties to use in summary text/body to supplied SummarizerParameters object
        /// </summary>
        /// <param name="SummaryParameters"></param>
        /// <param name="TitleLinkProperties"></param>
        /// <param name="SummaryProperties"></param>
        /// <param name="Fuzzieness"></param>
        /// <param name="Wildcard"></param>
        static void AddSummaryProperties(SummarizerParameters SummaryParameters, string TitleLinkProperties, string SummaryProperties, double Fuzzieness, bool Wildcard)
        {
            var titleBoost = Config.Instance.GetSearchTitleBoost();
            var titleSummary = GetProperties(TitleLinkProperties, titleBoost, Fuzzieness, Wildcard);
            SummaryParameters.TitleLinkProperties = titleSummary.Count > 0 ? titleSummary : new List<UmbracoProperty> { new UmbracoProperty("nodeName", titleBoost, Fuzzieness, Wildcard) };

            var bodySummary = GetProperties(SummaryProperties, 1.0, Fuzzieness, Wildcard);
            bodySummary.Add(new UmbracoProperty(Config.Instance.GetLuceneFtField(), 1.0, Fuzzieness, Wildcard));
            SummaryParameters.BodySummaryProperties = bodySummary.Count > 0 ? bodySummary : new List<UmbracoProperty> { new UmbracoProperty(Config.Instance.GetLuceneFtField(), 1.0, Fuzzieness, Wildcard) };
        }

        /// <summary>
        /// private function, called by Search to populate a list of umbraco properties to pass to the Search class
        /// </summary>
        /// <param name="TitleProperties"></param>
        /// <param name="BodyProperties"></param>
        /// <param name="Fuzzieness"></param>
        /// <param name="Wildcard"></param>
        /// <returns></returns>
        static List<UmbracoProperty> GetSearchProperties(string TitleProperties, string BodyProperties, double Fuzzieness, bool Wildcard)
        {
            var searchProperties = new List<UmbracoProperty>();
            var titleBoost = Config.Instance.GetSearchTitleBoost();
            searchProperties.AddRange(GetProperties(TitleProperties, titleBoost, Fuzzieness, Wildcard));
            searchProperties.AddRange(GetProperties(BodyProperties, 1.0, Fuzzieness, Wildcard));
            return searchProperties.Count > 0 ? searchProperties : null;
        }

        /// <summary>
        /// called by Search to get a list of the root nodes from the passed string
        /// </summary>
        /// <param name="RootNodes">Comma separated string from XSLT</param>
        /// <returns>List of integers</returns>
        static List<int> GetRootNotes(string RootNodes)
        {
            if (string.IsNullOrEmpty(RootNodes))
                return null;
            var rootNodesList = new List<int>();
            foreach (var nodeString in RootNodes.Split(','))
            {
                int node;
                if (Int32.TryParse(nodeString, out node))
                    rootNodesList.Add(node);
            }
            return rootNodesList;
        }

        #endregion


        #region Event
        public static void OnResultOutput(ResultOutputEventArgs e)
        {
            if (ResultOutput != null)
                ResultOutput(null, e);
        }
        #endregion
    }
}
