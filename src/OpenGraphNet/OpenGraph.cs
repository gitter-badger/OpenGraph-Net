﻿namespace OpenGraphNet
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using HtmlAgilityPack;

    using OpenGraphNet.Metadata;
    using OpenGraphNet.Namespaces;

    /// <summary>
    /// Represents Open Graph meta data parsed from HTML
    /// </summary>
    public class OpenGraph : IDictionary<string, StructuredMetadata>
    {
        /// <summary>
        /// The open graph data
        /// </summary>
        private readonly StructuredMetadataCollection internalOpenGraphData;

        /// <summary>
        /// Prevents a default instance of the <see cref="OpenGraph" /> class from being created.
        /// </summary>
        private OpenGraph()
        {
            this.internalOpenGraphData = new StructuredMetadataCollection();
            this.Namespaces = new Dictionary<string, Namespace>();
        }

        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <value>
        /// The data.
        /// </value>
        public IDictionary<string, IList<StructuredMetadata>> Metadata => new ReadOnlyDictionary<string, IList<StructuredMetadata>>(this.internalOpenGraphData);

        /// <summary>
        /// Gets or sets the namespaces.
        /// </summary>
        /// <value>
        /// The namespaces.
        /// </value>
        public IDictionary<string, Namespace> Namespaces { get; set; }

        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <value>The type of open graph document.</value>
        public string Type { get; private set; }

        /// <summary>
        /// Gets the title of the open graph document.
        /// </summary>
        /// <value>The title.</value>
        public string Title { get; private set; }

        /// <summary>
        /// Gets the image for the open graph document.
        /// </summary>
        /// <value>The image.</value>
        public Uri Image { get; private set; }

        /// <summary>
        /// Gets the URL for the open graph document
        /// </summary>
        /// <value>The URL.</value>
        public Uri Url { get; private set; }

        /// <summary>
        /// Gets the original URL used to generate this graph
        /// </summary>
        /// <value>The original URL.</value>
        public Uri OriginalUrl { get; private set; }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1" /> containing the keys of the <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.Generic.ICollection`1" /> containing the keys of the object that implements <see cref="T:System.Collections.Generic.IDictionary`2" />.</returns>
        [Obsolete("Use this.Data instead. This feature will be removed")]
        public ICollection<string> Keys => this.internalOpenGraphData.Keys.ToList();

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1" /> containing the values in the <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.Generic.ICollection`1" /> containing the values in the object that implements <see cref="T:System.Collections.Generic.IDictionary`2" />.</returns>
        [Obsolete("Use this.Data instead. This feature will be removed")]
        public ICollection<StructuredMetadata> Values => this.internalOpenGraphData.Select(kvp => kvp.Value.FirstOrDefault()).ToList();

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <returns>The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.</returns>
        [Obsolete("Use this.Data instead. This feature will be removed")]
        public int Count => this.internalOpenGraphData.Count;

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
        /// </summary>
        /// <returns>true</returns>
        [Obsolete("Use this.Data instead. This feature will be removed")]
        public bool IsReadOnly => true;

        /// <summary>
        /// Gets the head prefix attribute value.
        /// </summary>
        /// <value>
        /// The head prefix attribute value.
        /// </value>
        public string HeadPrefixAttributeValue 
        {
            get
            {
                var sb = new StringBuilder();
                foreach (var ns in this.Namespaces)
                {
                    sb.AppendFormat(" {0}", ns.Value.ToString());
                }

                return sb.ToString().Trim();
            }
        }

        /// <summary>
        /// Gets the HTML XML namespace values.
        /// </summary>
        /// <value>
        /// The HTML XML namespace values.
        /// </value>
        public string HtmlXmlnsValues
        {
            get
            {
                var sb = new StringBuilder();
                foreach (var ns in this.Namespaces)
                {
                    sb.AppendFormat(" xmlns:{0}=\"{1}\"", ns.Value.Prefix, ns.Value.SchemaUri.ToString());
                }

                return sb.ToString().Trim();
            }
        }

        /// <summary>
        /// Gets or sets the element with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>returns the open graph value at the specified key</returns>
        /// <exception cref="OpenGraphNet.ReadOnlyDictionaryException">Cannot modify a read-only collection</exception>
        [Obsolete("Use this.Data instead. This feature will be removed")]
        public StructuredMetadata this[string key]
        {
            get
            {
                if (key.IndexOf(':') < 0)
                {
                    key = "og:" + key;
                }

                return !this.internalOpenGraphData.ContainsKey(key) ? new NullMetadata() : this.internalOpenGraphData[key].First();
            }

            set => throw new ReadOnlyDictionaryException();
        }

        /// <summary>
        /// Makes the graph.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="type">The type.</param>
        /// <param name="image">The image.</param>
        /// <param name="url">The URL.</param>
        /// <param name="description">The description.</param>
        /// <param name="siteName">Name of the site.</param>
        /// <param name="audio">The audio.</param>
        /// <param name="video">The video.</param>
        /// <param name="locale">The locale.</param>
        /// <param name="localeAlternates">The locale alternates.</param>
        /// <param name="determiner">The determiner.</param>
        /// <returns><see cref="OpenGraph"/></returns>
        public static OpenGraph MakeGraph(
            string title,
            string type,
            string image,
            string url,
            string description = "",
            string siteName = "",
            string audio = "",
            string video = "",
            string locale = "",
            IList<string> localeAlternates = null,
            string determiner = "")
        {
            var graph = new OpenGraph
            {
                Title = title,
                Type = type,
                Image = new Uri(image, UriKind.Absolute),
                Url = new Uri(url, UriKind.Absolute),
            };
            var ns = NamespaceRegistry.Instance.Namespaces["og"];

            graph.Namespaces.Add(ns.Prefix, ns);
            graph.AddMetadata(new StructuredMetadata(ns, "title", title));
            graph.AddMetadata(new StructuredMetadata(ns, "type", type));
            graph.AddMetadata(new StructuredMetadata(ns, "image", image));
            graph.AddMetadata(new StructuredMetadata(ns, "url", url));

            if (!string.IsNullOrWhiteSpace(description))
            {
                graph.AddMetadata(new StructuredMetadata(ns, "description", description));
            }

            if (!string.IsNullOrWhiteSpace(siteName))
            {
                graph.AddMetadata(new StructuredMetadata(ns, "site_name", siteName));
            }

            if (!string.IsNullOrWhiteSpace(audio))
            {
                graph.AddMetadata(new StructuredMetadata(ns, "audio", audio));
            }

            if (!string.IsNullOrWhiteSpace(video))
            {
                graph.AddMetadata(new StructuredMetadata(ns, "video", video));
            }

            if (!string.IsNullOrWhiteSpace(locale))
            {
                graph.AddMetadata(new StructuredMetadata(ns, "locale", locale));
            }

            if (!string.IsNullOrWhiteSpace(determiner))
            {
                graph.AddMetadata(new StructuredMetadata(ns, "determiner", determiner));
            }

            if (graph.internalOpenGraphData.ContainsKey("og:locale"))
            {
                var localeElement = graph.internalOpenGraphData["og:locale"].First();
                foreach (var localeAlternate in localeAlternates ?? new List<string>())
                {
                    localeElement.AddProperty(new PropertyMetadata("alternate", localeAlternate));
                }
            }
            else
            {
                foreach (var localeAlternate in localeAlternates ?? new List<string>())
                {
                    graph.AddMetadata(new StructuredMetadata(ns, "locale:alternate", localeAlternate));
                }
            }

            return graph;
        }

        /// <summary>
        /// Downloads the HTML of the specified URL and parses it for open graph content.
        /// </summary>
        /// <param name="url">The URL to download the HTML from.</param>
        /// <param name="userAgent">The user agent to use when downloading content.  The default is <c>"facebookexternalhit"</c> which is required for some site (like amazon) to include open graph data.</param>
        /// <param name="validateSpecification">if set to <c>true</c> <see cref="OpenGraph"/> will validate against the specification.</param>
        /// <returns>
        ///   <see cref="OpenGraph" />
        /// </returns>
        public static OpenGraph ParseUrl(string url, string userAgent = "facebookexternalhit", bool validateSpecification = false)
        {
            Uri uri = new Uri(url);
            return ParseUrl(uri, userAgent, validateSpecification);
        }

        /// <summary>
        /// Parses the URL asynchronous.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="userAgent">The user agent.</param>
        /// <param name="validateSpecification">if set to <c>true</c> validate minimum Open Graph specification.</param>
        /// <returns><see cref="Task{OpenGraph}"/></returns>
        public static Task<OpenGraph> ParseUrlAsync(string url, string userAgent = "facebookexternalhit", bool validateSpecification = false)
        {
            Uri uri = new Uri(url);
            return ParseUrlAsync(uri, userAgent, validateSpecification);
        }

        /// <summary>
        /// Parses the URL asynchronous.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="userAgent">The user agent.</param>
        /// <param name="validateSpecification">if set to <c>true</c> [validate specification].</param>
        /// <returns><see cref="Task{OpenGraph}"/></returns>
        public static async Task<OpenGraph> ParseUrlAsync(Uri url, string userAgent = "facebookexternalhit", bool validateSpecification = false)
        {
            OpenGraph result = new OpenGraph { OriginalUrl = url };

            HttpDownloader downloader = new HttpDownloader(url, null, userAgent);
            string html = await downloader.GetPageAsync();

            return ParseHtml(result, html, validateSpecification);
        }

        /// <summary>
        /// Downloads the HTML of the specified URL and parses it for open graph content.
        /// </summary>
        /// <param name="url">The URL to download the HTML from.</param>
        /// <param name="userAgent">The user agent to use when downloading content.  The default is <c>"facebookexternalhit"</c> which is required for some site (like amazon) to include open graph data.</param>
        /// <param name="validateSpecification">if set to <c>true</c> verify that the document meets the required attributes of the open graph specification.</param>
        /// <returns><see cref="OpenGraph"/></returns>
        public static OpenGraph ParseUrl(Uri url, string userAgent = "facebookexternalhit", bool validateSpecification = false)
        {
            OpenGraph result = new OpenGraph { OriginalUrl = url };

            HttpDownloader downloader = new HttpDownloader(url, null, userAgent);
            string html = downloader.GetPage();

            return ParseHtml(result, html, validateSpecification);
        }

        /// <summary>
        /// Parses the HTML for open graph content.
        /// </summary>
        /// <param name="content">The HTML to parse.</param>
        /// <param name="validateSpecification">if set to <c>true</c> verify that the document meets the required attributes of the open graph specification.</param>
        /// <returns><see cref="OpenGraph"/></returns>
        public static OpenGraph ParseHtml(string content, bool validateSpecification = false)
        {
            OpenGraph result = new OpenGraph();
            return ParseHtml(result, content, validateSpecification);
        }

        /// <summary>
        /// Adds the meta element.
        /// </summary>
        /// <param name="element">The element.</param>
        public void AddMetadata(StructuredMetadata element)
        {
            var key = string.Concat(element.Namespace.Prefix, ":", element.Name);
            if (this.internalOpenGraphData.ContainsKey(key))
            {
                this.internalOpenGraphData[key].Add(element);
            }
            else
            {
                this.internalOpenGraphData.Add(key, new List<StructuredMetadata> { element });
            }
        }

        /// <summary>
        /// Adds the metadata.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="InvalidOperationException">The prefix {prefix} does not exist in the NamespaceRegistry</exception>
        public void AddMetadata(string prefix, string name, string value)
        {
            if (!NamespaceRegistry.Instance.Namespaces.ContainsKey(prefix))
            {
                throw new InvalidOperationException($"The prefix {prefix} does not exist in the {nameof(NamespaceRegistry)}");
            }

            var ns = NamespaceRegistry.Instance.Namespaces[prefix];

            var metadata = new StructuredMetadata(ns, name, value);
            this.AddMetadata(metadata);
        }

    /// <summary>
    /// Returns a <see cref="System.String" /> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="System.String" /> that represents this instance.
    /// </returns>
    public override string ToString()
        {
            var doc = new HtmlDocument();

            var elements = this.internalOpenGraphData.SelectMany(og => og.Value).ToList();

            foreach (var structuredMetaElement in elements)
            {
                doc.DocumentNode.AppendChild(structuredMetaElement.CreateDocument().DocumentNode);
            }

            return doc.DocumentNode.InnerHtml;
        }

        /// <summary>
        /// Adds the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="OpenGraphNet.ReadOnlyDictionaryException">Cannot change a read only dictionary</exception>
        [Obsolete("Use this.Data instead. This feature will be removed")]
        public void Add(string key, StructuredMetadata value)
        {
            throw new ReadOnlyDictionaryException();
        }

        /// <summary>
        /// Determines whether the specified key contains key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>
        ///   <c>true</c> if the specified key contains key; otherwise, <c>false</c>.
        /// </returns>
        [Obsolete("Use this.Data instead. This feature will be removed")]
        public bool ContainsKey(string key)
        {
            return this.internalOpenGraphData.ContainsKey(key);
        }

        /// <summary>
        /// Removes the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>false</c></returns>
        /// <exception cref="OpenGraphNet.ReadOnlyDictionaryException">Cannot change a read only dictionary</exception>
        [Obsolete("Use this.Data instead. This feature will be removed")]
        public bool Remove(string key)
        {
            throw new ReadOnlyDictionaryException();
        }

        /// <summary>
        /// Tries the get value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>true if the value was successfully set; otherwise false</returns>
        [Obsolete("Use this.Data instead. This feature will be removed")]
        public bool TryGetValue(string key, out StructuredMetadata value)
        {
            var result = this.internalOpenGraphData.TryGetValue(key, out var item);
            value = (item ?? new List<StructuredMetadata>()).FirstOrDefault();
            return result;
        }

        /// <summary>
        /// Adds the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <exception cref="OpenGraphNet.ReadOnlyDictionaryException">Cannot change a read only dictionary</exception>
        [Obsolete("Use this.Data instead. This feature will be removed")]
        public void Add(KeyValuePair<string, StructuredMetadata> item)
        {
            throw new ReadOnlyDictionaryException();
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        /// <exception cref="OpenGraphNet.ReadOnlyDictionaryException">Cannot change a read only dictionary</exception>
        [Obsolete("Use this.Data instead. This feature will be removed")]
        public void Clear()
        {
            throw new ReadOnlyDictionaryException();
        }

        /// <summary>
        /// Determines whether [contains] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>
        ///   <c>true</c> if [contains] [the specified item]; otherwise, <c>false</c>.
        /// </returns>
        [Obsolete("Use this.Data instead. This feature will be removed")]
        public bool Contains(KeyValuePair<string, StructuredMetadata> item)
        {
            return this.internalOpenGraphData.Contains(new KeyValuePair<string, IList<StructuredMetadata>>(item.Key, new List<StructuredMetadata> { item.Value }));
        }

        /// <summary>
        /// Copies to.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="arrayIndex">Index of the array.</param>
        [Obsolete("Use this.Data instead. This feature will be removed")]
        public void CopyTo(KeyValuePair<string, StructuredMetadata>[] array, int arrayIndex)
        {
            var items = array.Select(a => new KeyValuePair<string, IList<StructuredMetadata>>(a.Key, new List<StructuredMetadata> { a.Value })).ToArray();
            this.internalOpenGraphData.CopyTo(items, arrayIndex);
        }

        /// <summary>
        /// Removes the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>Returns false</returns>
        /// <exception cref="OpenGraphNet.ReadOnlyDictionaryException">Cannot change a read only dictionary</exception>
        [Obsolete("Use this.Data instead. This feature will be removed")]
        public bool Remove(KeyValuePair<string, StructuredMetadata> item)
        {
            throw new ReadOnlyDictionaryException();
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns>The enumerator for the key value pairs</returns>
        [Obsolete("Use this.Data instead. This feature will be removed")]
        public IEnumerator<KeyValuePair<string, StructuredMetadata>> GetEnumerator()
        {
            return this.internalOpenGraphData.Select(kvp => new KeyValuePair<string, StructuredMetadata>(kvp.Key, kvp.Value.First())).GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        [Obsolete("Use this.Data instead. This feature will be removed")]
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((System.Collections.IEnumerable)this.internalOpenGraphData).GetEnumerator();
        }

        /// <summary>
        /// Safes the HTML decode URL.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The string</returns>
        private static string HtmlDecodeUrl(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            // naive attempt
            var patterns = new Dictionary<string, string>
            {
                ["&amp;"] = "&",
            };

            foreach (var key in patterns)
            {
                value = value.Replace(key.Key, key.Value);
            }

            return value;
        }

        /// <summary>
        /// Gets the open graph key.
        /// </summary>
        /// <param name="metaTag">The meta tag.</param>
        /// <returns>Returns the key stored from the meta tag</returns>
        private static string GetOpenGraphKey(HtmlNode metaTag)
        {
            if (metaTag.Attributes.Contains("property"))
            {
                return metaTag.Attributes["property"].Value;
            }

            return metaTag.Attributes["name"].Value;
        }

        private static string GetOpenGraphPrefix(HtmlNode metaTag)
        {
            var value = metaTag.Attributes.Contains("property") ? metaTag.Attributes["property"].Value : metaTag.Attributes["name"].Value;

            return value.Split(':')[0];
        }

        /// <summary>
        /// Cleans the open graph key.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// strips the namespace prefix from the value
        /// </returns>
        private static string CleanOpenGraphKey(string prefix, string value)
        {
            return value.Replace(string.Concat(prefix, ":"), string.Empty).ToLower(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets the open graph value.
        /// </summary>
        /// <param name="metaTag">The meta tag.</param>
        /// <returns>Returns the value from the meta tag</returns>
        private static string GetOpenGraphValue(HtmlNode metaTag)
        {
            if (!metaTag.Attributes.Contains("content"))
            {
                return string.Empty;
            }

            return metaTag.Attributes["content"].Value;
        }

        /// <summary>
        /// Initializes the <see cref="OpenGraph" /> class.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="document">The document.</param>
        private static void ParseNamespaces(OpenGraph result, HtmlDocument document)
        {
            const string NamespacePattern = @"(\w+):\s?(https?://[^\s]+)";

            HtmlNode head = document.DocumentNode.SelectSingleNode("//head");
            HtmlNode html = document.DocumentNode.SelectSingleNode("html");

            if (head != null && head.Attributes.Contains("prefix") && Regex.IsMatch(head.Attributes["prefix"].Value, NamespacePattern))
            {
                var matches = Regex.Matches(
                    head.Attributes["prefix"].Value,
                    NamespacePattern,
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);

                foreach (Match match in matches)
                {
                    var prefix = match.Groups[1].Value;
                    if (NamespaceRegistry.Instance.Namespaces.ContainsKey(prefix))
                    {
                        result.Namespaces.Add(prefix, NamespaceRegistry.Instance.Namespaces[prefix]);
                        continue;
                    }

                    var ns = match.Groups[2].Value;
                    result.Namespaces.Add(prefix, new Namespace(prefix, ns));
                }
            }
            else if (html != null && html.Attributes.Any(a => a.Name.ToLowerInvariant().StartsWith("xmlns:")))
            {
                var namespaces = html.Attributes.Where(a => a.Name.ToLowerInvariant().StartsWith("xmlns:"));
                foreach (var ns in namespaces)
                {
                    var prefix = ns.Name.ToLowerInvariant().Replace("xmlns:", string.Empty);
                    result.Namespaces.Add(prefix, new Namespace(prefix, ns.Value));
                }
            }
            else
            {  
                // append the minimum og: prefix and namespace
                result.Namespaces.Add("og", NamespaceRegistry.Instance.Namespaces["og"]);
            }
        }

        private static bool MatchesNamespacePredicate(string value)
        {
            return value.IndexOf(':') >= 0;
        }

        private static void SetNamespace(OpenGraph graph, string prefix)
        {
            if (graph.Namespaces.Any(n => n.Key == prefix.ToLowerInvariant()))
            {
                return;
            }

            if (NamespaceRegistry.Instance.Namespaces.Any(_ => _.Key == prefix.ToLowerInvariant()))
            {
                var ns = NamespaceRegistry.Instance.Namespaces.First(_ => _.Key == prefix.ToLowerInvariant());
                graph.Namespaces.Add(ns.Key, ns.Value);
            }
        }

        private static HtmlDocument MakeDocumentToParse(string content)
        {
            int indexOfClosingHead = Regex.Match(content, "</head>").Index;
            string toParse = content.Substring(0, indexOfClosingHead + 7);

            toParse = toParse + "<body></body></html>\r\n";

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(toParse);

            return document;
        }

        /// <summary>
        /// Parses the HTML.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="content">The content.</param>
        /// <param name="validateSpecification">if set to <c>true</c> [validate specification].</param>
        /// <returns><see cref="OpenGraph"/></returns>
        /// <exception cref="OpenGraphNet.InvalidSpecificationException">The parsed HTML does not meet the open graph specification</exception>
        private static OpenGraph ParseHtml(OpenGraph result, string content, bool validateSpecification = false)
        {
            HtmlDocument document = MakeDocumentToParse(content);

            ParseNamespaces(result, document);

            HtmlNodeCollection allMeta = document.DocumentNode.SelectNodes("//meta");

            var openGraphMetaTags = from meta in allMeta ?? new HtmlNodeCollection(null)
                                    where (meta.Attributes.Contains("property") && MatchesNamespacePredicate(meta.Attributes["property"].Value)) ||
                                    (meta.Attributes.Contains("name") && MatchesNamespacePredicate(meta.Attributes["name"].Value))
                                    select meta;

            StructuredMetadata lastElement = null;
            foreach (HtmlNode metaTag in openGraphMetaTags)
            {
                var prefix = GetOpenGraphPrefix(metaTag);
                SetNamespace(result, prefix);
                if (!result.Namespaces.ContainsKey(prefix))
                {
                    continue;
                }

                string value = GetOpenGraphValue(metaTag);
                string property = GetOpenGraphKey(metaTag);
                var cleanProperty = CleanOpenGraphKey(prefix, property);

                value = HtmlDecodeUrl(property, value);

                if (lastElement != null && lastElement.IsMyProperty(property))
                {
                    lastElement.AddProperty(cleanProperty, value);
                }
                else
                {
                    lastElement = new StructuredMetadata(result.Namespaces[prefix], cleanProperty, value);
                    result.AddMetadata(lastElement);
                }
            }

                result.Type = string.Empty;
            if (result.internalOpenGraphData.TryGetValue("og:type", out var type))
            {
                result.Type = (type.FirstOrDefault() ?? new NullMetadata()).Value ?? string.Empty;
            }

            result.Title = string.Empty;
            if (result.internalOpenGraphData.TryGetValue("og:title", out var title))
            {
                result.Title = (title.FirstOrDefault() ?? new NullMetadata()).Value ?? string.Empty;
            }

            result.Image = GetUri(result, "og:image");
            result.Url = GetUri(result, "og:url");

            if (validateSpecification)
            {
                ValidateSpecification(result);
            }

            return result;
        }

        private static Uri GetUri(OpenGraph result, string property)
        {
            result.internalOpenGraphData.TryGetValue(property, out var url);

            try
            {
                return new Uri((url.FirstOrDefault() ?? new NullMetadata()).Value ?? string.Empty);
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (UriFormatException)
            {
                return null;
            }
        }

        /// <summary>
        /// HTMLs the decode urls.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <returns>The decoded url value</returns>
        private static string HtmlDecodeUrl(string property, string value)
        {
            var urlPropertyPatterns = new[] { "image", "url^" };
            foreach (var urlPropertyPattern in urlPropertyPatterns)
            {
                if (Regex.IsMatch(property, urlPropertyPattern))
                {
                    return HtmlDecodeUrl(value);
                }
            }

            return value;
        }

        private static void ValidateSpecification(OpenGraph result)
        {
            var prefixes = result.Namespaces.Select(ns => ns.Value.Prefix);

            var namespaces = NamespaceRegistry
                .Instance
                .Namespaces
                .Where(ns => prefixes.Contains(ns.Key) && ns.Value.RequiredElements.Count > 0)
                .Select(ns => ns.Value)
                .ToList();

            foreach (var ns in namespaces)
            {
                foreach (var required in ns.RequiredElements)
                {
                    if (!result.Metadata.ContainsKey(string.Concat(ns.Prefix, ":", required)))
                    {
                        throw new InvalidSpecificationException($"The parsed HTML does not meet the open graph specification, missing element: {required}");
                    }
                }
            }
        }
    }
}
