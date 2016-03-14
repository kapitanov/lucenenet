using NUnit.Framework;


namespace Lucene.Net.TestFramework.Support
{
    public class RandomizedTest
    {
        public static bool SystemPropertyAsBoolean(string key, bool defaultValue)
        {
            return SystemProperties.GetProperty<bool>(key, defaultValue,
                (str) =>
                {
                    bool v;
                    if (bool.TryParse(str, out v))
                        return v;
                    else
                        return defaultValue;
                }
                );
        }

        public static int SystemPropertyAsInt(string key, int defaultValue)
        {
            return SystemProperties.GetProperty<int>(key, defaultValue,
              (str) =>
              {
                  int v;
                  if (int.TryParse(str, out v))
                      return v;
                  else
                      return defaultValue;
              }
            );
        }

        public static void AssumeTrue(string msg, bool value)
        {
            Assume.That(value, msg);
        }

        public static void AssumeFalse(string msg, bool value)
        {
            Assume.That(!value, msg);
        }
    }
}