using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Security;

namespace ResxPropertiesBuilder
{
    public static class CodeCompileHelper
    {
        private static readonly char[] CharsToReplace = new char[30]
        {
            ' ',
            ' ',
            '.',
            ',',
            ';',
            '|',
            '~',
            '@',
            '#',
            '%',
            '^',
            '&',
            '*',
            '+',
            '-',
            '/',
            '\\',
            '<',
            '>',
            '?',
            '[',
            ']',
            '(',
            ')',
            '{',
            '}',
            '"',
            '\'',
            ':',
            '!'
        };

        public const char ReplacementChar = '_';
        public const string DocCommentSummaryStart = "<summary>";
        public const string DocCommentSummaryEnd = "</summary>";
        public const int DocCommentLengthThreshold = 512;

        public static string TruncateAndFormatCommentStringForOutput(string commentString)
        {
            if (commentString != null)
            {
                if (commentString.Length > DocCommentLengthThreshold)
                    commentString = RM.GetString("StringPropertyTruncatedComment", (object)commentString.Substring(0, DocCommentLengthThreshold));
                commentString = SecurityElement.Escape(commentString);
            }
            return commentString;
        }

        public static string VerifyResourceName(string key, CodeDomProvider provider)
        {
            return VerifyResourceName(key, provider, false);
        }
        public static string VerifyResourceName(
          string key,
          CodeDomProvider provider,
          bool isNameSpace)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            foreach (char oldChar in CharsToReplace)
            {
                if (!isNameSpace || oldChar != '.' && oldChar != ':')
                    key = key.Replace(oldChar, ReplacementChar);
            }
            if (provider.IsValidIdentifier(key))
                return key;
            key = provider.CreateValidIdentifier(key);
            if (provider.IsValidIdentifier(key))
                return key;
            key = "_" + key;
            return provider.IsValidIdentifier(key) ? key : (string)null;
        }
        public static SortedList VerifyResourceNames(
          Dictionary<string, ResourceData> resourceList,
          CodeDomProvider codeProvider,
          ArrayList errors,
          out Hashtable reverseFixupTable)
        {
            reverseFixupTable = new Hashtable(0, (IEqualityComparer)StringComparer.InvariantCultureIgnoreCase);
            SortedList sortedList = new SortedList((IComparer)StringComparer.InvariantCultureIgnoreCase, resourceList.Count);
            foreach (KeyValuePair<string, ResourceData> resource in resourceList)
            {
                string str1 = resource.Key;
                if (string.Equals(str1, "ResourceManager") || string.Equals(str1, "Culture") || typeof(void) == resource.Value.Type)
                    errors.Add((object)str1);
                else if ((str1.Length <= 0 || str1[0] != '$') && (str1.Length <= 1 || str1[0] != '>' || str1[1] != '>'))
                {
                    if (!codeProvider.IsValidIdentifier(str1))
                    {
                        string str2 = VerifyResourceName(str1, codeProvider, false);
                        if (str2 == null)
                        {
                            errors.Add((object)str1);
                            continue;
                        }
                        string str3 = (string)reverseFixupTable[(object)str2];
                        if (str3 != null)
                        {
                            if (!errors.Contains((object)str3))
                                errors.Add((object)str3);
                            if (sortedList.Contains((object)str2))
                                sortedList.Remove((object)str2);
                            errors.Add((object)str1);
                            continue;
                        }
                        reverseFixupTable[(object)str2] = (object)str1;
                        str1 = str2;
                    }
                    ResourceData resourceData = resource.Value;
                    if (!sortedList.Contains((object)str1))
                    {
                        sortedList.Add((object)str1, (object)resourceData);
                    }
                    else
                    {
                        string str2 = (string)reverseFixupTable[(object)str1];
                        if (str2 != null)
                        {
                            if (!errors.Contains((object)str2))
                                errors.Add((object)str2);
                            reverseFixupTable.Remove((object)str1);
                        }
                        errors.Add((object)resource.Key);
                        sortedList.Remove((object)str1);
                    }
                }
            }
            return sortedList;
        }
    }
}
