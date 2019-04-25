using System.Collections.Generic;
using Umbraco.Core.Models;

namespace Dragonfly.FullTextSearch.Interfaces
{
    /// <summary>
    /// The Full text indexer retrieves the page HTML (either by using a renderer, or by retrieving from cache),
    /// and adds it to the supplied fields dictionary to be stored in the lucene index.
    /// setting cancelIndexing to true will prevent currentDocuemnt from being put into the index.
    /// Any class implementing this interface can register itself as a fulltextindexer for any or all 
    /// node types using the Manager singleton
    /// </summary>
    public interface IFullTextIndexer
    {
        void NodeProcessor(IContent CurrentContent, Dictionary<string, string> Fields, out bool CancelIndexing);
    }
}
