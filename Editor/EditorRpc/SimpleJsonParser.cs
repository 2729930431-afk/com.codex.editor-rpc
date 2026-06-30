using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace EditorRpc
{
    internal static class SimpleJsonParser
    {
        public static Dictionary<string, string> Parse(string json)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(json))
            {
                return values;
            }

            var index = 0;
            SkipWhitespace(json, ref index);
            if (index >= json.Length || json[index] != '{')
            {
                return values;
            }

            index++;
            while (index < json.Length)
            {
                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == '}')
                {
                    index++;
                    break;
                }

                if (index >= json.Length || json[index] != '"')
                {
                    break;
                }

                var key = ReadString(json, ref index);
                SkipWhitespace(json, ref index);
                if (index >= json.Length || json[index] != ':')
                {
                    break;
                }

                index++;
                values[key] = ReadValue(json, ref index);
                SkipWhitespace(json, ref index);

                if (index < json.Length && json[index] == ',')
                {
                    index++;
                    continue;
                }

                if (index < json.Length && json[index] == '}')
                {
                    index++;
                }

                break;
            }

            return values;
        }

        private static string ReadValue(string json, ref int index)
        {
            SkipWhitespace(json, ref index);
            if (index >= json.Length)
            {
                return string.Empty;
            }

            var current = json[index];
            if (current == '"')
            {
                return ReadString(json, ref index);
            }

            if (current == '{' || current == '[')
            {
                return ReadBalanced(json, ref index);
            }

            var start = index;
            while (index < json.Length && json[index] != ',' && json[index] != '}' && json[index] != ']')
            {
                index++;
            }

            return json.Substring(start, index - start).Trim();
        }

        private static string ReadBalanced(string json, ref int index)
        {
            var start = index;
            var stack = new Stack<char>();
            var inString = false;
            var escaping = false;

            while (index < json.Length)
            {
                var current = json[index];
                if (inString)
                {
                    if (escaping)
                    {
                        escaping = false;
                    }
                    else if (current == '\\')
                    {
                        escaping = true;
                    }
                    else if (current == '"')
                    {
                        inString = false;
                    }

                    index++;
                    continue;
                }

                if (current == '"')
                {
                    inString = true;
                    index++;
                    continue;
                }

                if (current == '{')
                {
                    stack.Push('}');
                }
                else if (current == '[')
                {
                    stack.Push(']');
                }
                else if ((current == '}' || current == ']') && stack.Count > 0)
                {
                    var expected = stack.Pop();
                    if (current != expected)
                    {
                        index++;
                        break;
                    }

                    if (stack.Count == 0)
                    {
                        index++;
                        break;
                    }
                }

                index++;
            }

            return json.Substring(start, index - start);
        }

        private static string ReadString(string json, ref int index)
        {
            var builder = new StringBuilder();
            if (index < json.Length && json[index] == '"')
            {
                index++;
            }

            while (index < json.Length)
            {
                var current = json[index++];
                if (current == '"')
                {
                    break;
                }

                if (current != '\\' || index >= json.Length)
                {
                    builder.Append(current);
                    continue;
                }

                var escaped = json[index++];
                switch (escaped)
                {
                    case '"':
                    case '\\':
                    case '/':
                        builder.Append(escaped);
                        break;
                    case 'b':
                        builder.Append('\b');
                        break;
                    case 'f':
                        builder.Append('\f');
                        break;
                    case 'n':
                        builder.Append('\n');
                        break;
                    case 'r':
                        builder.Append('\r');
                        break;
                    case 't':
                        builder.Append('\t');
                        break;
                    case 'u':
                        builder.Append(ReadUnicodeEscape(json, ref index));
                        break;
                    default:
                        builder.Append(escaped);
                        break;
                }
            }

            return builder.ToString();
        }

        private static char ReadUnicodeEscape(string json, ref int index)
        {
            if (index + 4 > json.Length)
            {
                index = json.Length;
                return '?';
            }

            var hex = json.Substring(index, 4);
            index += 4;
            int value;
            return int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value)
                ? (char)value
                : '?';
        }

        private static void SkipWhitespace(string json, ref int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index]))
            {
                index++;
            }
        }
    }
}
