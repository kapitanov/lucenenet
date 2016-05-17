using Microsoft.Extensions.Configuration;
using System.Collections.Immutable;

namespace Lucene.Net.Support.Configuration
{
    /// <summary>
    /// A *.config file based <see cref="FileConfigurationSource"/>
    /// </summary>
    public class ConfigFileConfigurationSource : FileConfigurationSource
    {
        public ImmutableArray<IConfigurationParser> Parsers { get; }

        public ConfigFileConfigurationSource(ImmutableArray<IConfigurationParser> parsers)
        {
            Parsers = parsers;
        }

        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            FileProvider = FileProvider ?? builder.GetFileProvider();
            return new ConfigFileConfigurationProvider(this);
        }
    }
}
