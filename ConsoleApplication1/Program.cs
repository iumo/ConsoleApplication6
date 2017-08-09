using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class Program
    {
        private static Stack<PSObject> execstack = new Stack<PSObject>();
        private static Stack<Dictionary<string, dynamic>> dictstack = new Stack<Dictionary<string, dynamic>>();
        private static Dictionary<string, dynamic> systemdict;
        private static object mark = new object();

        private static Stack<dynamic> stack = new Stack<dynamic>();

        static void Main(string[] args)
        {
            initialize(new FileInfo(@"C:\Users\Iura\Downloads\postscriptbarcode-monolithic-2017-07-10\barcode_with_sample.ps"));
            interpreter();
        }

        private static void interpreter()
        {
            while (execstack.Count != 0)
            {
                dynamic top = execstack.Pop();
                dynamic next = top.Next();
                if (next != null) 
                {
                    execstack.Push(top);
                    Type type = next.GetType();
                    if (type == typeof(int)) stack.Push(next);
                    else if (type == typeof(string)) 
                    {
                        if (next.StartsWith("(")) stack.Push(next);
                        else if (next.StartsWith("//")) 
                        {
                            Dictionary <string, dynamic> dict = dictstack.FirstOrDefault(d => d.ContainsKey(next));
                            if (dict == null) throw new Exception();
                            stack.Push(dict[next]);
                        } 
                        else if (next.StartsWith("/")) stack.Push(next);
                        else 
                        {
                            Dictionary<string, dynamic> dict = dictstack.FirstOrDefault(d => d.ContainsKey(next));
                            if (dict == null) throw new Exception(next);
                            dict[next]();
                        }
                    }
                    else if (type == typeof(object[])) stack.Push(next);
                    else System.Diagnostics.Debugger.Break();
                }
            }
        }

        private static void initialize(object source)
        {
            Dictionary<string, object> graphicstate = new Dictionary<string, object>
            {
                ["currentfont"] = new Dictionary<string, dynamic>(),
                ["currentpoint"] = null,
                ["currentcolor"] = new int[] { 0 },
                ["currentlinecap"] = 0,
                ["currentlinewidth"] = 1,
            };

            Dictionary<string, object> myparams = new Dictionary<string, object>
            {
                ["currentglobal"] = false,
                ["currentpacking"] = false,
            };

            Dictionary<string, dynamic> genericresource = new Dictionary<string, dynamic>();
            genericresource["DefineResource"] = new Action<string, dynamic>((str, obj) => { genericresource[str] = obj; });
            genericresource["UnDefineResource"] = new Action<string>((str) => { genericresource.Remove(str); });
            genericresource["FindResource"] = new Func<string, object>((str) => { return genericresource[str]; });
            genericresource["ResourceStatus"] = new Func<string, object>((str) => { return null; });
            genericresource["Category"] = "Generic";

            Dictionary<string, Dictionary<string, dynamic>> resources = new Dictionary<string, Dictionary<string, dynamic>>();
            resources["/Generic"] = genericresource;
            
            Dictionary<string, dynamic> categoryresource = new Dictionary<string, dynamic>();
            categoryresource["DefineResource"] = new Action<string, Dictionary<string, object>>((str, obj) => {
                obj["DefineResource"] = new Action<string, dynamic>((str2, obj2) => { obj[str2] = obj2; });
                obj["FindResource"] = new Func<string, object>((str2) => { return obj[str2]; });
                resources[str] = obj;
            });
            categoryresource["FindResource"] = new Func<string, object>((str) => { return resources[str]; });
            categoryresource["Category"] = "Category";
            resources["/Category"] = categoryresource;

            systemdict = new Dictionary<string, dynamic>
            {
                //["["] = () => { stack.Push(mark); },
                //["]"] = () => {
                //    dynamic[] temp = stack.TakeWhile(el => el.value != mark).Reverse().ToArray();
                //    while (stack.Peek().value != mark) stack.Pop();
                //    stack.Pop();
                //    stack.Push(temp);
                //},
                //["<<"] = () => { stack.Push(mark); },
                //[">>"] = () => {
                //    Dictionary<string, dynamic> templist = new Dictionary<string, dynamic>();
                //    while (stack.Peek() != mark) { dynamic value = stack.Pop(); templist[stack.Pop()] = value; }
                //    stack.Pop();
                //    stack.Push(templist);
                //},
                //["abs"] = () => { dynamic temp = stack.Pop(); stack.Push(Math.Abs(temp)); },
                ["add"] = (Action)(() => { dynamic temp = stack.Pop(); stack.Push(temp + stack.Pop()); } ),
                //["aload"] = () => { dynamic temp = stack.Pop(); foreach (var el in temp) stack.Push(el); stack.Push(temp); },
                //["and"] = () => { dynamic temp = stack.Pop(); stack.Push(temp is bool ? temp && stack.Pop() : temp & stack.Pop()); },
                //["arc"] = () => { throw new NotImplementedException(); },
                //["arcn"] = () => { throw new NotImplementedException(); },
                //["array"] = () => { dynamic temp = stack.Pop(); stack.Push(new dynamic[temp]); },
                //["ashow"] = () => { throw new NotImplementedException(); },
                //["astore"] = () => { dynamic temp = stack.Pop(); int length = temp.Length; for (var i = length; i > 0; i--) temp[i - 1] = stack.Pop(); stack.Push(temp); },
                //["begin"] = () => { dictstack.Push(stack.Pop()); },
                //["bind"] = () => {
                //    return;
                //    dynamic proc = stack.Pop();
                //    for (var i = 0; i < proc.Count; i++)
                //    {
                //        if (proc[i] is string && !proc[i].StartsWith("/"))
                //        {
                //            if (systemdict.ContainsKey(proc[i]))
                //            {
                //                proc[i] = systemdict[proc[i]];
                //            }

                //        }
                //    }
                //    stack.Push(proc);
                //},
                //["bitshift"] = () => { dynamic temp = stack.Pop(); stack.Push(temp >= 0 ? stack.Pop() << temp : stack.Pop() >> temp); },
                //["ceiling"] = () => { stack.Push(Math.Ceiling((float)stack.Pop())); },
                //["charpath"] = () => { throw new NotImplementedException(); },
                //["cleartomark"] = () => { while (stack.Pop() != mark) ; stack.Pop(); },
                //["closepath"] = () => { throw new NotImplementedException(); },
                ["copy"] = (Action)(() =>
                {
                    dynamic temp = stack.Pop();
                    if (temp is int)
                    {
                        var temparray = stack.Take((int)temp).Reverse().ToArray(); foreach (var el in temparray) stack.Push(el);
                    }
                    else if (temp is Dictionary<string, object>)
                    {
                        Dictionary<string, object> dest = temp as Dictionary<string, object>;
                        Dictionary<string, object> sourcedict = stack.Pop() as Dictionary<string, object>;
                        foreach (var item in sourcedict)
                        {
                            dest[item.Key] = item.Value;
                        }
                        stack.Push(dest);
                    }
                    else throw new NotImplementedException();
                }),
                //["counttomark"] = () => { dynamic temp = stack.TakeWhile(el => el != mark).Count(); stack.Push(temp); },
                //["currentdict"] = () => { stack.Push(dictstack.Peek()); },
                //["currentfont"] = () => { stack.Push(graphicstate["currentfont"]); },
                ["currentglobal"] = (Action)(() => { stack.Push(myparams["currentglobal"]); }),
                //["currentpacking"] = () => { stack.Push(systemparams["currentpacking"]); },
                //["currentpoint"] = () => { dynamic temp = graphicstate["currentpoint"]; if (temp == null) throw new Exception(); stack.Push(temp); },
                //["cvi"] = () => { dynamic temp = stack.Pop(); stack.Push(temp is StringBuilderSegment ? int.Parse(temp.ToString()) : (int)temp); },
                //["cvlit"] = () => { /*System.Diagnostics.Debugger.Break(); */},
                //["cvr"] = () => { dynamic temp = stack.Pop(); stack.Push(temp is StringBuilderSegment ? float.Parse(temp.ToString()) : (float)temp); },
                //["cvrs"] = () => { dynamic temp = stack.Pop(); dynamic radix = stack.Pop(); stack.Push(Convert.ToInt32(stack.Pop(), radix)); },
                //["cvs"] = () => { dynamic temp = stack.Pop(); stack.Push(stack.Pop().ToString()); },
                //["cvx"] = () => { throw new NotImplementedException(); },
                //["def"] = () => { dynamic temp = stack.Pop(); (dictstack.Peek())[stack.Pop()] = temp; },
                //["defineresource"] = () => {
                //    dynamic category = stack.Pop(); dynamic instance = stack.Pop(); dynamic key = stack.Pop();
                //    resources[category]["DefineResource"](key, instance);
                //    stack.Push(instance);
                //},
                ["dict"] = (Action)(() => { stack.Pop(); stack.Push(new Dictionary<string, dynamic>()); }),
                //["div"] = () => { dynamic temp = stack.Pop(); stack.Push(stack.Pop() / temp); },
                //["dtransform"] = () => { throw new NotImplementedException(); },
                ["dup"] = (Action)(() => { stack.Push(stack.Peek()); }),
                //["end"] = () => { dictstack.Pop(); },
                //["eq"] = () => { stack.Push(stack.Pop() == stack.Pop()); },
                //["exch"] = () => { dynamic first = stack.Pop(); dynamic second = stack.Pop(); stack.Push(first); stack.Push(second); },
                //["exec"] = () => { dynamic proc = stack.Pop(); interpreter(proc); },
                //["exit"] = () => { exit = true; },
                //["exp"] = () => { dynamic temp = stack.Pop(); stack.Push(Math.Pow((dynamic)stack.Pop(), temp)); },
                //["false"] = () => { stack.Push(false); },
                //["fill"] = () => { throw new NotImplementedException(); },
                //["findfont"] = () => { stack.Pop(); stack.Push(new Dictionary<string, dynamic>()); },
                ["findresource"] = (Action)(() =>
                {
                    dynamic category = stack.Pop(); dynamic key = stack.Pop();
                    dynamic instance = resources[category]["FindResource"](key);
                    stack.Push(instance);
                }),
                //["for"] = () => {
                //    dynamic proc = stack.Pop();
                //    dynamic end = stack.Pop();
                //    dynamic step = stack.Pop();
                //    dynamic start = stack.Pop();
                //    for (int i = start; i != end; i += step)
                //    {
                //        stack.Push(i);
                //        interpreter(proc);
                //    }
                //},
                //["forall"] = () => {
                //    dynamic temp = stack.Pop();
                //    dynamic target = stack.Pop();
                //    foreach (dynamic item in target)
                //    {
                //        stack.Push(item.Key);
                //        stack.Push(item.Value);
                //        interpreter(temp);
                //    }
                //},
                //["ge"] = () => { stack.Push(stack.Pop() <= stack.Pop()); },
                //["get"] = () => {
                //    dynamic temp = stack.Pop();
                //    dynamic source = stack.Pop();
                //    stack.Push(source[temp]);
                //},
                //["getinterval"] = () => {
                //    dynamic count = stack.Pop();
                //    dynamic index = stack.Pop();
                //    dynamic source = stack.Pop();
                //    if (source is StringBuilderSegment) stack.Push(new StringBuilderSegment(source.SB, index, count));
                //    else stack.Push(((IEnumerable<dynamic>)stack.Pop()).Skip((int)index).Take((int)count));
                //},
                //["grestore"] = () => { throw new NotImplementedException(); },
                //["gsave"] = () => { throw new NotImplementedException(); },
                //["gt"] = () => { stack.Push(stack.Pop() < stack.Pop()); },
                //["idiv"] = () => { dynamic temp = stack.Pop(); stack.Push((int)(stack.Pop() / temp)); },
                //["if"] = () => {
                //    dynamic proc = stack.Pop();
                //    if (stack.Pop()) interpreter(proc);
                //},
                ["ifelse"] = (Action)(() => { dynamic else_proc = stack.Pop(); dynamic if_proc = stack.Pop(); execstack.Push(stack.Pop() ? new PSObject(if_proc) : new PSObject(else_proc)); }),
                //["index"] = () => { dynamic temp = stack.Pop(); stack.Push(stack.Skip(stack.Count - ((int)temp + 1)).Take(1)); },
                //["known"] = () => { dynamic temp = stack.Pop(); dynamic source = stack.Pop(); stack.Push(source.ContainsKey(temp.ToString())); },
                //["le"] = () => { stack.Push((int)stack.Pop() >= (int)stack.Pop()); },
                ["length"] = (Action)(() =>
                {
                    dynamic temp = stack.Pop();
                    Dictionary<string, object> d = temp as Dictionary<string, object>;
                    if (d != null) { stack.Push(d.Count); return; }
                    stack.Push(temp.Length);
                }),
                //["lineto"] = () => { throw new NotImplementedException(); },
                //["ln"] = () => { stack.Push(Math.Log((dynamic)stack.Pop())); },
                //["load"] = () => {
                //    dynamic key = stack.Pop();
                //    dynamic dicts = dictstack.ToArray();
                //    foreach (var item in dicts)
                //    {
                //        if (item.ContainsKey(key))
                //        {
                //            stack.Push(item[key]);
                //            break;
                //        }

                //    }
                //    //throw new NotImplementedException(); 
                //},
                //["loop"] = () => {
                //    dynamic proc = stack.Pop();
                //    while (!exit) interpreter(proc);
                //    exit = false;
                //},
                //["lt"] = () => { stack.Push((int)stack.Pop() > (int)stack.Pop()); },
                //["mark"] = () => { stack.Push(mark); },
                //["mod"] = () => { dynamic temp = stack.Pop(); stack.Push(stack.Pop() % temp); },
                //["moveto"] = () => { stack.Pop(); stack.Pop(); System.Diagnostics.Debug.Print("moveto"); },
                //["mul"] = () => { stack.Push((dynamic)stack.Pop() * (dynamic)stack.Pop()); },
                //["ne"] = () => { stack.Push((dynamic)stack.Pop() != (dynamic)stack.Pop()); },
                //["neg"] = () => { stack.Push(-(dynamic)stack.Pop()); },
                //["newpath"] = () => { System.Diagnostics.Debug.Print("newpath"); },
                //["not"] = () => { dynamic temp = stack.Pop(); stack.Push(temp is bool ? !temp : ~temp); },
                //["null"] = () => { stack.Push(null); },
                //["or"] = () => { dynamic temp = stack.Pop(); stack.Push(temp is bool ? (bool)stack.Pop() || temp : stack.Pop() | temp); },
                //["pathbbox"] = () => { throw new NotImplementedException(); },
                //["pop"] = () => { stack.Pop(); },
                //["put"] = () => { dynamic value = stack.Pop(); dynamic index = stack.Pop(); ((dynamic)stack.Pop())[index is StringBuilderSegment ? index.ToString() : index] = value; },
                //["putinterval"] = () => { dynamic value = stack.Pop(); dynamic index = stack.Pop(); throw new Exception(); },
                //["repeat"] = () => { throw new NotImplementedException(); },
                //["rlineto"] = () => { throw new NotImplementedException(); },
                //["rmoveto"] = () => { throw new NotImplementedException(); },
                //["roll"] = () => {
                //    dynamic steps = stack.Pop();
                //    dynamic count = stack.Pop();
                //    List<dynamic> templist = new List<dynamic>();
                //    while (count--) templist.Insert(0, stack.Pop());
                //    for (int i = 0, l = templist.Count; i < l; i++) stack.Push(templist[(((i - steps) % l) + l) % l]);
                //},
                //["round"] = () => { stack.Push(Math.Round((dynamic)stack.Pop())); },
                //["scale"] = () => { throw new NotImplementedException(); },
                //["scalefont"] = () => { stack.Pop(); },
                //["search"] = () => {
                //    string temp = stack.Pop().ToString();
                //    string source = stack.Pop().ToString();
                //    int index = source.IndexOf(temp);
                //    if (index == -1) { stack.Push(source); stack.Push(false); }
                //    else
                //    {
                //        stack.Push(source.Substring(index + temp.Length));
                //        stack.Push(temp);
                //        stack.Push(source.Substring(0, index));
                //    }
                //},
                //["selectfont"] = () => { graphicstate["currentfont"] = stack.Pop(); },
                //["setcmykcolor"] = () => { graphicstate["currentcolor"] = stack.Take(4).Reverse().ToArray(); for (var i = 0; i < 4; i++) stack.Pop(); },
                ["setglobal"] = (Action)(() => { graphicstate["currentglobal"] = stack.Pop(); }),
                //["setlinecap"] = () => { graphicstate["currentlinecap"] = stack.Pop(); },
                //["setlinewidth"] = () => { graphicstate["currentlinewidth"] = stack.Pop(); },
                //["setrgbcolor"] = () => { graphicstate["currentcolor"] = stack.Take(3).Reverse().ToArray(); for (var i = 0; i < 3; i++) stack.Pop(); },
                //["setfont"] = () => { graphicstate["currentfont"] = stack.Pop(); },
                //["show"] = () => { throw new NotImplementedException(); },
                //["sqrt"] = () => { stack.Push(Math.Sqrt((dynamic)stack.Pop())); },
                //["stop"] = () => { throw new NotImplementedException(); },
                //["string"] = () => { stack.Push(new string('\0', (dynamic)stack.Pop())); },
                //["stringwidth"] = () => { throw new NotImplementedException(); },
                //["stroke"] = () => { throw new NotImplementedException(); },
                //["sub"] = () => { dynamic temp = stack.Pop(); stack.Push(stack.Pop() - temp); },
                //["token"] = () => {
                //    dynamic source = stack.Pop();
                //    dynamic res = PSSource.GetTokenFromString(source is string ? source.ToString() : source.SB.ToString());
                //    if (res.Length == 2)
                //    {
                //        stack.Push(res[1]);
                //        stack.Push(res[0]);
                //        stack.Push(true);
                //    }
                //    else stack.Push(false);
                //},
                //["translate"] = () => { throw new NotImplementedException(); },
                ["true"] = (Action)(() => { stack.Push(true); }),
                //["type"] = () => {
                //    dynamic temp = stack.Pop();
                //    if (temp is StringBuilderSegment) stack.Push("/stringtype");
                //    else
                //    {
                //        System.Diagnostics.Debugger.Break();

                //    }
                //},
                ["where"] = (Action)(() =>
                {
                    string temp = (string)stack.Pop();
                    if (temp.StartsWith("/")) temp = temp.Substring(1);
                    if (systemdict.ContainsKey(temp))
                    {
                        stack.Push(systemdict); stack.Push(true);
                    }
                    else stack.Push(false);
                }),
                //["xor"] = () => { dynamic temp = stack.Pop(); stack.Push(temp ^ stack.Pop()); },
            };

            dictstack.Push(systemdict);
            
            

            var psobject = new PSObject((dynamic)source);
            execstack.Push(psobject);
            interpreter();
        }
    }

    internal class PSObject
    {
        private string[] _tokens;
        private int _current = 0;
        private bool _evaluated = false;
        private object[] _objects;

        public PSObject()
        {
        }

        public PSObject(FileSystemInfo file)
        {
            var fileBytes = System.IO.File.ReadAllBytes(file.FullName);
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
            var matches = regex.Matches(psString);
            var captures = matches[0].Groups[1].Captures;
            var tokens = captures.OfType<System.Text.RegularExpressions.Capture>()
                .Select(capture => capture.Value)
                .Where(str => !str.StartsWith("%")) //---Remove comments
                .Where(str => !System.Text.RegularExpressions.Regex.IsMatch(str, (@"^[\t\r\n\f \x00]+$")))
                .ToArray(); //---Remove white spaces
            _tokens = tokens;
        }

        public PSObject(string source)
        {

        }

        public PSObject(object[] objects)
        {
            _evaluated = true;
            _objects = objects;
        }

        public int Position { get { return _current; } }

        public object Next()
        {
            if (!_evaluated)
            {
                if (_current == _tokens.Length) return null;
                dynamic current = PSTypeConverter.Evaluate(_tokens[_current++]);
                if (current.Equals("{"))
                {
                    Stack<object> innerstack = new Stack<object>();
                    do
                    {
                        innerstack.Push(current);
                        current = PSTypeConverter.Evaluate(_tokens[_current++]);
                        if (current.Equals("}"))
                        {
                            Stack<object> temp = new Stack<object>();
                            while (!innerstack.Peek().Equals("{")) temp.Push(innerstack.Pop());
                            current = temp.ToArray();
                            innerstack.Pop();
                        }
                    } while (innerstack.Count != 0);
                }
                return current;
            }
            else
            {
                if (_current == _objects.Length) return null;
                return _objects[_current++];
            }
        }
        
    }

    internal static class PSTypeConverter
    {
        public static object Evaluate(string source)
        {

            if (Regex.IsMatch(source, @"^-?\d+$")) return int.Parse(source);
            if (Regex.IsMatch(source, @"^16#[0-9A-Fa-f]+$")) return Convert.ToInt32(source.Substring(3), 16);
            if (Regex.IsMatch(source, @"^2#[01]+$")) return Convert.ToInt32(source.Substring(2), 2);
            if (Regex.IsMatch(source, @"^-?\d*\.\d+$")) return float.Parse(source.Replace(".", ","));
            if (Regex.IsMatch(source, @"^<[0-9a-fA-F]+>$")) return Encoding.ASCII.GetString(StringToByteArray(source.Substring(1, source.Length - 2)));
            return source;
        }

        private static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
    }
}
