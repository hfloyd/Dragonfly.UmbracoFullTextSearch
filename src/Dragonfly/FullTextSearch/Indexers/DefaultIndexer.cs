namespace Dragonfly.FullTextSearch.Indexers
{
    using System.Collections.Generic;
    using System.Linq;
    using Dragonfly.FullTextSearch.Interfaces;
    using Dragonfly.FullTextSearch.Utilities;
    using Umbraco.Core.Logging;
    using Umbraco.Core.Models;

    /// <summary>
    /// Default indexer class. Used for all indexers in this project
    /// </summary>
    public class DefaultIndexer : IFullTextIndexer
    {
        protected IContent CurrentContent;

        /// <summary>
        /// Fully process the current node, check whether to cancel indexing, check whether to index the node
        /// retrieve the HTML and add it to the index. Then make a cup of tea. This is tiring. 
        /// </summary>
        /// <param name="currentContent"></param>
        /// <param name="fields"></param>
        /// <param name="cancelIndexing"></param>
        public virtual void NodeProcessor(IContent currentContent, Dictionary<string, string> fields, out bool cancelIndexing)
        {
            cancelIndexing = false;
            // this can take a while, if we're running sync this is needed
            Library.SetTimeout(Config.Instance.GetByKey("ScriptTimeout"));

            if (currentContent == null)
                return;
            CurrentContent = currentContent;

            LogHelper.Debug<DefaultIndexer>($"FullTextIndexing: NodeProcessor for {CurrentContent.Name} [{currentContent.Id}]...");
            string fullHtml;
            if (CheckCancelIndexing())
            {
                LogHelper.Debug<DefaultIndexer>($"FullTextIndexing: NodeProcessor for {CurrentContent.Name} [{currentContent.Id}]: CANCELLED");
                cancelIndexing = true;
                return;
            }
            fields.Add(Config.Instance.GetPathPropertyName(), GetPath());
            if (IsIndexable())
            {
                var htmlText = "";
                if (GetHtml(out fullHtml))
                {
                    var ftFieldName = Config.Instance.GetLuceneFtField();
                    htmlText = GetTextFromHtml(ref fullHtml);
                    fields.Add(ftFieldName, htmlText);
                }
                LogHelper.Debug<DefaultIndexer>($"FullTextIndexing: NodeProcessor for {CurrentContent.Name} [{currentContent.Id}] - HTML Length: {fullHtml.Length}  FullText Length: {htmlText.Length}");

            }
            LogHelper.Debug<DefaultIndexer>($"FullTextIndexing: NodeProcessor for {CurrentContent.Name} [{currentContent.Id}]: DONE");

        }

        /// <summary>
        /// Check whether to cancel indexing or not(generally if umbraco(Search/Navi/etc)Hide is set)
        /// </summary>
        /// <returns></returns>
        protected virtual bool CheckCancelIndexing()
        {
            if (Library.IsSearchDisabledByMissingTemplate(CurrentContent))
            {
                return true;
            }

            if (Library.IsSearchDisabledByProperty(CurrentContent))
            {
                return true;
            }

            //if we get here...
            return false;
        }

        /// <summary>
        /// I'm pretty much assuming if we're here and we have a valid document object we should be
        /// trying to index, 
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsIndexable()
        {
            return CurrentContent != null;
        }

        protected virtual string GetPath()
        {
            var path = CurrentContent.Path.Replace(',', ' ');
            path = System.Text.RegularExpressions.Regex.Replace(path, @"^-1 ", string.Empty);
            return path;
        }
        /// <summary>
        /// Get the actual HTML, we use the DefaultHttpRenderer here usually, unless it's been overriden, 
        /// in which case it should be noted that it should only be overriden by renderers using only the 
        /// </summary>
        /// <param name="fullHtml"></param>
        /// <returns></returns>
        protected virtual bool GetHtml(out string fullHtml)
        {
            LogHelper.Debug<DefaultIndexer>($"FullTextIndexing: DefaultIndexer.GetHtml() for {CurrentContent.Name} [{CurrentContent.Id}]...");

            var renderer = Manager.Instance.DocumentRendererFactory.CreateNew(CurrentContent.ContentType.Alias);
            return renderer.Render(CurrentContent.Id, out fullHtml);
        }

        /// <summary>
        /// Use Html Tag stripper to get text from the passed HTML. Certain tags specified in the
        /// config file get removed entirely, head, script, possibly some relevant ids etc.
        /// </summary>
        /// <param name="fullHtml"></param>
        /// <returns>Text to add to index</returns>
        protected virtual string GetTextFromHtml(ref string fullHtml)
        {
            var config = Config.Instance;
            var tagsToStrip = config.GetMultiByKey("TagsToRemove").ToArray();
            //If present in list, remove "head" tag (this breaks the stripper)
            tagsToStrip = tagsToStrip.Where(val => val != "head").ToArray();

            var idsToStrip = config.GetMultiByKey("IdsToRemove").ToArray();

            var tagStripper = new HtmlStrip(tagsToStrip, idsToStrip);
            return tagStripper.TextFromHtml(ref fullHtml);
        }
    }
}