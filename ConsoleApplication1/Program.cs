using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
        public static Stack<Dictionary<string, dynamic>> dictstack = new Stack<Dictionary<string, dynamic>>();
        private static Dictionary<string, dynamic> systemdict;
        private static object mark = new object();

        private static Stack<dynamic> stack = new Stack<dynamic>();

        static void Main(string[] args)
        {
            string filepath = @"G:\Desktop\To Sort Out\Mine\postscript\postscriptbarcode-monolithic-2014-11-12\barcode_with_sample.ps";
            //string filepath = @"C:\Users\Iura\Downloads\postscriptbarcode-monolithic-2017-07-10\barcode_with_sample.ps";

            var fileBytes = System.IO.File.ReadAllBytes(filepath);
            var psString = Encoding.ASCII.GetString(fileBytes);
            PSObject._get_token_from_string(psString);

            initialize(new FileInfo(filepath));
            interpreter();
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
                ["currentmatrix"] = new float[] { 1, 0, 0, 1, 0, 0 },
                ["currentpath"] = null
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
                #region polymorphic
                ["copy"] = (Action)(() => // stack, array, dictionary, string 
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
                        if (sourcedict.ContainsKey("Category") && (string)sourcedict["Category"] == "Generic")
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
                    else if (temp is string)
                    {
                        string target = (string)temp;
                        string ps_source = stack.Pop();
                        if (ps_source.StartsWith("(") && ps_source.EndsWith(")")) ps_source = ps_source.Substring(1, ps_source.Length - 2);
                        int target_shift = 0;
                        if (target.StartsWith("(") && target.EndsWith(")")) target_shift = 1;
                        for (int i = 0; i < ps_source.Length; i++)
                        {
                            target.SetChar(i + target_shift, ps_source[i]);
                        }
                        stack.Push(target);
                    }
                    else throw new NotImplementedException();
                }),
                ["length"] = (Action)(() => // array, dictionary, string 
                {
                    dynamic temp = stack.Pop();
                    Dictionary<string, object> d = temp as Dictionary<string, object>;
                    if (d != null) { stack.Push(d.Count); return; }
                    if (temp is string && temp.StartsWith("(") && temp.EndsWith(")")) stack.Push(temp.Length - 2);
                    else stack.Push(temp.Length);
                }),
                ["get"] = (Action)(() => // array, dictionary, string 
                {
                    dynamic index = stack.Pop();
                    if (index is float) index = (int)index;
                    dynamic ps_source = stack.Pop();
                    if (ps_source is string && ps_source.StartsWith("(") && ps_source.EndsWith(")")) ps_source = ps_source.Substring(1, ps_source.Length - 2);
                    stack.Push(ps_source[index]);
                }),
                ["put"] = (Action)(() => // array, dictionary, string 
                { 
                    dynamic value = stack.Pop();
                    if (value is string && value.StartsWith("/")) value = value.Substring(1);
                    dynamic index = stack.Pop();
                    if (index is string && index.StartsWith("/")) index = index.Substring(1);
                    dynamic target = stack.Pop();
                    if (index is float) index = (int)index;
                    if (target is string)
                    {
                        if (target.StartsWith("(") && target.EndsWith(")")) index++;
                        ((string)target).SetChar((int)index, (char)value);
                    }
                    else target[index] = value;
                    ;//var d = target as Dictionary<string, dynamic>;
                    ;// if (d == null) throw new Exception();
                    ;// d[index] = value;
                }),
                ["forall"] = (Action)(() => // array, dictionary, string 
                {
                    dynamic proc = stack.Pop();
                    dynamic target = stack.Pop();
                    PSObject temp = new PSObject(proc);
                    temp.LoopArgs = new object[] { target };
                    //temp.Loop = true;
                    //temp.LoopController = new PSLoopController(start, step, end);
                    execstack.Push(temp);
                }),
                ["getinterval"] = (Action)(() => // array, string 
                {
                    dynamic count = stack.Pop();
                    dynamic index = stack.Pop();
                    dynamic int_source = stack.Pop();
                    if (int_source is string && int_source.StartsWith("(")) int_source = int_source.Substring(1, int_source.Length - 2);
                    if (int_source is string) stack.Push("(" + int_source.Substring((int)index, (int)count) + ")");
                    else if (int_source is object[])
                    {
                        var res = ((object[])int_source).Skip((int)index).Take((int)count).ToArray();
                        stack.Push(res);
                    }
                    else throw new Exception();//stack.Push(((IEnumerable<dynamic>)stack.Pop()).Skip((int)index).Take((int)count));
                }),
                ["putinterval"] = (Action)(() => // array, string 
                { 
                    dynamic value = stack.Pop(); 
                    dynamic index = stack.Pop();
                    dynamic target = stack.Pop();
                    if (target is string)
                    {
                        string str_target = (string)target;
                        if (value.StartsWith("(")) value = value.Substring(1, value.Length - 2);
                        for (int i = 0; i < value.Length; i++)
                        {
                            str_target.SetChar((int)index + i, (char)value[i]);
                        }
                    }
                    else if (target is object[]) 
                    {
                        for (int i = 0; i < value.Length; i++)
                        {
                            target[index + i] = value[i];
                        }
                    }
                    else throw new Exception();
                }),
                ["token"] = (Action)(() => // string, file 
                {
                    dynamic token_source = stack.Pop();
                    dynamic token_objects;
                    if (token_source is PSObject) token_objects = token_source;
                    else token_objects = new PSObject(token_source.Substring(1, token_source.Length - 2));
                    dynamic res = token_objects.Next();
                    if (res == null) stack.Push(false);
                    else {
                        stack.Push(token_objects);
                        stack.Push(res);
                        stack.Push(true);
                    }
                }),
                #endregion            

                #region stack manipulation
                ["pop"] = (Action)(() => { stack.Pop(); }),
                ["exch"] = (Action)(() => { dynamic first = stack.Pop(); dynamic second = stack.Pop(); stack.Push(first); stack.Push(second); }),
                ["dup"] = (Action)(() => { stack.Push(stack.Peek()); }),
                ["index"] = (Action)(() => { dynamic temp = stack.Pop(); stack.Push(stack.Skip((int)temp).Take(1).ElementAt(0)); }),
                ["roll"] = (Action)(() =>
                {
                    dynamic steps = stack.Pop();
                    dynamic count = stack.Pop();
                    List<dynamic> templist = new List<dynamic>();
                    while (count-- > 0) templist.Insert(0, stack.Pop());
                    for (int i = 0, l = templist.Count; i < l; i++) stack.Push(templist[(((i - steps) % l) + l) % l]);
                }),
                //["mark"] = () => { stack.Push(mark); },
                //["cleartomark"] = () => { while (stack.Pop() != mark) ; stack.Pop(); },
                ["counttomark"] = (Action)(() => { dynamic temp = stack.TakeWhile(el => el != mark).Count(); stack.Push(temp); }),
                #endregion

                #region arithmetic and math
                //["abs"] = () => { dynamic temp = stack.Pop(); stack.Push(Math.Abs(temp)); },
                ["add"] = (Action)(() => { dynamic temp = stack.Pop(); stack.Push(temp + stack.Pop()); }),
                ["div"] = (Action)(() => { dynamic temp = stack.Pop(); stack.Push(stack.Pop() / temp); }),
                ["idiv"] = (Action)(() => { dynamic temp = stack.Pop(); stack.Push((int)(stack.Pop() / temp)); }),
                ["mod"] = (Action)(() => { dynamic temp = stack.Pop(); stack.Push(stack.Pop() % temp); }),
                ["mul"] = (Action)(() => { stack.Push((dynamic)stack.Pop() * (dynamic)stack.Pop()); }),
                ["sub"] = (Action)(() => { dynamic temp = stack.Pop(); stack.Push(stack.Pop() - temp); }),
                ["neg"] = (Action)(() => { stack.Push(-(dynamic)stack.Pop()); }),
                //["ceiling"] = () => { stack.Push(Math.Ceiling((float)stack.Pop())); },
                //["round"] = () => { stack.Push(Math.Round((dynamic)stack.Pop())); },
                //["sqrt"] = () => { stack.Push(Math.Sqrt((dynamic)stack.Pop())); },
                //["exp"] = () => { dynamic temp = stack.Pop(); stack.Push(Math.Pow((dynamic)stack.Pop(), temp)); },
                //["ln"] = () => { stack.Push(Math.Log((dynamic)stack.Pop())); },
                #endregion

                #region array
                ["array"] = (Action)(() => { dynamic temp = stack.Pop(); stack.Push(new dynamic[temp]); }),
                ["["] = (Action)(() => { stack.Push(mark); }),
                ["]"] = (Action)(() =>
                {
                    dynamic[] temp = stack.TakeWhile(el => !el.Equals(mark)).Reverse().ToArray();
                    while (!stack.Peek().Equals(mark)) stack.Pop();
                    stack.Pop();
                    stack.Push(temp);
                }),
                //["astore"] = () => { dynamic temp = stack.Pop(); int length = temp.Length; for (var i = length; i > 0; i--) temp[i - 1] = stack.Pop(); stack.Push(temp); },
                ["aload"] = (Action)(() => { dynamic temp = stack.Pop(); foreach (var el in temp) stack.Push(el); stack.Push(temp); }),
                #endregion

                #region packed array
                //["currentpacking"] = () => { stack.Push(systemparams["currentpacking"]); },
                #endregion

                #region dictionary
                ["dict"] = (Action)(() => { stack.Pop(); stack.Push(new Dictionary<string, dynamic>()); }),
                ["<<"] = (Action)(() => { stack.Push(mark); }),
                [">>"] = (Action)(() =>
                {
                    Dictionary<string, dynamic> templist = new Dictionary<string, dynamic>();
                    while (!stack.Peek().Equals(mark)) { dynamic value = stack.Pop(); templist[stack.Pop()] = value; }
                    stack.Pop();
                    stack.Push(templist);
                }),
                ["begin"] = (Action)(() => { dictstack.Push(stack.Pop()); }),
                ["end"] = (Action)(() => { dictstack.Pop(); }),
                ["def"] = (Action)(() => 
                {
                    dynamic value = stack.Pop();
                    dynamic key = stack.Pop();
                    if (key.StartsWith("/")) key = key.Substring(1);
                    else if (key.StartsWith("(")) key = key.Substring(1, key.Length - 2);
                    dictstack.Peek()[key] = value;
                }),
                ["load"] = (Action)(() =>
                {
                    dynamic key = stack.Pop();
                    Dictionary<string, dynamic> dict = dictstack.FirstOrDefault(d => d.ContainsKey(key.Substring(1)));
                    if (dict == null) throw new Exception();
                    stack.Push(dict[key.Substring(1)]);
                }),
                ["known"] = (Action)(() => { dynamic temp = stack.Pop(); dynamic dict_source = stack.Pop(); stack.Push(dict_source.ContainsKey(temp.ToString())); }),
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
                ["currentdict"] = (Action)(() => { stack.Push(dictstack.Peek()); }),
                ["$error"] = new Dictionary<string, dynamic>(),
                #endregion

                #region string
                ["string"] = (Action)(() => { stack.Push("(" + new string('\0', stack.Pop()) + ")"); }),
                ["search"] = (Action)(() => 
                {
                    string temp = stack.Pop();
                    string string_source = stack.Pop();
                    if(temp.StartsWith("(") && temp.EndsWith(")")) temp = temp.Substring(1, temp.Length - 2);
                    if(string_source.StartsWith("(") &&  string_source.EndsWith(")")) string_source = string_source.Substring(1, string_source.Length - 2);
                    int index = string_source.IndexOf(temp);
                    if (index == -1) { stack.Push("(" + string_source + ")"); stack.Push(false); }
                    else
                    {
                        stack.Push("(" + string_source.Substring(index + temp.Length) + ")");
                        stack.Push("(" + temp + ")");
                        stack.Push("(" + string_source.Substring(0, index) + ")");
                        stack.Push(true);
                    }
                }),
                #endregion

                #region relational, boolean, bitwise
                ["eq"] = (Action)(() => { stack.Push(stack.Pop().Equals(stack.Pop())); }),
                ["ne"] = (Action)(() => { stack.Push(!stack.Pop().Equals(stack.Pop())); }),
                ["ge"] = (Action)(() => { stack.Push(stack.Pop() <= stack.Pop()); }),
                ["gt"] = (Action)(() => { stack.Push(stack.Pop() < stack.Pop()); }),
                //["le"] = () => { stack.Push((int)stack.Pop() >= (int)stack.Pop()); },
                ["lt"] = (Action)(() => { stack.Push(stack.Pop() > stack.Pop()); }),
                ["and"] = (Action)(() => 
                { 
                    dynamic second = stack.Pop();
                    dynamic first = stack.Pop();
                    dynamic res = (first is bool) ? (first && second) : (first & second);
                    stack.Push(res); 
                }),
                ["not"] = (Action)(() => { dynamic temp = stack.Pop(); stack.Push(temp is bool ? !temp : ~temp); }),
                ["or"] = (Action)(() => 
                {
                    dynamic second = stack.Pop();
                    dynamic first = stack.Pop();
                    dynamic res = (first is bool) ? (first || second) : (first | second);
                    stack.Push(res);
                }),
                //["xor"] = () => { dynamic temp = stack.Pop(); stack.Push(temp ^ stack.Pop()); },
                ["true"] = (Action)(() => { stack.Push(true); }),
                ["false"] = (Action)(() => { stack.Push(false); }),
                //["bitshift"] = () => { dynamic temp = stack.Pop(); stack.Push(temp >= 0 ? stack.Pop() << temp : stack.Pop() >> temp); },
                #endregion

                #region control
                ["exec"] = (Action)(() => {
                    execstack.Push(new PSObject( stack.Pop())); }),
                ["ifelse"] = (Action)(() => { dynamic else_proc = stack.Pop(); dynamic if_proc = stack.Pop(); execstack.Push(new PSObject(stack.Pop() ? if_proc : else_proc)); }),
                ["if"] = (Action)(() =>
                {
                    dynamic proc = stack.Pop();
                    if (stack.Pop()) execstack.Push(new PSObject(proc));
                }),
                ["for"] = (Action)(() =>
                {
                    dynamic proc = stack.Pop();
                    dynamic end = stack.Pop();
                    dynamic step = stack.Pop();
                    dynamic start = stack.Pop();
                    PSObject temp = new PSObject(proc);
                    temp.LoopArgs = new object[] { (float)start, (float)step, (float)end };
                    //temp.Loop = true;
                    //temp.LoopController = new PSLoopController(start, step, end);
                    execstack.Push(temp);
                }),
                ["repeat"] = (Action)(() => {
                    dynamic proc = stack.Pop();
                    dynamic count = stack.Pop();
                    var temp = new PSObject(proc);
                    temp.LoopArgs = new object[] { count };
                    execstack.Push(temp); 
                }),
                ["loop"] = (Action)(() => {
                    dynamic proc = stack.Pop();
                    var temp = new PSObject(proc); temp.LoopArgs = new object[] { true };
                    execstack.Push(temp);
                }),
                ["exit"] = (Action)(() => {
                    while (!execstack.Peek().Loop) execstack.Pop();
                    execstack.Pop();
                }),
                //["stop"] = () => { throw new NotImplementedException(); },
                #endregion

                #region type, attribute, conversion
                ["type"] = (Action)(() =>
                {
                    dynamic temp = stack.Pop();
                    if (temp is string)
                    {
                        if (temp.StartsWith("(")) stack.Push("/stringtype");
                        else if (temp.StartsWith("/")) stack.Push("/nametype");
                        else System.Diagnostics.Debugger.Break();
                    }
                    else if (temp is Dictionary<string, object>)
                        stack.Push("dicttype");
                    else
                    {
                        System.Diagnostics.Debugger.Break();

                    }
                }),
                ["cvlit"] = (Action)(() => {
                    dynamic temp = stack.Peek();
                    if (temp.StartsWith("(") || temp.StartsWith("/")) return;
                    else System.Diagnostics.Debugger.Break(); }),
                //["cvx"] = () => { throw new NotImplementedException(); },
                //["cvi"] = () => { dynamic temp = stack.Pop(); stack.Push(temp is StringBuilderSegment ? int.Parse(temp.ToString()) : (int)temp); },
                ["cvr"] = (Action)(() => {
                    dynamic temp = stack.Pop();
                    if (temp is int) stack.Push((float)temp);
                    else if (temp is float) stack.Push((float)temp);
                    else throw new Exception();
                    //stack.Push(temp is StringBuilderSegment ? float.Parse(temp.ToString()) : (float)temp);
                }),
                ["cvrs"] = (Action)(() => { 
                    dynamic temp = stack.Pop(); 
                    dynamic radix = stack.Pop();
                    dynamic n_source = stack.Pop();
                    dynamic res = Convert.ToString((int)n_source, radix);
                    stack.Push("(" + res + ")"); 
                }),
                ["cvs"] = (Action)(() => { dynamic temp = stack.Pop(); stack.Push("(" + stack.Pop().ToString() + ")"); }),
                #endregion

                #region resource
                ["defineresource"] = (Action)(() =>
                {
                    dynamic category = stack.Pop().Substring(1); dynamic instance = stack.Pop(); dynamic key = stack.Pop().Substring(1);
                    if (category == "Category") instance["Category"] = key;
                    categoryresource[category]["DefineResource"](key, instance);
                    stack.Push(instance);
                }),
                ["findresource"] = (Action)(() =>
                {
                    dynamic category = stack.Pop(); dynamic key = stack.Pop();
                    if (category.StartsWith("/")) category = category.Substring(1);
                    if (key.StartsWith("/")) key = key.Substring(1);
                    dynamic instance = categoryresource[category]["FindResource"](key);
                    stack.Push(instance);
                }),
                #endregion

                #region virtual memory
                ["currentglobal"] = (Action)(() => { stack.Push(myparams["currentglobal"]); }),
                ["setglobal"] = (Action)(() => { graphicstate["currentglobal"] = stack.Pop(); }),
                #endregion

                #region misc
                ["bind"] = (Action)(() =>
                {
                    return;
                    //dynamic proc = stack.Pop();
                    //for (var i = 0; i < proc.Count; i++)
                    //{
                    //    if (proc[i] is string && !proc[i].StartsWith("/"))
                    //    {
                    //        if (systemdict.ContainsKey(proc[i]))
                    //        {
                    //            proc[i] = systemdict[proc[i]];
                    //        }

                    //    }
                    //}
                    //stack.Push(proc);
                }),
                ["null"] = (Action)(() => { stack.Push(null); }),
                #endregion

                #region graphic state
                ["gsave"] = (Action)(() => { }),
                ["grestore"] = (Action)(() => { }),
                ["setlinewidth"] = (Action)(() => { graphicstate["currentlinewidth"] = stack.Pop(); }),
                ["setlinecap"] = (Action)(() => { graphicstate["currentlinecap"] = stack.Pop(); }),
                //["setrgbcolor"] = () => { graphicstate["currentcolor"] = stack.Take(3).Reverse().ToArray(); for (var i = 0; i < 3; i++) stack.Pop(); },
                //["setcmykcolor"] = () => { graphicstate["currentcolor"] = stack.Take(4).Reverse().ToArray(); for (var i = 0; i < 4; i++) stack.Pop(); },
                #endregion

                #region coordinate and matrix
                ["translate"] = (Action)(() => {
                    dynamic y = stack.Pop();
                    dynamic x = stack.Pop();
                    dynamic ctm = graphicstate["currentmatrix"];
                    ctm[4] = x;
                    ctm[5] = y;
                }),
                ["scale"] = (Action)(() => {
                    dynamic y = stack.Pop();
                    dynamic x = stack.Pop();
                    dynamic ctm = graphicstate["currentmatrix"];
                    ctm[0] = x;
                    ctm[3] = y;
                }),
                //["dtransform"] = () => { throw new NotImplementedException(); },
                #endregion

                #region path construction
                ["newpath"] = (Action)(() => {
                    graphicstate["currentpath"] = new object();
                }),
                ["currentpoint"] = (Action)(() => { dynamic temp = graphicstate["currentpoint"]; stack.Push(temp[0]); stack.Push(temp[1]); }),
                ["moveto"] = (Action)(() => { 
                    dynamic x = stack.Pop(); 
                    dynamic y = stack.Pop();
                    graphicstate["currentpoint"] = new dynamic[] { x, y };
                }),
                ["rmoveto"] = (Action)(() => {
                    dynamic dy = stack.Pop();
                    dynamic dx = stack.Pop();
                    dynamic currentpoint = graphicstate["currentpoint"];
                    currentpoint[0] += dx;
                    currentpoint[1] += dy;
                }),
                //["lineto"] = () => { throw new NotImplementedException(); },
                ["rlineto"] = (Action)(() => {
                    stack.Pop(); stack.Pop();
                }),
                //["arc"] = () => { throw new NotImplementedException(); },
                //["arcn"] = () => { throw new NotImplementedException(); },
                ["closepath"] = (Action)(() => {  }),
                //["charpath"] = () => { throw new NotImplementedException(); },
                //["pathbbox"] = () => { throw new NotImplementedException(); },
                #endregion

                #region painting
                ["stroke"] = (Action)(() => {
                    graphicstate["currentpath"] = null;  
                }),
                //["fill"] = () => { throw new NotImplementedException(); },
                #endregion

                #region glyph and font
                ["findfont"] = (Action)(() => { stack.Pop(); stack.Push(new Dictionary<string, dynamic>()); }),
                ["scalefont"] = (Action)(() => { stack.Pop(); }),
                ["setfont"] = (Action)(() => { graphicstate["currentfont"] = stack.Pop(); }),
                //["currentfont"] = () => { stack.Push(graphicstate["currentfont"]); },
                ["selectfont"] = (Action)(() => {
                    dynamic fontsize = stack.Pop();
                    graphicstate["currentfont"] = stack.Pop(); 
                }),
                ["show"] = (Action)(() => { 
                    dynamic text = stack.Pop();
                }),
                //["ashow"] = () => { throw new NotImplementedException(); },
                //["stringwidth"] = () => { throw new NotImplementedException(); },
                #endregion

            };

            dictstack.Push(systemdict);
            PSObject psobject = null;
            if (source is FileInfo) psobject = new PSObject((FileInfo)source);
            else throw new Exception();
            execstack.Push(psobject);
        }

        private static void interpreter()
        {
            while (execstack.Count != 0)
            {
                dynamic top = execstack.Peek();
                dynamic next = top.Next();
                //if (next is string && ((string)next).Contains("token") && !((string)next).Contains("/raiseerror")) ;// System.Diagnostics.Debugger.Break();
                //if (next is string && ((string)next).Contains("gs1-128composite")) ;// System.Diagnostics.Debugger.Break();
                //if (next is string && ((string)next).Contains("barcode")) ;// System.Diagnostics.Debugger.Break();
                if (next != null)
                {
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
                            else if (!next.StartsWith("/") && execobject is object[]) 
                            {
                                execstack.Push(new PSObject(execobject));
                            }
                            else stack.Push(execobject);
                        }
                    }
                    else if (type == typeof(object[])) stack.Push(next);
                    else if (type == typeof(bool)) stack.Push(next);
                    else if (type == typeof(Dictionary<string, object>)) stack.Push(next);
                    else if (type == typeof(PSObject)) stack.Push(next);
                    else System.Diagnostics.Debugger.Break();
                }
                else execstack.Pop();
            }
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

        //[DebuggerDisplay("{ToString()}")]
        internal class PSObject
        {
            private string[] _tokens;
            private int _current = 0;
            private bool _evaluated = false;
            private object[] _objects;
            private object[] _loop_args = null;
            private IEnumerator iterator = null;
            private static Regex test_regex = new Regex(
                    @"^
                    (?:
                    [\t\r\n\f \x00]+
                    |
                    %[^\n]*\n
                    |
                    (
                    /{0,2}[^()%\[\]{}<>/\t\r\n\f \x00]+
                    |
                    \((?:(?:[^()\\]*)|(?:\\(?:[nrtbf\\()]|[0-7]{1,3}))|\((?:(?:[^()\\]*)|(?:\\(?:[nrtbf\\()]|[0-7]{1,3})))*\))*\)
                    |
                    <[0-9A-Fa-f]+>
                    |
                    <<
                    |
                    >>
                    |
                    [\[\]{}]
                    )
                    )+
                    $",
                    RegexOptions.IgnorePatternWhitespace);

            public PSObject(FileSystemInfo file)
            {
                var fileBytes = System.IO.File.ReadAllBytes(file.FullName);
                var psString = Encoding.ASCII.GetString(fileBytes);
                _get_from_string(psString);
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

            public PSObject(PSObject pso_source): this(pso_source._objects) { }           

            private void _get_from_string(string source)
            {
                var regex = new Regex(
                    @"^
                    (?:
                    [\t\r\n\f \x00]+
                    |
                    %[^\n]*\n
                    |
                    (
                    /{0,2}[^()%\[\]{}<>/\t\r\n\f \x00]+
                    |
                    \((?:(?:[^()\\]*)|(?:\\(?:[nrtbf\\()]|[0-7]{1,3}))|\((?:(?:[^()\\]*)|(?:\\(?:[nrtbf\\()]|[0-7]{1,3})))*\))*\)
                    |
                    <[0-9A-Fa-f]+>
                    |
                    <<
                    |
                    >>
                    |
                    [\[\]{}]
                    )
                    )+
                    $",
                    RegexOptions.IgnorePatternWhitespace);
                var matches = regex.Matches(source);
                var captures = matches[0].Groups[1].Captures;
                var tokens = captures.OfType<Capture>()
                    .Select(capture => capture.Value)
                    .Where(str => !str.StartsWith("%")) //---Remove comments
                    .Where(str => !Regex.IsMatch(str, (@"^[\t\r\n\f \x00]+$"))) //---Remove white spaces
                    .ToArray(); 
                _tokens = tokens;
            }

            public static void _get_token_from_string(string source)
            {
                var regex = new Regex(
                    @"
                    (?<!^)(?=[{}])
                    |
                    (?<=^[{}])
                    |
                    (?:[\t\r\n\f \x00]+
                    |
                    %[^\n]*\n+
                    )+
                    ",
                    RegexOptions.IgnorePatternWhitespace);
                var matches = regex.Split(source, 2);
                do
                {
                    matches = regex.Split(matches[1], 2);
                    while (matches[0] == string.Empty) matches = regex.Split(matches[1], 2);
                } while (test_regex.IsMatch(matches[0]));
                
                ;
                //var captures = matches[0].Groups[1].Captures;
                //var tokens = captures.OfType<Capture>()
                //    .Select(capture => capture.Value)
                //    .Where(str => !str.StartsWith("%")) //---Remove comments
                //    .Where(str => !Regex.IsMatch(str, (@"^[\t\r\n\f \x00]+$"))) //---Remove white spaces
                //    .ToArray();
                //_tokens = tokens;
            }

            public int Position { get { return _current - 1; } }

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
                            _objects = Enumerable.Repeat(new object(), 2).Concat(_objects).ToArray();
                        }
                        else if (_loop_args[0] is string)
                        {
                            iterator = ((string)_loop_args[0]).GetEnumerator();
                            _objects = Enumerable.Repeat((object)0, 1).Concat(_objects).ToArray();
                        }
                        else if (_loop_args[0] is object[])
                        {
                            iterator = ((object[])_loop_args[0]).GetEnumerator();
                            _objects = Enumerable.Repeat((object)0, 1).Concat(_objects).ToArray();
                        }
                        else if (_loop_args[0] is bool) { }
                        else if (_loop_args[0] is int)
                        {
                            ;

                        }
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
                                //if (Array.IndexOf(current, "//raiseerror") != -1) System.Diagnostics.Debugger.Break();
                                current = ((dynamic[])current).Select(new Func<dynamic, dynamic>(o =>
                                {
                                    if (!(o is string)) return o;
                                    if (!o.StartsWith("//")) return o;
                                    return dictstack.First(d => d.ContainsKey(((string)o).Substring(2)))[((string)o).Substring(2)];
                                }
                                )).ToArray();
                                current = new PSObject(current);
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
                            if (iterator.MoveNext())
                            {
                                var p = (KeyValuePair<string, object>)iterator.Current;

                                _objects[0] = (p.Key.StartsWith("/") ? "" : "/") + p.Key;
                                _objects[1] = p.Value;
                                return _objects[_current++];
                            }
                            else return null;
                        }
                        else if (_loop_args[0] is string) //forall - string
                        {
                            if (iterator.MoveNext())
                            {
                                var p = (char)iterator.Current;
                                _objects[0] = (int)p;
                                return _objects[_current++];
                            }
                            else return null;
                        }
                        else if (_loop_args[0] is object[]) //forall - array
                        {
                            if (iterator.MoveNext())
                            {
                                dynamic p = iterator.Current;
                                _objects[0] = p;
                                return _objects[_current++];
                            }
                            else return null;
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
                        
            public override string ToString()
            {
                return string.Format("objects: {0}, position: {1}, loop: {2}", (_tokens != null) ? _tokens.Length : _objects.Length, Position, Loop);
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
            if (Regex.IsMatch(source, @"^\((?:(?:(?:[^()\\]*)(?:\\(?:[nrtbf\\()]|[0-7]{1,3}))(?:[^()\\]*))|\((?:(?:[^()\\]*)(?:\\(?:[nrtbf\\()]|[0-7]{1,3}))(?:[^()\\]*))\))+\)$", RegexOptions.IgnorePatternWhitespace))
            {
                if (source.StartsWith("(") && source.EndsWith(")"))
                {
                    ;// System.Diagnostics.Debugger.Break();
                    source = source.Substring(1, source.Length - 2);
                    int index = source.IndexOf(@"\");
                    while (index != -1)
                    {
                        if (source[index + 1] == '(') source = source.Replace(@"\(", "(");
                        else if (source[index + 1] == ')') source = source.Replace(@"\)", ")");
                        else if (Regex.IsMatch(source, @"\\[0-7]{3}"))
                        {                            
                            var split = Regex.Split(source, @"\\[0-7]{3}");
                            var matches = Regex.Matches(source, @"\\[0-7]{3}").Cast<Match>().Select(s => s.Value.Substring(1)).Select(s => Convert.ToInt32(s, 8)).Select(i => (char)i).Select(c => new string(c, 1)).ToArray();
                            Array.Resize(ref matches, split.Length);
                            var match_seq = matches.Select(s => s == null ? "" : s);
                            var result = split.Zip(match_seq, (f, s) => new string[] { f, s }).SelectMany(x => x).Aggregate((s1, s2) => s1 + s2);
                            source= result;
                        }
                        else System.Diagnostics.Debugger.Break();
                        index = source.IndexOf(@"\");
                    }
                    source = "(" + source + ")";
                }
                else System.Diagnostics.Debugger.Break();
                return source;
            } 
            //if(source.Contains("\\")) System.Diagnostics.Debugger.Break();
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
