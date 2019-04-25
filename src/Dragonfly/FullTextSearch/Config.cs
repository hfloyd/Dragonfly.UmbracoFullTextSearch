namespace Dragonfly.FullTextSearch
{
    using System;
    using System.Collections.Generic;
    using System.Web;
    using System.IO;
    using System.Xml;
    using System.Text.RegularExpressions;
    using Umbraco.Core.Logging;

    /// <summary>
    /// Singleton configuration object for FullTextSearch
    /// </summary>
    public sealed class Config
    {
        // configuration cache
        private XmlDocument _configDocument;
        private XmlDocument config
        {
            get
            {
                CheckReadConfig();
                return _configDocument;
            }
        }

        private string _filePath;
        private Dictionary<string, string> _singleCache;
        private DateTime _lastModified;
        private Config()
        {
            _singleCache = new Dictionary<string, string>();
        }
        /// <summary>
        /// singleton
        /// </summary>
        public static Config Instance
        {
            get { return Nested.instance; }
        }

        private class Nested
        {
            static Nested()
            {
            }
            internal static readonly Config instance = new Config();
        }
        /// <summary>
        /// Gets the InnerText of a configuration key, to just have a simple string value, but use as you like 
        /// </summary>
        /// <param name="key">key name</param>
        /// <returns>value</returns>
        public string GetByKey(string key)
        {
            CheckReadConfig();
            if (_singleCache.ContainsKey(key))
                return _singleCache[key];
            key = Regex.Replace(key, @"[^A-Za-z0-9_\-]", string.Empty);
            var node = config.SelectSingleNode("/FullTextSearch/" + key);
            if (node != null)
            {
                _singleCache.Add(key, node.InnerText);
                return node.InnerText;
            }
            _singleCache.Add(key, string.Empty);
            return string.Empty;
        }
        /// <summary>
        /// Get a multi value key
        /// </summary>
        /// <param name="key">name of the outermost tag</param>
        /// <returns>list of strings</returns>
        public List<string> GetMultiByKey(string key)
        {
            var values = new List<string>();
            key = Regex.Replace(key, @"[^A-Za-z0-9_\-]", string.Empty);
            var configNodes = config.SelectNodes("/FullTextSearch/" + key + "/add");
            if (configNodes != null && configNodes.Count > 0)
            {
                foreach (XmlNode node in configNodes)
                {
                    if (node.Attributes != null)
                    {
                        var nameAttribute = node.Attributes["name"];
                        if (nameAttribute != null)
                        {
                            var name = nameAttribute.Value;
                            if (!string.IsNullOrEmpty(name))
                            {
                                values.Add(name);
                            }
                        }
                        else
                        {
                            var msg = string.Format("FullText Search Configuration Value error for '{0}' - missing 'name' attribute", key);
                            LogHelper.Warn<Config>(msg);
                        }
                    }
                    else
                    {
                        var msg = string.Format("FullText Search Configuration Value error for '{0}'", key);
                        LogHelper.Warn<Config>(msg);
                    }
                }
            }
            else
            {
                var msg = string.Format("FullText Search Configuration Value not found for '{0}'", key);
                LogHelper.Warn<Config>(msg);
            }

            return values;
        }
        /// <summary>
        /// Check whether the a given key is set to boolean True/False
        /// </summary>
        /// <returns>true on enabled</returns>
        public bool GetBooleanByKey(string key)
        {
            var s = GetByKey(key);
            return !string.IsNullOrWhiteSpace(s) && (s == "1" || s.ToLower() == "true");
        }
        public double? GetDoubleByKey(string key)
        {
            double d;
            var s = GetByKey(key);
            if (string.IsNullOrEmpty(s) || !double.TryParse(s, out d))
            {
                return null;
            }
            return d;
        }
        /// <summary>
        /// return the name of the lucene field we fill with the full text
        /// </summary>
        public string GetLuceneFtField()
        {
            var value = GetByKey("LuceneFTField");
            return string.IsNullOrEmpty(value) ? "FullTextSearch" : value;
        }
        public double GetSearchFuzzieness()
        {
            var fuzz = GetDoubleByKey("SearchFuzzieness") ?? 1.0;
            return fuzz;
        }
        public double GetSearchTitleBoost()
        {
            var boost = GetDoubleByKey("SearchTitleBoost") ?? 10.0;
            return boost;
        }
        /// <summary>
        /// Needs to be kept track of, but not really changed.
        /// </summary>
        public string GetPathPropertyName()
        {
            return "FullTextPath";
        }
        /// <summary>
        /// Check if the config file has been loaded, if not read it into memory
        /// </summary>
        /// <returns>bool indicating sucessfull/unsucessfull load</returns>
        private bool CheckReadConfig()
        {
            if (_configDocument == null || string.IsNullOrEmpty(_filePath) || File.GetLastWriteTime(_filePath).CompareTo(_lastModified) > 0)
            {
                _configDocument = new XmlDocument();
                _filePath = HttpContext.Current != null ? HttpContext.Current.Server.MapPath("/config/FullTextSearch.config") : System.Web.Hosting.HostingEnvironment.MapPath("/config/FullTextSearch.config");
                try
                {
                    _configDocument.Load(_filePath);
                    _lastModified = File.GetLastWriteTime(_filePath);
                    _singleCache = new Dictionary<string, string>();
                    return true;
                }
                catch (IOException ex)
                {
                    _configDocument = null;
                    _filePath = string.Empty;
                    LogHelper.Error(GetType(), "Error loading configuration in FullTextSearch.", ex);
                }
                catch (XmlException ex)
                {
                    _configDocument = null;
                    _filePath = string.Empty;
                    LogHelper.Error(GetType(), "Error parsing configuration in FullTextSearch.", ex);
                }
                return false;
            }
            return true;
        }
    }
}