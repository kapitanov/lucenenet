using Microsoft.Extensions.Configuration;

namespace Lucene.Net.Support.Configuration
{
    /// <summary>
    /// Configuration source for parsing setting files. <see cref="SettingsFileConfigurationProvider"/>
    /// </summary>
    public class SettingsFileConfigurationSource : FileConfigurationSource
    {
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            FileProvider = FileProvider ?? builder.GetFileProvider();

            return new SettingsFileConfigurationProvider(this);
        }
    }
}
