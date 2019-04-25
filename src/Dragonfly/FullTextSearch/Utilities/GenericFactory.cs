namespace Dragonfly.FullTextSearch.Utilities
{
    using System.Collections.Generic;

    /// <summary>
    /// Allows different classes implementing the same interfaces to be mapped to strings, 
    /// the string passed to CreateNew then decides which implementation gets instantiated and 
    /// passed back to the caller.
    /// </summary>
    /// <typeparam name="TInterface"></typeparam>
    public class GenericFactory<TInterface>
    {
        private Dictionary<string,CreateDelegate> _mapping;
        private CreateDelegate _defaultClass;
        private delegate TInterface CreateDelegate();
        /// <summary>
        /// Register Class to be created when key is passed to CreateNew
        /// </summary>
        /// <typeparam name="TClass">Any Class Implementing Interface</typeparam>
        /// <param name="key">the string to identify Class by</param>
        public void Register<TClass>(string key) where TClass : TInterface, new()
        {
            if (_mapping == null)
                _mapping = new Dictionary<string, CreateDelegate>();

            CreateDelegate createme = CreateFunction<TClass>;
            if(! _mapping.ContainsKey(key) )
                _mapping.Add(key,createme);
        }
        /// <summary>
        /// Register the class that will be returned when no string "key" is found
        /// in the dictionary by CreateNew
        /// </summary>
        /// <typeparam name="TClass">Any Class Implementing Interface</typeparam>
        public void RegisterDefault<TClass>() where TClass : TInterface, new()
        {
            CreateDelegate createme = CreateFunction<TClass>;
            _defaultClass = createme;
        }
        /// <summary>
        /// Instanciate the class regestered for key and return the object.
        /// return a default class if key is not found in the dictionary
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TInterface CreateNew(string key)
        {
            CreateDelegate createme;
            if (_mapping != null && _mapping.ContainsKey(key))
                createme = _mapping[key];
            else
                createme = _defaultClass;
            return createme();
        }
        private TInterface CreateFunction<TClass>() where TClass : TInterface, new()
        {
            return new TClass();
        }
    }
}