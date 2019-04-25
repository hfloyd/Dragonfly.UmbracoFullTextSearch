namespace Dragonfly.FullTextSearch.Utilities
{
    using System;
    using System.Linq;
    using System.Web;
    using HtmlAgilityPack;
    using System.Text;
    using System.Text.RegularExpressions;
    using Umbraco.Core.Logging;

    /// <summary>
    /// Uses HTML agility pack to do custom tag stripping
    /// </summary>
    class HtmlStrip
    {
        private readonly string[] _tagsToStripContentsOf;
        private readonly string[] _idsToStripContentsOf;

        private readonly bool _continueOnAgilityException;
        /// <summary>
        /// Default constructor, set up sane values
        /// </summary>
        public HtmlStrip()
        {
            _tagsToStripContentsOf = new[] { "head", "script" };
            _idsToStripContentsOf = new string[] { };
            _continueOnAgilityException = true;
        }

        /// <summary>
        /// Overloaded Constructor, allows specifying which tags get stripped
        /// </summary>
        /// <param name="tagsToStripContentsOf">an array of tags the FULL contents of which will be removed, not just the tags themselves</param>
        /// <param name="idsToStripContentsOf">an array of HTML element IDs to fully remove</param>
        /// <param name="continueOnAgilityException"></param>
        public HtmlStrip(string[] tagsToStripContentsOf, string[] idsToStripContentsOf, bool continueOnAgilityException = true)
        {
            _tagsToStripContentsOf = tagsToStripContentsOf;
            _idsToStripContentsOf = idsToStripContentsOf;
            _continueOnAgilityException = continueOnAgilityException;
        }
        /// <summary>
        /// Strips HTML according to the parameters set in the constructor
        /// </summary>
        /// <param name="fullHtml">HTML to strip</param>
        /// <returns>Text</returns>
        public string TextFromHtml(ref string fullHtml)
        {
            if (fullHtml.Length < 1)
            {
                return "";
            }
            var fullText = AgilityTagStrip(fullHtml);
            // the above leaves some residual tags. Search me why, probably the HTML input isn't perfectly
            // formed and the parser is choking on it. Help it along
            fullText = Regex.Replace(fullText, "<[^>]*>", String.Empty);
            // Decode any HTML entities
            fullText = HttpUtility.HtmlDecode(fullText);
            // replace multiple spaces with single spaces. 
            fullText = Regex.Replace(fullText, @"(\s)(\s+)","$1",RegexOptions.Singleline);
            return fullText;
        }
        /// <summary>
        /// Run a tag stripper based on the HTML Agility pack
        /// </summary>
        /// <param name="fullHtml">Html to strip</param>
        /// <returns>Text. Hopefully.</returns>
        private string AgilityTagStrip(string fullHtml)
        {
            var doc = new HtmlDocument();
            try
            {
                //Does this break stuff?
                doc.OptionReadEncoding = false;
                doc.LoadHtml(fullHtml);
            }
            catch (Exception ex)
            {
                if(HttpContext.Current != null)
                    HttpContext.Current.Trace.Warn("Search", "There was an exception cleaning HTML: " + ex);
                LogHelper.Error(GetType(), "HTML Strip exception.", ex);
                // swallow the exception and run the regex based tag stripper on it anyway. Won't be perfect but better than nothing. 
                return _continueOnAgilityException ? fullHtml : string.Empty;
            }
            foreach (var tag in _tagsToStripContentsOf)
            {
                var nodes = doc.DocumentNode.SelectNodes("//" + tag);
                if (nodes != null)
                {
                    foreach (var h in nodes)
                    {
                        h.ParentNode.RemoveChild(h, false);
                    }
                }
            }
            foreach (var h in _idsToStripContentsOf.Select(doc.GetElementbyId).Where(h => h != null))
            {
                h.ParentNode.RemoveChild(h, false);
            }
            var fullTextBuilder = new StringBuilder();
            TagStripHTML(doc.DocumentNode, fullTextBuilder);
            return fullTextBuilder.ToString();
        }
        private void TagStripHTML(HtmlNode root, StringBuilder fullTextBuilder)
        {
            if (root.HasChildNodes)
            {
                foreach (var node in root.ChildNodes)
                {
                    TagStripHTML(node, fullTextBuilder);
                }
            }
            else if (root.NodeType == HtmlNodeType.Text)
            {
                fullTextBuilder.Append(root.InnerText);
                fullTextBuilder.Append(" ");
            }
        }
    }
}