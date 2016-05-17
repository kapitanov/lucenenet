// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. 
//See https://github.com/aspnet/Entropy/blob/dev/LICENSE.txt in the project root for license information.

//Code modified to work with latest version of framework.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Immutable;
using System.IO;

namespace Lucene.Net.Support.Configuration
{
    public static class ConfigFileConfigurationExtensions
    {
        /// <summary>
        /// Adds configuration values for a *.config file to the ConfigurationBuilder
        /// </summary>
        /// <param name="builder">Builder to add configuration values to</param>
        /// <param name="path">Path to *.config file</param>
        /// <param name="optional">true if file is optional; false otherwise</param>
        /// <param name="parsers">Additional parsers to use to parse the config file</param>
        public static IConfigurationBuilder AddConfigFile(this IConfigurationBuilder builder, string path, bool optional, params IConfigurationParser[] parsers)
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

            return AddConfigFile(builder, parsers.ToImmutableArray(), source => {
                source.Path = path;
                source.Optional = optional;
            });
        }

        private static IConfigurationBuilder AddConfigFile(this IConfigurationBuilder builder, ImmutableArray<IConfigurationParser> parsers, Action<FileConfigurationSource> configureSource)
        {
            var source = new ConfigFileConfigurationSource(parsers.ToImmutableArray());

            configureSource(source);

            return builder.Add(source);
        }
    }
}
