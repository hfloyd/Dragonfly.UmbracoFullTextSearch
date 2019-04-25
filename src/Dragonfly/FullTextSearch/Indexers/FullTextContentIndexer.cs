namespace Dragonfly.FullTextSearch.Indexers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Dragonfly.FullTextSearch.Utilities;
    using Examine;
    using Lucene.Net.Analysis;
    using Umbraco.Core;
    using Umbraco.Core.Logging;
    using Umbraco.Core.Models;
    using UmbracoExamine;
    using UmbracoExamine.DataServices;

    /// <summary>
    /// We could probably just use the events built into the UmbracoContentIndexer for this, 
    /// however, this is just a little easier to implement for certain things, though it uses 99% of
    /// the functionality of the base class.
    /// </summary>
    public class FullTextContentIndexer : UmbracoContentIndexer
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public FullTextContentIndexer()
        { }

        /// <summary>
        /// Constructor to allow for creating an indexer at runtime
        /// </summary>
        /// <param name="indexerData"></param>
        /// <param name="indexPath"></param>
        /// <param name="dataService"></param>
        /// <param name="analyzer"></param>
        /// <param name="async"></param>
        public FullTextContentIndexer(IIndexCriteria indexerData, DirectoryInfo indexPath, IDataService dataService, Analyzer analyzer, bool async)
            : base(indexerData, indexPath, dataService, analyzer, async) { }

        
        /// <summary>
        /// We override this method, do some checks and add some stuff and then just call the base method. 
        /// We could do this from an event, but we want the full text renderer/retriever
        /// to run before any other events we might choose to hook into NodeIndexing so users will find 
        /// it easier to modify what's going on
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="writer"></param> 
        /// <param name="nodeId"></param>
        /// <param name="type"></param>
        protected override void AddDocument(Dictionary<string, string> fields, Lucene.Net.Index.IndexWriter writer, int nodeId, string type)
        {
            if (!Config.Instance.GetBooleanByKey("Enabled"))
            {
                LogHelper.Debug<FullTextContentIndexer>("FullTextSearch is installed, but not enabled (see: /config/FullTextSearch.config)");
            }

            if( type == IndexTypes.Content && Config.Instance.GetBooleanByKey("Enabled")) 
            {
                // running this sync causes HORRIBLE issues when DefaultHttpRender is in use
                if (RunAsync != true && !Config.Instance.GetBooleanByKey("PublishEventRendering"))
                {
                    LogHelper.Error(GetType(), "FullTextSearch Cowardly refusing to break your site by firing up Http Render while indexer is in sync mode. If you see nothing in the index this is why!", null);
                }
                else
                {
                    IContent currentContent;
                    try
                    {
                        // this seems to have a lot of unidentifiable failure modes...
                        currentContent = ApplicationContext.Current.Services.ContentService.GetById(nodeId);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error(GetType(), "Error getting document.", ex);
                        if (Utilities.Library.IsCritical(ex))
                            throw;
                        currentContent = null;
                    }
                    if (currentContent != null && currentContent.Id > 0)
                    {
                        // check if document is protected
                        var path = currentContent.Path;
                        if (!DataService.ContentService.IsProtected(nodeId, path))
                        {
                            bool cancel;
                            var indexer = Manager.Instance.FullTextIndexerFactory.CreateNew(currentContent.ContentType.Alias);
                            indexer.NodeProcessor(currentContent, fields, out cancel);
                            if (cancel)
                                return;
                        }
                    }
                }
            }
            base.AddDocument(fields, writer, nodeId, type);
        }
    }
}