using System.Collections.Generic;
using Newtonsoft.Json;

namespace Foundation.Sdk
{
    /// <summary>
    /// Class representing a result set
    /// </summary>
    public class SearchResults
    {
        /// <summary>
        /// Gets/sets the total number of objects in the collection
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// Gets/sets the total number of objects returned in the result set
        /// </summary>
        public int Count => Items.Count;

        /// <summary>
        /// Gets/sets the starting value from which the result set was taken
        /// </summary>
        /// <remarks>
        /// For example, if the search results are used for pagination in a UI
        /// and this result set was for page 2 (and where each peage shows 10
        /// items), then this value would be 11.
        /// </remarks>
        public int From { get; set; }

        /// <summary>
        /// Gets/sets the instances of T that were returned in the result set
        /// </summary>
        public List<string> Items { get; set; } = new List<string>();

        /// <summary>
        /// Stringifies the items in the search results
        /// </summary>
        /// <returns>String representation of a Json array. The Json array represents the items returned in the search results.</returns>
        public string StringifyItems() => "[ " + string.Join(",", Items) + " ]";

        /// <summary>
        /// Gets a list of typed objects
        /// </summary>
        /// <typeparam name="T">The type of each item in the list, where T is a class (i.e. a reference type)</typeparam>
        /// <returns>List of typed objects</returns>
        public List<T> GetTypedItems<T>() where T : class
        {
            List<T> items = new List<T>(Items.Count);

            foreach (var itemStr in Items)
            {
                var itemObj = JsonConvert.DeserializeObject<T>(itemStr);
                items.Add(itemObj);
            }

            return items;
        }
    }
}