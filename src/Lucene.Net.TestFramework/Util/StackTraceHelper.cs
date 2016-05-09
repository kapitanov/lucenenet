using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Lucene.Net.Util
{
    public static class StackTraceHelper
    {
        private static Regex s_fullyQualifiedMethodRegex = new Regex(@"\s*at (?<fullyQualifiedMethod>.*)\(");
        private static Regex s_namePartsRegex = new Regex(@"((?<part>[\w`]+)\.|(?<part>[\w`]+))");

        /// <summary>
        /// Matches the StackTrace for a method name.
        /// </summary>
        public static bool DoesStackTraceContainMethod(string methodName)
        {
#if FEATURE_STACKTRACE
            IEnumerable<string> allMethods = GetStackTrace(false);
            return allMethods.Contains(methodName);
#else
            StackTrace trace = new StackTrace();
            foreach (var frame in trace.GetFrames())
            {
                if (frame.GetMethod().Name.equals(methodName))
                {
                    return true;                  
                }
            }
            return false;
#endif

        }

        /// <summary>
        /// Matches the StackTrace for a particular class (not fully-qualified) and method name.
        /// </summary>
        public static bool DoesStackTraceContainMethod(string className, string methodName)
        {
#if FEATURE_STACKTRACE
            IEnumerable<string> allMethods = GetStackTrace(true);
            return allMethods.Contains(className + '.' + methodName);
#else
            StackTrace trace = new StackTrace();
            foreach (var frame in trace.GetFrames())
            {
                var method = frame.GetMethod();
                if (method.DeclaringType.Name.equals(className) && method.Name.equals(methodName))
                {
                    return true;
                }
            }
            return false;
#endif
        }

        private static IEnumerable<string> GetStackTrace(bool includeFullyQualifiedName)
        {
            var matches =
                Environment.StackTrace
                .Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line =>
                {
                    var match = s_fullyQualifiedMethodRegex.Match(line);

                    if (!match.Success)
                    {
                        return null;
                    }

                    var fullyQualifiedMethod = match.Groups["fullyQualifiedMethod"].Value;

                    if (includeFullyQualifiedName)
                    {
                        return fullyQualifiedMethod;
                    }

                    var allParts = s_namePartsRegex.Matches(fullyQualifiedMethod)
                            .Cast<Match>()
                            .Where(x => x.Success)
                            .Select(x => x.Groups["part"].Value);

                    // Last part will be the method name because it is the 
                    // last . in the fully qualified name.
                    return allParts.Count() > 0 ? allParts.Last() : null;
                })
                .Where(line => !string.IsNullOrEmpty(line));

            return matches;
        }
    }
}
