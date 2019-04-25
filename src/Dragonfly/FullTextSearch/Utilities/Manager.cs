namespace Dragonfly.FullTextSearch.Utilities
{
    using Dragonfly.FullTextSearch.Indexers;
    using Dragonfly.FullTextSearch.Interfaces;
    using Dragonfly.FullTextSearch.Renderers;

    public sealed class Manager
    {
        public GenericFactory<IDocumentRenderer> DocumentRendererFactory { get; set; }
        public GenericFactory<IFullTextIndexer> FullTextIndexerFactory { get; set; }

        // We enclose the generic factory in this singleton, rather than make that class itself 
        // a singleton so we can set up the default handlers here.
        private Manager()
        {
            var config = Config.Instance;
            DocumentRendererFactory = new GenericFactory<IDocumentRenderer>();
            FullTextIndexerFactory = new GenericFactory<IFullTextIndexer>();

            if (Config.Instance.GetBooleanByKey("PublishEventRendering"))
            {
                string defaultRenderer = config.GetByKey("DefaultRenderer");
                if (defaultRenderer.ToLower().Contains("program"))
                {
                    DocumentRendererFactory.RegisterDefault<ProgramaticRenderer>();
                }
                else
                {
                    DocumentRendererFactory.RegisterDefault<HttpPublishEventRenderer>();
                }
                FullTextIndexerFactory.RegisterDefault<CacheIndexer>();
            }
            else
            {
                DocumentRendererFactory.RegisterDefault<DefaultHttpRenderer>();
                FullTextIndexerFactory.RegisterDefault<DefaultIndexer>();
            }
            
        }
        /// <summary>
        /// singleton
        /// </summary>
        public static Manager Instance
        {
            get { return NestedManager.instance; }
        }

        private class NestedManager
        {
            static NestedManager()
            {
            }
            internal static readonly Manager instance = new Manager();
        }
    }
}