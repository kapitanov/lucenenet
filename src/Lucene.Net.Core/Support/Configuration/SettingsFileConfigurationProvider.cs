using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Lucene.Net.Support.Configuration
{
    /// <summary>
    /// This configuration provider parses *.settings files.
    /// </summary>
    /// <example>
    /// The following settings file:
    /// &lt;SettingsFile xmlns=&quot;http://schemas.microsoft.com/VisualStudio/2004/01/settings&quot; CurrentProfile=&quot;(Default)&quot; GeneratedClassNamespace=&quot;TestGeneratedNamespace&quot; GeneratedClassName=&quot;Settings&quot;&gt;
    ///  &lt;Profiles /&gt;
    ///  &lt;Settings&gt;
    ///    &lt;Setting Name=&quot; Bob&quot; Type=&quot;System.String&quot; Scope=&quot;User&quot;&gt;
    ///      &lt;Value Profile=&quot;(Default)&quot;&gt;John&lt;/Value&gt;
    ///      &lt;Value Profile=&quot;AnotherProfile&quot;&gt;Johanna&lt;/Value&gt;
    ///    &lt;/Setting&gt;
    ///    &lt;Setting Name=&quot; Foo&quot; Type=&quot;System.String&quot; Scope=&quot;Application&quot;&gt;
    ///      &lt;Value Profile=&quot;(Default)&quot;&gt;Joe&lt;/Value&gt;
    ///    &lt;/Setting&gt;
    /// &lt;/SettingsFile&gt;
    /// 
    /// Will be parsed in to the following IConfiguration
    /// { Key, Value}
    /// { Bob:(Default), John }
    /// { Bob:AnotherProfile, Johanna }
    /// { Foo:(Default), Joe }
    /// </example>
    public class SettingsFileConfigurationProvider : FileConfigurationProvider
    {
        public SettingsFileConfigurationProvider(FileConfigurationSource source) : base(source) { }

        public override void Load(Stream stream)
        {
            var document = XDocument.Load(stream);
            var context = new Stack<string>();
            var dictionary = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var ns = document.Root.Name.NamespaceName;

            foreach (var setting in document.Root.Descendants(XName.Get("Setting", ns)))
            {
                string key;
                var allValues = setting.Elements(XName.Get("Value", ns)).ToArray();

                if (!TryGetAttributeValue(setting, "Name", out key)
                    || allValues == null)
                {
                    continue;
                }

                context.Push(key);

                foreach (var value in allValues)
                {
                    string profile;

                    if (!TryGetAttributeValue(value, "Profile", out profile))
                    {
                        continue;
                    }

                    dictionary.Add(GetKey(context, profile), value.Value);
                }
                context.Pop();
            }

            Data = dictionary;
        }

        /// <summary>
        /// Tries to get the value from an XAttribute if it exists in the given XElement.
        /// </summary>
        private bool TryGetAttributeValue(XElement element, XName attributeName, out string value)
        {
            value = null;
            var attribute = element.Attribute(attributeName);

            if (attribute == null)
            {
                return false;
            }
            else
            {
                value = attribute.Value;
                return true;
            }
        }

        private static string GetKey(Stack<string> context, string name)
        {
            return string.Join(ConfigurationPath.KeyDelimiter, context.Reverse().Concat(new[] { name }));
        }
    }

    /// <summary>
    /// Parses a *.settings file. For more information <see cref="SettingsFileConfigurationProvider"/>
    /// </summary>
    public static class SettingsFileConfigurationProviderExtensions
    {
        /// <summary>
        /// Adds configuration values for a *.settings file to the ConfigurationBuilder
        /// </summary>
        /// <param name="builder">Builder to add configuration values to</param>
        /// <param name="path">Path to *.config file</param>
        /// <param name="optional">true if file is optional; false otherwise</param>
        public static IConfigurationBuilder AddSettingsFile(this IConfigurationBuilder builder, string path, bool optional)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            else if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Path for configuration cannot be null/empty.", nameof(path));
            }

            if (!optional && !File.Exists(path))
            {
                throw new FileNotFoundException($"Could not find configuration file. File: [{path}]", path);
            }

            var source = new SettingsFileConfigurationSource()
            {
                Path = path,
                Optional = optional,
            };

            return builder.Add(source);
        }
    }
}
