using System;
using System.Collections;
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
            string filepath = @"G:\Desktop\To Sort Out\Mine\postscript\postscriptbarcode-monolithic-2014-11-12\barcode_with_sample.ps";
            //string filepath = @"C:\Users\Iura\Downloads\postscriptbarcode-monolithic-2017-07-10\barcode_with_sample.ps";
            initialize(new FileInfo(filepath));
            interpreter();
        }

        private static void interpreter()
        {
            while (execstack.Count != 0)
            {
                dynamic top = execstack.Pop();
                dynamic next = top.Next();
                if (next != null && next.ToString().Contains("raiseerror")) System.Diagnostics.Debugger.Break();
                if (next != null) 
                {
                    execstack.Push(top);
                    Type type = next.GetType();
                    if (type == typeof(int)) stack.Push(next);
                    else if (type == typeof(float)) stack.Push(next);
                    else if (type == typeof(string))
                    {
                        if (next.StartsWith("(")) stack.Push(next);
                        else if (next.StartsWith("//"))
                        {
                            Dictionary<string, dynamic> dict = dictstack.FirstOrDefault(d => d.ContainsKey(next));
                            if (dict == null) throw new Exception();
                            stack.Push(dict[next]);
                        }
                        else if (next.StartsWith("/")) stack.Push(next);
                        else
                        {
                            Dictionary<string, dynamic> dict = dictstack.FirstOrDefault(d => d.ContainsKey(next));
                            if (dict == null) throw new Exception(next);
                            dynamic execobject = dict[next];
                            if (execobject is Action) execobject();
                            else stack.Push(execobject);
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

            Dictionary<string, dynamic> genericresource = ResourceFactory();
            Dictionary<string, dynamic> categoryresource = ResourceFactory("Category", "dicttype");
            categoryresource["DefineResource"]("Generic", genericresource);
            categoryresource["DefineResource"]("Category", categoryresource);

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
                ["begin"] = (Action)(() => { dictstack.Push(stack.Pop()); }),
                ["bind"] = (Action)(() =>
                {
                    return;
                    dynamic proc = stack.Pop();
                    for (var i = 0; i < proc.Count; i++)
                    {
                        if (proc[i] is string && !proc[i].StartsWith("/"))
                        {
                            if (systemdict.ContainsKey(proc[i]))
                            {
                                proc[i] = systemdict[proc[i]];
                            }

                        }
                    }
                    stack.Push(proc);
                }),
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
                        if (sourcedict.ContainsKey("Category") && sourcedict["Category"] == "Generic")
                        {
                            dest = ResourceFactory();
                        }
                        else
                        { 
                            foreach (var item in sourcedict)
                            {
                                dest[item.Key] = item.Value;
                            }
                        }
                        stack.Push(dest);
                    }
                    else throw new NotImplementedException();
                }),
                //["counttomark"] = () => { dynamic temp = stack.TakeWhile(el => el != mark).Count(); stack.Push(temp); },
                ["currentdict"] = (Action)(() => { stack.Push(dictstack.Peek()); }),
                //["currentfont"] = () => { stack.Push(graphicstate["currentfont"]); },
                ["currentglobal"] = (Action)(() => { stack.Push(myparams["currentglobal"]); }),
                //["currentpacking"] = () => { stack.Push(systemparams["currentpacking"]); },
                //["currentpoint"] = () => { dynamic temp = graphicstate["currentpoint"]; if (temp == null) throw new Exception(); stack.Push(temp); },
                //["cvi"] = () => { dynamic temp = stack.Pop(); stack.Push(temp is StringBuilderSegment ? int.Parse(temp.ToString()) : (int)temp); },
                ["cvlit"] = (Action)(() => {
                    dynamic temp = stack.Peek();
                    if (temp.StartsWith("(") || temp.StartsWith("/")) return;
                    else System.Diagnostics.Debugger.Break(); }),
                ["cvr"] = (Action)(() => {
                    dynamic temp = stack.Pop();
                    if (temp is int) stack.Push((float)temp);
                    else throw new Exception();
                    //stack.Push(temp is StringBuilderSegment ? float.Parse(temp.ToString()) : (float)temp);
                }),
                //["cvrs"] = () => { dynamic temp = stack.Pop(); dynamic radix = stack.Pop(); stack.Push(Convert.ToInt32(stack.Pop(), radix)); },
                ["cvs"] = (Action)(() => { dynamic temp = stack.Pop(); stack.Push("(" + stack.Pop().ToString() + ")"); }),
                //["cvx"] = () => { throw new NotImplementedException(); },
                ["def"] = (Action)(() => {

                    dynamic value = stack.Pop();
                    dynamic key = stack.Pop();
                    if (key.StartsWith("/")) key = key.Substring(1);
                    else if (key.StartsWith("(")) key = key.Substring(1, key.Length - 2);
                    dictstack.Peek()[key] = value;
                }),
                ["defineresource"] = (Action)(() =>
                {
                    dynamic category = stack.Pop().Substring(1); dynamic instance = stack.Pop(); dynamic key = stack.Pop().Substring(1);
                    if (category == "Category") instance["Category"] = key;
                    categoryresource[category]["DefineResource"](key, instance);
                    stack.Push(instance);
                }),
                ["dict"] = (Action)(() => { stack.Pop(); stack.Push(new Dictionary<string, dynamic>()); }),
                //["div"] = () => { dynamic temp = stack.Pop(); stack.Push(stack.Pop() / temp); },
                //["dtransform"] = () => { throw new NotImplementedException(); },
                ["dup"] = (Action)(() => { stack.Push(stack.Peek()); }),
                ["end"] = (Action)(() => { dictstack.Pop(); }),
                ["eq"] = (Action)(() => { stack.Push(stack.Pop() == stack.Pop()); }),
                ["exch"] = (Action)(() => { dynamic first = stack.Pop(); dynamic second = stack.Pop(); stack.Push(first); stack.Push(second); }),
                ["exec"] = (Action)(() => {
                    execstack.Push(new PSObject(stack.Pop())); }),
                ["exit"] = (Action)(() => {
                    while (!execstack.Peek().Loop) execstack.Pop();
                    execstack.Pop();
                }),
                //["exp"] = () => { dynamic temp = stack.Pop(); stack.Push(Math.Pow((dynamic)stack.Pop(), temp)); },
                ["false"] = (Action)(() => { stack.Push(false); }),
                //["fill"] = () => { throw new NotImplementedException(); },
                ["findfont"] = (Action)(() => { stack.Pop(); stack.Push(new Dictionary<string, dynamic>()); }),
                ["findresource"] = (Action)(() =>
                {
                    dynamic category = stack.Pop(); dynamic key = stack.Pop();
                    if (category.StartsWith("/")) category = category.Substring(1);
                    if (key.StartsWith("/")) key = key.Substring(1);
                    dynamic instance = categoryresource[category]["FindResource"](key);
                    stack.Push(instance);
                }),
                ["for"] = (Action)(() =>
                {
                    dynamic proc = stack.Pop();
                    dynamic end = stack.Pop();
                    dynamic step = stack.Pop();
                    dynamic start = stack.Pop();
                    PSObject temp = new PSObject(proc);
                    temp.LoopArgs = new object[] { (float)start, (float)step, (float)end};
                    //temp.Loop = true;
                    //temp.LoopController = new PSLoopController(start, step, end);
                    execstack.Push(temp);
                }),
                ["forall"] = (Action)(() =>
                {
                    dynamic proc = stack.Pop();
                    dynamic target = stack.Pop();
                    if (target is Dictionary<string, dynamic>)
                    {
                        foreach (dynamic item in target)
                        {
                            stack.Push(item.Key);
                            stack.Push(item.Value);
                            execstack.Push(new PSObject(proc));
                        }
                    }
                    else throw new Exception();
                }),
                //["ge"] = () => { stack.Push(stack.Pop() <= stack.Pop()); },
                //["get"] = () => {
                //    dynamic temp = stack.Pop();
                //    dynamic source = stack.Pop();
                //    stack.Push(source[temp]);
                //},
                ["getinterval"] = (Action)(() =>
                {
                    dynamic count = stack.Pop();
                    dynamic index = stack.Pop();
                    dynamic int_source = stack.Pop();
                    if (int_source is string) stack.Push(int_source.Substring((int)index+1, (int)count));
                    else throw new Exception();//stack.Push(((IEnumerable<dynamic>)stack.Pop()).Skip((int)index).Take((int)count));
                }),
                //["grestore"] = () => { throw new NotImplementedException(); },
                //["gsave"] = () => { throw new NotImplementedException(); },
                //["gt"] = () => { stack.Push(stack.Pop() < stack.Pop()); },
                //["idiv"] = () => { dynamic temp = stack.Pop(); stack.Push((int)(stack.Pop() / temp)); },
                ["if"] = (Action)(() =>
                {
                    dynamic proc = stack.Pop();
                    if (stack.Pop()) execstack.Push(new PSObject(proc));
                }),
                ["ifelse"] = (Action)(() => { dynamic else_proc = stack.Pop(); dynamic if_proc = stack.Pop(); execstack.Push(stack.Pop() ? new PSObject(if_proc) : new PSObject(else_proc)); }),
                //["index"] = () => { dynamic temp = stack.Pop(); stack.Push(stack.Skip(stack.Count - ((int)temp + 1)).Take(1)); },
                ["known"] = (Action)(() => { dynamic temp = stack.Pop(); dynamic dict_source = stack.Pop(); stack.Push(dict_source.ContainsKey(temp.ToString())); }),
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
                ["load"] = (Action)(() =>
                {
                    dynamic key = stack.Pop();
                    Dictionary<string, dynamic> dict = dictstack.FirstOrDefault(d => d.ContainsKey(key.Substring(1)));
                    if (dict == null) throw new Exception();
                    stack.Push(dict[key.Substring(1)]);
                }),
                ["loop"] = (Action)(() => {
                    dynamic proc = stack.Pop();
                    var temp = new PSObject(proc); temp.LoopArgs = new object[] { true };
                    execstack.Push(temp);
                }),
                //["lt"] = () => { stack.Push((int)stack.Pop() > (int)stack.Pop()); },
                //["mark"] = () => { stack.Push(mark); },
                //["mod"] = () => { dynamic temp = stack.Pop(); stack.Push(stack.Pop() % temp); },
                ["moveto"] = (Action)(() => { stack.Pop(); stack.Pop();}),
                //["mul"] = () => { stack.Push((dynamic)stack.Pop() * (dynamic)stack.Pop()); },
                //["ne"] = () => { stack.Push((dynamic)stack.Pop() != (dynamic)stack.Pop()); },
                //["neg"] = () => { stack.Push(-(dynamic)stack.Pop()); },
                //["newpath"] = () => { System.Diagnostics.Debug.Print("newpath"); },
                ["not"] = (Action)(() => { dynamic temp = stack.Pop(); stack.Push(temp is bool ? !temp : ~temp); }),
                //["null"] = () => { stack.Push(null); },
                //["or"] = () => { dynamic temp = stack.Pop(); stack.Push(temp is bool ? (bool)stack.Pop() || temp : stack.Pop() | temp); },
                //["pathbbox"] = () => { throw new NotImplementedException(); },
                ["pop"] = (Action)(() => { stack.Pop(); }),
                ["put"] = (Action)(() => {
                    dynamic value = stack.Pop();
                    if (value is string && value.StartsWith("/")) value = value.Substring(1);
                    dynamic index = stack.Pop();
                    if (index is string && index.StartsWith("/")) index= index.Substring(1);
                    dynamic target = stack.Pop();
                    var d = target as Dictionary<string, dynamic>;
                    if (d == null) throw new Exception();
                    d[index] = value;
                }),
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
                ["scalefont"] = (Action)(() => { stack.Pop(); }),
                ["search"] = (Action)(() =>
                {
                    string temp = stack.Pop();
                    string string_source = stack.Pop();
                    temp = temp.Substring(1, temp.Length - 2);
                    string_source = string_source.Substring(1, string_source.Length - 2);
                    int index = string_source.IndexOf(temp);
                    if (index == -1) { stack.Push("(" + string_source + ")"); stack.Push(false); }
                    else
                    {
                        stack.Push("(" + string_source.Substring(index + temp.Length) + ")");
                        stack.Push("(" + temp + ")");
                        stack.Push("(" + string_source.Substring(0, index) + ")");
                    }
                }),
                //["selectfont"] = () => { graphicstate["currentfont"] = stack.Pop(); },
                //["setcmykcolor"] = () => { graphicstate["currentcolor"] = stack.Take(4).Reverse().ToArray(); for (var i = 0; i < 4; i++) stack.Pop(); },
                ["setglobal"] = (Action)(() => { graphicstate["currentglobal"] = stack.Pop(); }),
                //["setlinecap"] = () => { graphicstate["currentlinecap"] = stack.Pop(); },
                //["setlinewidth"] = () => { graphicstate["currentlinewidth"] = stack.Pop(); },
                //["setrgbcolor"] = () => { graphicstate["currentcolor"] = stack.Take(3).Reverse().ToArray(); for (var i = 0; i < 3; i++) stack.Pop(); },
                ["setfont"] = (Action)(() => { graphicstate["currentfont"] = stack.Pop(); }),
                //["show"] = () => { throw new NotImplementedException(); },
                //["sqrt"] = () => { stack.Push(Math.Sqrt((dynamic)stack.Pop())); },
                //["stop"] = () => { throw new NotImplementedException(); },
                ["string"] = (Action)(() => { stack.Push(new string('\0', stack.Pop())); }),
                //["stringwidth"] = () => { throw new NotImplementedException(); },
                //["stroke"] = () => { throw new NotImplementedException(); },
                ["sub"] = (Action)(() => { dynamic temp = stack.Pop(); stack.Push(stack.Pop() - temp); }),
                ["token"] = (Action)(() =>
                {
                    dynamic token_source = stack.Pop();
                    dynamic token_objects;
                    if (token_source is PSObject) token_objects = token_source;
                    else token_objects= new PSObject(token_source.Substring(1, token_source.Length - 2));
                    dynamic res = token_objects.Next();
                    if (res == null) stack.Push(false);
                    else{
                        stack.Push(token_objects);
                        stack.Push(res);
                        stack.Push(true);
                    }
                }),
                //["translate"] = () => { throw new NotImplementedException(); },
                ["true"] = (Action)(() => { stack.Push(true); }),
                ["type"] = (Action)(() =>
                {
                    dynamic temp = stack.Pop();
                    if (temp is string)
                    {
                        if (temp.StartsWith("(")) stack.Push("/stringtype");
                        else if (temp.StartsWith("/")) stack.Push("/nametype");
                        else System.Diagnostics.Debugger.Break();
                    }
                    else
                    {
                        System.Diagnostics.Debugger.Break();

                    }
                }),
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
        
        private static Dictionary<string, dynamic> ResourceFactory(string category = "Generic", string instancetype = null )
        {
            Dictionary<string, dynamic> resource = new Dictionary<string, dynamic>();
            resource["DefineResource"] = new Action<string, dynamic>((key, obj) => {
                resource[key] = obj;
            });
            resource["UnDefineResource"] = new Action<string>((key) => { resource.Remove(key); });
            resource["FindResource"] = new Func<string, dynamic>((key) => { return resource[key]; });
            resource["ResourceStatus"] = new Func<string, dynamic>((key) => { return null; });
            resource["Category"] = category;
            if (instancetype != null) resource["InstanceType"] = instancetype;
            return resource;
        }
    }

    internal class PSObject
    {
        private string[] _tokens;
        private int _current = 0;
        private bool _evaluated = false;
        private object[] _objects;
        //private bool _loop = false;
        private object[] _loop_args = null;
        private bool args_emitted = false;
        private bool emitting_args = false;
        private IEnumerator iterator = null;

        public PSObject()
        {
        }

        public PSObject(FileSystemInfo file)
        {
            var fileBytes = System.IO.File.ReadAllBytes(file.FullName);
            var psString = Encoding.ASCII.GetString(fileBytes);
            _get_from_string(psString);
        }

        private void _get_from_string(string source)
        {
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
            var matches = regex.Matches(source);
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
            _get_from_string(source);
        }

        public PSObject(object[] objects)
        {
            _evaluated = true;
            _objects = objects;
        }

        public int Position { get { return _current; } }

        public bool Loop { get { return _loop_args != null; } }

        public object[] LoopArgs
        {
            set
            {
                _loop_args = value;
                if (_loop_args.Length == 1)
                {
                    if (_loop_args[0] is Dictionary<string, object>)
                    {
                        iterator = ((Dictionary<string, object>)_loop_args[0]).GetEnumerator();
                        iterator.MoveNext();
                        _objects = Enumerable.Repeat(new object(), 2).Concat(_objects).ToArray();
                    }
                    else if (_loop_args[0] is bool) ;
                    else throw new Exception();
                }
                else if (_loop_args.Length == 3)
                    _objects = Enumerable.Repeat(new object(), 1).Concat(_objects).ToArray();

            }
        }

        public object Next()
        {
            if (!_evaluated)
            {
                if (_current == _tokens.Length)
                    return null;
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
                if (_current != 0 && _current != _objects.Length)
                    return _objects[_current++]; // inside proc
                if (_current == _objects.Length) { // at the end of proc
                    if (_loop_args == null)
                        return null; // not a loop
                    else _current = 0; // loop: wrap around
                }
                // _current == 0;
                if (_loop_args == null) return _objects[_current++];
                if (_loop_args.Length == 1) // loop/repeat/forall
                {
                    if (_loop_args[0] is bool) return _objects[_current++]; // loop
                    else if (_loop_args[0] is int) // repeat
                    {
                        if ((int)_loop_args[0] == 0) return null;
                        else
                        {
                            _loop_args[0] = (int)_loop_args[0] - 1;
                            return _objects[_current++];
                        }
                    }
                    else if (_loop_args[0] is Dictionary<string, object>) //forall - dictionary
                    {
                        if (iterator.Current == null) return null;
                        var p = (KeyValuePair<string, object>)iterator.Current;
                        _objects[0] = p.Key;
                        _objects[1] = p.Value;
                        iterator.MoveNext();
                        return _objects[_current++];
                    }
                    else throw new Exception();
                }
                else if (_loop_args.Length == 3)
                {
                    if ((float)_loop_args[0] > (float)_loop_args[2] && (float)_loop_args[1] > 0 ||
                        (float)_loop_args[0] < (float)_loop_args[2] && (float)_loop_args[1] < 0)
                        return null;
                    _objects[0] = _loop_args[0];
                    _loop_args[0] = (float)_loop_args[0] + (float)_loop_args[1];
                    return _objects[_current++];
                }
                else throw new Exception();
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
