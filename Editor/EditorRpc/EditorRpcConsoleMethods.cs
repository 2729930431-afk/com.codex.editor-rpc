using System;
using System.Collections.Generic;
using System.Reflection;

namespace EditorRpc
{
    public static partial class EditorRpcMethodExecutor
    {
        private static EditorRpcMethodResult ExecuteReadConsole(string methodName, string argumentsJson)
        {
            var args = ParseArgs(argumentsJson);
            var count = UnityEngine.Mathf.Max(1, GetInt(args, "count", 20));
            var entries = GetConsoleEntries(count);
            return Success("Console entries retrieved.", new ConsolePayload
            {
                returnedCount = entries.Length,
                entries = entries
            });
        }

        private static EditorRpcMethodResult ExecuteClearConsole(string methodName, string argumentsJson)
        {
            var logEntriesType = Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
            if (logEntriesType == null)
            {
                return Failure("Could not resolve UnityEditor.LogEntries.");
            }

            var clearMethod = logEntriesType.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (clearMethod == null)
            {
                return Failure("Could not resolve LogEntries.Clear.");
            }

            clearMethod.Invoke(null, null);
            return Success("Console cleared.");
        }

        private static ConsoleEntryInfo[] GetConsoleEntries(int count)
        {
            var logEntriesType = Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
            var logEntryType = Type.GetType("UnityEditor.LogEntry, UnityEditor.dll");
            if (logEntriesType == null || logEntryType == null)
            {
                return new ConsoleEntryInfo[0];
            }

            var getCountMethod = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var startMethod = logEntriesType.GetMethod("StartGettingEntries", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var endMethod = logEntriesType.GetMethod("EndGettingEntries", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var getEntryMethod = logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (getCountMethod == null || startMethod == null || endMethod == null || getEntryMethod == null)
            {
                return new ConsoleEntryInfo[0];
            }

            var conditionField = logEntryType.GetField("condition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var modeField = logEntryType.GetField("mode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var fileField = logEntryType.GetField("file", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var lineField = logEntryType.GetField("line", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var totalCount = Convert.ToInt32(getCountMethod.Invoke(null, null));
            if (totalCount <= 0)
            {
                return new ConsoleEntryInfo[0];
            }

            var firstIndex = UnityEngine.Mathf.Max(0, totalCount - count);
            var entries = new List<ConsoleEntryInfo>();
            startMethod.Invoke(null, null);
            try
            {
                for (int index = firstIndex; index < totalCount; index++)
                {
                    var entry = Activator.CreateInstance(logEntryType);
                    var ok = Convert.ToBoolean(getEntryMethod.Invoke(null, new object[] { index, entry }));
                    if (!ok)
                    {
                        continue;
                    }

                    entries.Add(new ConsoleEntryInfo
                    {
                        index = index,
                        message = conditionField != null ? Convert.ToString(conditionField.GetValue(entry)) : string.Empty,
                        mode = modeField != null ? Convert.ToInt32(modeField.GetValue(entry)) : 0,
                        file = fileField != null ? Convert.ToString(fileField.GetValue(entry)) : string.Empty,
                        line = lineField != null ? Convert.ToInt32(lineField.GetValue(entry)) : 0
                    });
                }
            }
            finally
            {
                endMethod.Invoke(null, null);
            }

            return entries.ToArray();
        }
    }
}
