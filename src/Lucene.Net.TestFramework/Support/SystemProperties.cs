#if NETCORE
using Lucene.Net.Portable.Support.Configuration;
using Microsoft.Extensions.Configuration;
#else
using System.Configuration;
#endif
using System;

namespace Lucene.Net.TestFramework.Support
{
    public static class SystemProperties
    {
        public static string GetProperty(string key)
        {
#if NETCORE                     
            IConfigurationBuilder builder = new ConfigurationBuilder().AddConfigFile("App.config", true, new KeyValueParser());
            IConfigurationRoot configuration = builder.Build();
            return configuration.GetAppSetting(key);
#else
            return ConfigurationManager.AppSettings[key];
#endif
        }

        public static string GetProperty(string key, string defaultValue)
        {
            string setting = GetProperty(key);

            if (string.IsNullOrEmpty(setting))
                return defaultValue;

            return setting;
        }

        /// <summary>
        /// Gets the value for the AppSetting with specified key.  
        /// If key is not present, default value is returned.
        /// If key is present, value is converted to specified type based on the conversionFunction specified.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="conversionFunction"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T GetProperty<T>(string key,  T defaultValue, System.Func<string, T> conversionFunction)
        {
            string setting = GetProperty(key);

            if (string.IsNullOrEmpty(setting))
                return defaultValue;
            else
                return conversionFunction(setting);           
        }
    }
}