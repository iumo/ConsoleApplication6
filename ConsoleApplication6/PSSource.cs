using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication6
{
    class PSSource
    {
        public static string[] CreateFromFile(string strFileName) 
        {
            var fileBytes = System.IO.File.ReadAllBytes(strFileName);

            //---Used characters
            //Dictionary<byte, long> byteDictionary = new Dictionary<byte, long>();
            //foreach (var item in fileBytes) { if (!byteDictionary.ContainsKey(item)) byteDictionary[item] = 1; else byteDictionary[item]++; }
            //var byteString = Encoding.ASCII.GetString(byteDictionary.Keys.OrderBy(key => key).ToArray());

            var psString = Encoding.ASCII.GetString(fileBytes);
            var regex = new System.Text.RegularExpressions.Regex(
                @"^
                (
                [^()%\[\]{}<>/\t\r\n\f \x00]+
                |
                [\t\r\n\f \x00]+
                |
                //?[^()%\[\]{}<>/\t\r\n\f \x00]*
                |
                %[^\n]*\n
                |
                \((?:(?:[^()\\]*)|(?:\\(?:[nrtbf\\()]|[0-7]{1,3}))|\((?:(?:[^()\\]*)|(?:\\(?:[nrtbf\\()]|[0-7]{1,3})))*\))*\)
                |
                <<
                |
                >>
                |
                <[0-9A-Fa-f]+>
                |
                [\[\]{}]
                )+
                $",
                System.Text.RegularExpressions.RegexOptions.IgnorePatternWhitespace);
            //---Check
            //var canProcess = regex.IsMatch(psString);
            var matches = regex.Matches(psString);
            var captures = matches[0].Groups[1].Captures;
            var tokens = captures.OfType<System.Text.RegularExpressions.Capture>()
                .Select(capture => capture.Value)
                .Where(str => !str.StartsWith("%")) //---Remove comments
                .Where(str => !System.Text.RegularExpressions.Regex.IsMatch(str, (@"^[\t\r\n\f \x00]+$")))
                .ToArray(); //---Remove white spaces
            return tokens;
            //var names = tokens.Where(str => str.StartsWith("/")).Select(s1 => s1.Substring(1));



            //var keywords = tokens
            //    .Where(s2 => !s2.StartsWith("/")) //---Remove names
            //    .Where(s1 => !System.Text.RegularExpressions.Regex.IsMatch(s1, @"^\d+$")) //---Remove numbers
            //    .Where(s1 => !System.Text.RegularExpressions.Regex.IsMatch(s1, @"^-?\d*\.\d+$")) //---Remove numbers
            //    .Where(s1 => !s1.StartsWith("(")) //---Remove strings
            //    .Where(s1 => !System.Text.RegularExpressions.Regex.IsMatch(s1, @"^<[0-9a-fA-F]+>$")) //---Remove hexadecimal strings
            //    .Where(s1 => !System.Text.RegularExpressions.Regex.IsMatch(s1, @"^-[1-9][0-9]*$")) //---Remove numbers
            //    .Where(s1 => !System.Text.RegularExpressions.Regex.IsMatch(s1, @"^16#[0-9A-Fa-f]+$")) //---Remove hexadecimal
            //    .Where(s1 => !System.Text.RegularExpressions.Regex.IsMatch(s1, @"^2#[01]+$")); //---Remove binary
            
            //var operators = keywords.OrderBy(keyword => keyword).GroupBy(keyword => keyword).Select(group => group.Key).Except(names).ToArray();

        }

        public static string[] CreateFromString(string strSource)
        {
            

            //---Used characters
            //Dictionary<byte, long> byteDictionary = new Dictionary<byte, long>();
            //foreach (var item in fileBytes) { if (!byteDictionary.ContainsKey(item)) byteDictionary[item] = 1; else byteDictionary[item]++; }
            //var byteString = Encoding.ASCII.GetString(byteDictionary.Keys.OrderBy(key => key).ToArray());

            var psString = strSource;
            var regex = new System.Text.RegularExpressions.Regex(
                @"^
                (
                [^()%\[\]{}<>/\t\r\n\f \x00]+
                |
                [\t\r\n\f \x00]+
                |
                //?[^()%\[\]{}<>/\t\r\n\f \x00]*
                |
                %[^\n]*\n
                |
                \((?:(?:[^()\\]*)|(?:\\(?:[nrtbf\\()]|[0-7]{1,3}))|\((?:(?:[^()\\]*)|(?:\\(?:[nrtbf\\()]|[0-7]{1,3})))*\))*\)
                |
                <<
                |
                >>
                |
                <[0-9A-Fa-f]+>
                |
                [\[\]{}]
                )+
                $",
                System.Text.RegularExpressions.RegexOptions.IgnorePatternWhitespace);
            //---Check
            //var canProcess = regex.IsMatch(psString);
            var matches = regex.Matches(psString);
            var captures = matches[0].Groups[1].Captures;
            var tokens = captures.OfType<System.Text.RegularExpressions.Capture>()
                .Select(capture => capture.Value)
                .Where(str => !str.StartsWith("%")) //---Remove comments
                .Where(str => !System.Text.RegularExpressions.Regex.IsMatch(str, (@"^[\t\r\n\f \x00]+$")))
                .ToArray(); //---Remove white spaces
            return tokens;
            //var names = tokens.Where(str => str.StartsWith("/")).Select(s1 => s1.Substring(1));



            //var keywords = tokens
            //    .Where(s2 => !s2.StartsWith("/")) //---Remove names
            //    .Where(s1 => !System.Text.RegularExpressions.Regex.IsMatch(s1, @"^\d+$")) //---Remove numbers
            //    .Where(s1 => !System.Text.RegularExpressions.Regex.IsMatch(s1, @"^-?\d*\.\d+$")) //---Remove numbers
            //    .Where(s1 => !s1.StartsWith("(")) //---Remove strings
            //    .Where(s1 => !System.Text.RegularExpressions.Regex.IsMatch(s1, @"^<[0-9a-fA-F]+>$")) //---Remove hexadecimal strings
            //    .Where(s1 => !System.Text.RegularExpressions.Regex.IsMatch(s1, @"^-[1-9][0-9]*$")) //---Remove numbers
            //    .Where(s1 => !System.Text.RegularExpressions.Regex.IsMatch(s1, @"^16#[0-9A-Fa-f]+$")) //---Remove hexadecimal
            //    .Where(s1 => !System.Text.RegularExpressions.Regex.IsMatch(s1, @"^2#[01]+$")); //---Remove binary

            //var operators = keywords.OrderBy(keyword => keyword).GroupBy(keyword => keyword).Select(group => group.Key).Except(names).ToArray();

        }

        public static string[] GetTokenFromString(string strSource)
        {


            //---Used characters
            //Dictionary<byte, long> byteDictionary = new Dictionary<byte, long>();
            //foreach (var item in fileBytes) { if (!byteDictionary.ContainsKey(item)) byteDictionary[item] = 1; else byteDictionary[item]++; }
            //var byteString = Encoding.ASCII.GetString(byteDictionary.Keys.OrderBy(key => key).ToArray());

            var psString = strSource;
            var regex = new System.Text.RegularExpressions.Regex(
                @"^
                (
                [^()%\[\]{}<>/\t\r\n\f \x00]+
                |
                [\t\r\n\f \x00]+
                |
                //?[^()%\[\]{}<>/\t\r\n\f \x00]*
                |
                %[^\n]*\n
                |
                \((?:(?:[^()\\]*)|(?:\\(?:[nrtbf\\()]|[0-7]{1,3}))|\((?:(?:[^()\\]*)|(?:\\(?:[nrtbf\\()]|[0-7]{1,3})))*\))*\)
                |
                <<
                |
                >>
                |
                <[0-9A-Fa-f]+>
                |
                [\[\]{}]
                )+
                $",
                System.Text.RegularExpressions.RegexOptions.IgnorePatternWhitespace);
            //---Check
            //var canProcess = regex.IsMatch(psString);
            var matches = regex.Matches(psString);
            if (matches.Count == 0) return new string[] { };
            var captures = matches[0].Groups[1].Captures;
            var tokens = captures.OfType<System.Text.RegularExpressions.Capture>()
                .Select(capture => capture.Value)
                .Where(str => !str.StartsWith("%")) //---Remove comments
                .Where(str => !System.Text.RegularExpressions.Regex.IsMatch(str, (@"^[\t\r\n\f \x00]+$")))
                .ToArray(); //---Remove white spaces

            var found = tokens[0];
            var post = string.Join(" ", tokens.Skip(1).ToArray());

            return new string[] { found, post };
            //var names = tokens.Where(str => str.StartsWith("/")).Select(s1 => s1.Substring(1));



            //var keywords = tokens
            //    .Where(s2 => !s2.StartsWith("/")) //---Remove names
            //    .Where(s1 => !System.Text.RegularExpressions.Regex.IsMatch(s1, @"^\d+$")) //---Remove numbers
            //    .Where(s1 => !System.Text.RegularExpressions.Regex.IsMatch(s1, @"^-?\d*\.\d+$")) //---Remove numbers
            //    .Where(s1 => !s1.StartsWith("(")) //---Remove strings
            //    .Where(s1 => !System.Text.RegularExpressions.Regex.IsMatch(s1, @"^<[0-9a-fA-F]+>$")) //---Remove hexadecimal strings
            //    .Where(s1 => !System.Text.RegularExpressions.Regex.IsMatch(s1, @"^-[1-9][0-9]*$")) //---Remove numbers
            //    .Where(s1 => !System.Text.RegularExpressions.Regex.IsMatch(s1, @"^16#[0-9A-Fa-f]+$")) //---Remove hexadecimal
            //    .Where(s1 => !System.Text.RegularExpressions.Regex.IsMatch(s1, @"^2#[01]+$")); //---Remove binary

            //var operators = keywords.OrderBy(keyword => keyword).GroupBy(keyword => keyword).Select(group => group.Key).Except(names).ToArray();

        }

    }
}
