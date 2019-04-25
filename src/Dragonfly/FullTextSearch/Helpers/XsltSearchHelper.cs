namespace Dragonfly.FullTextSearch.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using Dragonfly.FullTextSearch.HighlightTools;
    using Dragonfly.FullTextSearch.Models;
    using Dragonfly.FullTextSearch.SearchTools;
    using Examine;

    //TODO: HLF - Evaluate if necessary

    /// <summary>
    /// Retrieve search results as Xml node objects for users to use in their own XSLT
    /// </summary>
    public class XsltSearchHelper
    {
        /// <summary>
        /// Quick and simple static event to allow users to modify search results
        /// before they are output
        /// </summary>
        public static event EventHandler<ResultOutputEventArgs> ResultOutput;

        /// <summary>
        /// Main XSLT Helper Search function, the laundry list of parameters are documented more fully in FullTextSearch.xslt
        /// Basically this constructs a search object and a highlighter object from the parameters, then calls another 
        /// function to return search results as XML.
        /// </summary>
        /// <param name="searchType">MultiRelevance, MultiAnd, etc.</param>
        /// <param name="searchTerm">The search terms as entered by user</param>
        /// <param name="titleProperties">A list of umbraco properties, comma separated, to be searched as the page title</param>
        /// <param name="bodyProperties">A list of umbraco properties, comma separated, to be searched as the page body</param>
        /// <param name="rootNodes">Return only results under these nodes, set to blank or -1 to search all nodes</param>
        /// <param name="titleLinkProperties">Umbraco properties, comma separated, to use in forming the (optionally highlighted) title</param>
        /// <param name="summaryProperties">Umbraco properties, comma separated, to use in forming the (optionally highlighted) summary text</param>
        /// <param name="useHighlighting">Enable context highlighting(note this can slow things down)</param>
        /// <param name="summaryLength">Number of characters in the summary text</param>
        /// <param name="pageNumber">Page number of results to return</param>
        /// <param name="pageLength">Number of results on each page, zero disables paging and returns all results</param>
        /// <param name="fuzzieness">Amount 0-1 to "fuzz" the search, return non exact matches</param>
        /// <param name="wildcard">Add wildcard to the end of search term. Doesn't work together with fuzzyness</param>
        /// <returns></returns>
        public static XPathNodeIterator Search(string searchType, string searchTerm, string titleProperties, string bodyProperties, string rootNodes, string titleLinkProperties, string summaryProperties, int useHighlighting, int summaryLength, int pageNumber = 0, int pageLength = 0, string fuzzieness = "1.0", int wildcard = 0)
        {
            // Measure time taken. This could be done more neatly, but this is more accurate
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            // Check search terms were actually entered
            if (String.IsNullOrEmpty(searchTerm))
                return ReturnError("NoTerms", "You must enter a search term");
            // Setup search parameters
            double fuzzy;
            if (String.IsNullOrEmpty(fuzzieness) || !Double.TryParse(fuzzieness, out fuzzy))
                fuzzy = 1.0;
            var wildcardBool = wildcard > 0;
            var searchParameters = new SearchParameters();
            var searchProperties = GetSearchProperties(titleProperties, bodyProperties, fuzzy, wildcardBool);
            if (searchProperties != null)
                searchParameters.SearchProperties = searchProperties;
            searchParameters.RootNodes = GetRootNotes(rootNodes);
            searchParameters.SearchTerm = searchTerm;

            //Setup summarizer parameters
            var summaryParameters = new SummarizerParameters { SearchTerm = searchTerm };
            if (summaryLength > 0)
                summaryParameters.SummaryLength = summaryLength;
            AddSummaryProperties(summaryParameters, titleLinkProperties, summaryProperties, fuzzy, wildcardBool);
            // Create summarizer according to highlighting option
            Summarizer summarizer;
            if (useHighlighting > 0)
                summarizer = new Highlight(summaryParameters);
            else
                summarizer = new Plain(summaryParameters);
            //Finally create search object and pass ISearchResults to XML renderer
            var search = new Search(searchParameters);
            switch (searchType)
            {
                case "MultiAnd":
                    return ResultsAsXml(search.ResultsMultiAnd(), summarizer, pageNumber, pageLength, stopwatch);
                case "SimpleOr":
                    return ResultsAsXml(search.ResultsSimpleOr(), summarizer, pageNumber, pageLength, stopwatch);
                case "AsEntered":
                    return ResultsAsXml(search.ResultsAsEntered(), summarizer, pageNumber, pageLength, stopwatch);
                default:
                    return ResultsAsXml(search.ResultsMultiRelevance(), summarizer, pageNumber, pageLength, stopwatch);
            }
        }

        // These all just call Search with some default parameters
        public static XPathNodeIterator SearchMultiRelevance(string searchTerm, string titleProperties, string bodyProperties, string rootNodes, string titleLinkProperties, string summaryProperties, int useHighlighting, int summaryLength, int pageNumber = 0, int pageLength = 0, string fuzzieness = "1.0", int wildcard = 0)
        {
            return Search("MultiRelevance", searchTerm, titleProperties, bodyProperties, rootNodes, titleLinkProperties, summaryProperties, useHighlighting, summaryLength, pageNumber, pageLength, fuzzieness, wildcard);
        }
        public static XPathNodeIterator SearchMultiRelevance(string searchTerm, string rootNodes, int pageNumber = 0, int pageLength = 0)
        {
            return Search("MultiRelevance", searchTerm, "nodeName", Config.Instance.GetLuceneFtField(), rootNodes, "nodeName", Config.Instance.GetLuceneFtField(), 1, 0, pageNumber, pageLength, "0.8");
        }
        public static XPathNodeIterator SearchMultiAnd(string searchTerm, string titleProperties, string bodyProperties, string rootNodes, string titleLinkProperties, string summaryProperties, int useHighlighting, int summaryLength, int pageNumber = 0, int pageLength = 0, string fuzzieness = "1.0", int wildcard = 0)
        {
            return Search("MultiAnd", searchTerm, titleProperties, bodyProperties, rootNodes, titleLinkProperties, summaryProperties, useHighlighting, summaryLength, pageNumber, pageLength, fuzzieness, wildcard);
        }
        public static XPathNodeIterator SearchMultiAnd(string searchTerm, string rootNodes, int pageNumber = 0, int pageLength = 0)
        {
            return Search("MultiAnd", searchTerm, "nodeName", Config.Instance.GetLuceneFtField(), rootNodes, "nodeName", Config.Instance.GetLuceneFtField(), 1, 0, pageNumber, pageLength, "0.8");
        }
        public static XPathNodeIterator SearchSimpleOr(string searchTerm, string titleProperties, string bodyProperties, string rootNodes, string titleLinkProperties, string summaryProperties, int useHighlighting, int summaryLength, int pageNumber = 0, int pageLength = 0, string fuzzieness = "1.0", int wildcard = 0)
        {
            return Search("SimpleOr", searchTerm, titleProperties, bodyProperties, rootNodes, titleLinkProperties, summaryProperties, useHighlighting, summaryLength, pageNumber, pageLength, fuzzieness, wildcard);
        }
        public static XPathNodeIterator SearchSimpleOr(string searchTerm, string rootNodes, int pageNumber = 0, int pageLength = 0)
        {
            return Search("SimpleOr", searchTerm, "nodeName", Config.Instance.GetLuceneFtField(), rootNodes, "nodeName", Config.Instance.GetLuceneFtField(), 1, 0, pageNumber, pageLength, "0.8");
        }
        public static XPathNodeIterator SearchAsEntered(string searchTerm, string titleProperties, string bodyProperties, string rootNodes, string titleLinkProperties, string summaryProperties, int useHighlighting, int summaryLength, int pageNumber = 0, int pageLength = 0, string fuzzieness = "1.0", int wildcard = 0)
        {
            return Search("AsEntered", searchTerm, titleProperties, bodyProperties, rootNodes, titleLinkProperties, summaryProperties, useHighlighting, summaryLength, pageNumber, pageLength, fuzzieness, wildcard);
        }
        public static XPathNodeIterator SearchAsEntered(string searchTerm, string rootNodes, int pageNumber = 0, int pageLength = 0)
        {
            return Search("AsEntered", searchTerm, "nodeName", Config.Instance.GetLuceneFtField(), rootNodes, "nodeName", Config.Instance.GetLuceneFtField(), 1, 0, pageNumber, pageLength, "0.8");
        }

        //Private methods
        /// <summary>
        /// Split up the comma separated string and retun a list of UmbracoProperty objects
        /// </summary>
        /// <param name="commaSeparated"></param>
        /// <param name="boost"></param>
        /// <param name="fuzzy"></param>
        /// <param name="wildcard"></param>
        /// <returns></returns>
        static List<UmbracoProperty> GetProperties(string commaSeparated, double boost, double fuzzy, bool wildcard)
        {
            var properties = new List<UmbracoProperty>();
            if (!String.IsNullOrEmpty(commaSeparated))
            {
                foreach (var propName in commaSeparated.Split(','))
                {
                    if (!String.IsNullOrEmpty(propName))
                    {
                        properties.Add(new UmbracoProperty(propName, boost, fuzzy, wildcard));
                    }
                }
            }
            return properties;
        }

        /// <summary>
        /// Add a list of properties to use in summary text/body to supplied SummarizerParameters object
        /// </summary>
        /// <param name="summaryParameters"></param>
        /// <param name="titleLinkProperties"></param>
        /// <param name="summaryProperties"></param>
        /// <param name="fuzzieness"></param>
        /// <param name="wildcard"></param>
        static void AddSummaryProperties(SummarizerParameters summaryParameters, string titleLinkProperties, string summaryProperties, double fuzzieness, bool wildcard)
        {
            var titleBoost = Config.Instance.GetSearchTitleBoost();
            var titleSummary = GetProperties(titleLinkProperties, titleBoost, fuzzieness, wildcard);
            summaryParameters.TitleLinkProperties = titleSummary.Count > 0 ? titleSummary : new List<UmbracoProperty> { new UmbracoProperty("nodeName", titleBoost, fuzzieness, wildcard) };

            var bodySummary = GetProperties(summaryProperties, 1.0, fuzzieness, wildcard);
            summaryParameters.BodySummaryProperties = bodySummary.Count > 0 ? bodySummary : new List<UmbracoProperty> { new UmbracoProperty(Config.Instance.GetLuceneFtField(), 1.0, fuzzieness, wildcard) };
        }

        /// <summary>
        /// private function, called by Search to populate a list of umbraco properties to pass to the Search class
        /// </summary>
        /// <param name="titleProperties"></param>
        /// <param name="bodyProperties"></param>
        /// <param name="fuzzieness"></param>
        /// <param name="wildcard"></param>
        /// <returns></returns>
        static List<UmbracoProperty> GetSearchProperties(string titleProperties, string bodyProperties, double fuzzieness, bool wildcard)
        {
            var searchProperties = new List<UmbracoProperty>();
            var titleBoost = Config.Instance.GetSearchTitleBoost();
            searchProperties.AddRange(GetProperties(titleProperties, titleBoost, fuzzieness, wildcard));
            searchProperties.AddRange(GetProperties(bodyProperties, 1.0, fuzzieness, wildcard));
            return searchProperties.Count > 0 ? searchProperties : null;
        }

        /// <summary>
        /// called by Search to get a list of the root nodes from the passed string
        /// </summary>
        /// <param name="rootNodes">Comma separated string from XSLT</param>
        /// <returns>List of integers</returns>
        static List<int> GetRootNotes(string rootNodes)
        {
            if (String.IsNullOrEmpty(rootNodes))
                return null;
            var rootNodesList = new List<int>();
            foreach (var nodeString in rootNodes.Split(','))
            {
                int node;
                if (Int32.TryParse(nodeString, out node))
                    rootNodesList.Add(node);
            }
            return rootNodesList;
        }
        /// <summary>
        /// Take ISearchResults from examine, create title and body summary, and convert to an XML document
        /// This is broadly based off the same function in the Examine codebase, the XML it returns should be 
        /// broadly compatible, that seems best...
        /// </summary>
        /// <returns>XPathNodeIterator to return to Umbraco XSLT foreach</returns>
        static XPathNodeIterator ResultsAsXml(ISearchResults searchResults, Summarizer Summarizer, int pageNumber = 0, int pageLength = 0, Stopwatch stopwatch = null)
        {
            var output = new XDocument();
            var numNodesInSet = 0;
            var numResults = searchResults.TotalItemCount;
            if (numResults < 1)
                return ReturnError("NoResults", "Your search returned no results");
            IEnumerable<SearchResult> results;
            var toSkip = 0;
            if (pageLength > 0)
            {
                if (pageNumber > 1)
                {
                    toSkip = (pageNumber - 1) * pageLength;
                }
                results = searchResults.Skip(toSkip).Take(pageLength);
            }
            else
            {
                results = searchResults.AsEnumerable();
            }
            var rootNode = new XElement("results");
            var nodesNode = new XElement("nodes");
            var returnAllFieldsInXslt = Config.Instance.GetBooleanByKey("ReturnAllFieldsInXSLT");
            foreach (var result in results)
            {
                var resultNumber = toSkip + numNodesInSet + 1;
                OnResultOutput(new ResultOutputEventArgs(result, pageNumber, resultNumber, numNodesInSet + 1));
                var node = new XElement("node",
                    new XAttribute("id", result.Id),
                    new XAttribute("score", result.Score),
                    new XAttribute("number", resultNumber)
                );
                if (returnAllFieldsInXslt)
                {
                    //Add all fields from index, you would think this would slow things
                    //down, but it doesn't (that much) really, could be useful
                    foreach (var field in result.Fields)
                    {
                        node.Add(
                            new XElement("data",
                                new XAttribute("alias", field.Key),
                                new XCData(field.Value)
                            ));
                    }
                }
                //Add title (optionally highlighted)
                string title;
                Summarizer.GetTitle(result, out title);
                node.Add(
                    new XElement("data",
                    new XAttribute("alias", "FullTextTitle"),
                    new XCData(title)
                ));
                //Add Summary(optionally highlighted)
                string summary;
                Summarizer.GetSummary(result, out summary);
                node.Add(
                    new XElement("data",
                        new XAttribute("alias", "FullTextSummary"),
                        new XCData(summary)
                ));

                nodesNode.Add(node);
                numNodesInSet++;
            }
            if (numNodesInSet > 0)
            {
                rootNode.Add(nodesNode);
                var summary = new XElement("summary");
                summary.Add(new XAttribute("numResults", numResults));
                var numPages = numResults % pageLength == 0 ? numResults / pageLength : numResults / pageLength + 1;
                summary.Add(new XAttribute("numPages", numPages));
                if (stopwatch != null)
                {
                    stopwatch.Stop();
                    double millisecs = stopwatch.ElapsedMilliseconds;
                    var numSecs = Math.Round((millisecs / 1000), 3);
                    summary.Add(new XAttribute("timeTaken", numSecs));
                }
                summary.Add(new XAttribute("firstResult", toSkip + 1));
                var lastResult = toSkip + pageLength;
                if (lastResult > numResults)
                    lastResult = numResults;
                summary.Add(new XAttribute("lastResult", lastResult));
                rootNode.Add(summary);
                output.Add(rootNode);
            }
            else
                return ReturnError("NoPage", "Pagination incorrectly set up, no results on page " + pageNumber);

            return output.CreateNavigator().Select("/");
        }
        /// <summary>
        /// Quick function to return errors to XSLT to be handled there
        /// </summary>
        /// <param name="shortMessage">A code that can be checked for in the XSLT and replaced with appropriate dictionary entry</param>
        /// <param name="longMessage">Some text that will be used if dictionary entry is not available/for debugging</param>
        /// <returns></returns>
        static XPathNodeIterator ReturnError(string shortMessage, string longMessage)
        {
            var output = new XDocument();
            output.Add(new XElement("error",
                new XAttribute("type", shortMessage),
                new XCData(longMessage)
            ));
            return output.CreateNavigator().Select("/");
        }

        public static void OnResultOutput(ResultOutputEventArgs e)
        {
            if (ResultOutput != null)
                ResultOutput(null, e);
        }

        #region Remove?

        /// <summary>
        /// This is budget. But params are not supported by MS XSLT, so we create a real method and a bunch of overloads. 
        /// As far as I'm aware, no, there isn't any way of doing this that is less painful and ugly
        /// than, say, root canal without anasthetic performed by the elephant man.
        /// </summary>
        /// <param name="Format"></param>
        /// <param name="Args"></param>
        /// <returns></returns>
        private static string StringFormatInternal(string Format, params string[] Args)
        {
            string result;
            try
            {
                result = String.Format(Format, Args);
            }
            catch (FormatException)
            {
                result = "Format string '" + Format + "' incorrectly formated";
            }
            return result;
        }

        public static string StringFormat(string Format, string Arg1)
        {
            return StringFormatInternal(Format, Arg1);
        }

        public static string StringFormat(string Format, string Arg1, string Arg2)
        {
            return StringFormatInternal(Format, Arg1, Arg2);
        }

        public static string StringFormat(string Format, string Arg1, string Arg2, string Arg3)
        {
            return StringFormatInternal(Format, Arg1, Arg2, Arg3);
        }

        public static string StringFormat(string Format, string Arg1, string Arg2, string Arg3, string Arg4)
        {
            return StringFormatInternal(Format, Arg1, Arg2, Arg3, Arg4);
        }

        public static string StringFormat(string Format, string Arg1, string Arg2, string Arg3, string Arg4, string Arg5)
        {
            return StringFormatInternal(Format, Arg1, Arg2, Arg3, Arg4, Arg5);
        }

        public static string StringFormat(string Format, string Arg1, string Arg2, string Arg3, string Arg4, string Arg5, string Arg6)
        {
            return StringFormatInternal(Format, Arg1, Arg2, Arg3, Arg4, Arg5, Arg6);
        }

        // If you need more than 7 argmumennts you're SOL. Sorry. 

        public static string StringFormat(string Format, string Arg1, string Arg2, string Arg3, string Arg4, string Arg5, string Arg6, string Arg7)
        {
            return StringFormatInternal(Format, Arg1, Arg2, Arg3, Arg4, Arg5, Arg6, Arg7);
        }

        #endregion
    }
}
