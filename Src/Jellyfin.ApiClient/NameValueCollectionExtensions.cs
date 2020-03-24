using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Web;

namespace Jellyfin.ApiClient
{
    /// <summary>
    /// Extensions for NameValueCollection
    /// </summary>
    public static class NameValueCollectionExtensions
    {
        /// <summary>
        /// Adds the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public static void Add(this NameValueCollection col, string name, int value)
        {
            col.Add(name, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Adds the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public static void Add(this NameValueCollection col, string name, long value)
        {
            col.Add(name, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Adds the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public static void Add(this NameValueCollection col, string name, double value)
        {
            col.Add(name, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Adds if not null or empty.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public static void AddIfNotNullOrEmpty(this NameValueCollection col, string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                col.Add(name, value);
            }
        }

        /// <summary>
        /// Adds if not null.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public static void AddIfNotNull(this NameValueCollection col, string name, int? value)
        {
            if (value.HasValue)
            {
                col.Add(name, value.Value);
            }
        }

        /// <summary>
        /// Adds if not null.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public static void AddIfNotNull(this NameValueCollection col, string name, double? value)
        {
            if (value.HasValue)
            {
                col.Add(name, value.Value);
            }
        }

        /// <summary>
        /// Adds if not null.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public static void AddIfNotNull(this NameValueCollection col, string name, long? value)
        {
            if (value.HasValue)
            {
                col.Add(name, value.Value);
            }
        }

        /// <summary>
        /// Adds the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        public static void Add(this NameValueCollection col, string name, bool value)
        {
            col.Add(name, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Adds if not null.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        public static void AddIfNotNull(this NameValueCollection col, string name, bool? value)
        {
            if (value.HasValue)
            {
                col.Add(name, value.Value);
            }
        }

        /// <summary>
        /// Adds the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">value</exception>
        public static void Add(this NameValueCollection col, string name, IEnumerable<int> value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            col.Add(name, string.Join(",", value.Select(v => v.ToString(CultureInfo.InvariantCulture)).ToArray()));
        }

        /// <summary>
        /// Adds if not null.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public static void AddIfNotNull(this NameValueCollection col, string name, IEnumerable<int> value)
        {
            if (value != null)
            {
                col.Add(name, value);
            }
        }

        /// <summary>
        /// Adds the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">value</exception>
        public static void Add(this NameValueCollection col, string name, IEnumerable<string> value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            col.Add(name, string.Join(",", value.ToArray()));
        }

        /// <summary>
        /// Adds if not null.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public static void AddIfNotNull(this NameValueCollection col, string name, IEnumerable<string> value)
        {
            if (value != null)
            {
                col.Add(name, value);
            }
        }

        /// <summary>
        /// Adds the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <param name="delimiter">The delimiter.</param>
        /// <exception cref="ArgumentNullException">value</exception>
        public static void Add(this NameValueCollection col, string name, IEnumerable<string> value, string delimiter)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            col.Add(name, string.Join(delimiter, value.ToArray()));
        }

        /// <summary>
        /// Adds if not null.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <param name="delimiter">The delimiter.</param>
        public static void AddIfNotNull(this NameValueCollection col, string name, IEnumerable<string> value, string delimiter)
        {
            if (value != null)
            {
                col.Add(name, value, delimiter);
            }
        }

        public static string ToQueryString(this NameValueCollection col)
        {
            var queryArray = (
                from key in col.AllKeys
                from value in col.GetValues(key)
                select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value))
            ).ToArray();
            return string.Join("&", queryArray);
        }
    }
}
