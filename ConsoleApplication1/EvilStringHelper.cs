using System;
using System.Linq.Expressions;
using System.Reflection;

public static class EvilStringHelper
{
    private static readonly Action<string, int, char> _setChar;
    private static readonly Action<string, int> _setLength;

    static EvilStringHelper()
    {
        if (Environment.Version.Major < 4)
        {
            MethodInfo setCharMethod = typeof(string).GetMethod(
                "SetChar",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            _setChar = (Action<string, int, char>)Delegate.CreateDelegate(typeof(Action<string, int, char>), setCharMethod);

            MethodInfo setLengthMethod = typeof(string).GetMethod(
                "SetLength",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            _setLength = (Action<string, int>)Delegate.CreateDelegate(typeof(Action<string, int>), setLengthMethod);
        }
        else
        {
            MethodInfo fillStringCheckedMethod = typeof(string).GetMethod(
                "FillStringChecked",
                BindingFlags.Static | BindingFlags.NonPublic
            );

            Action<string, int, string> fillStringCheckedDelegate = (Action<string, int, string>)Delegate.CreateDelegate(
                typeof(Action<string, int, string>), fillStringCheckedMethod
            );

            _setChar = (str, i, c) => fillStringCheckedDelegate(str, i, c.ToString());

            FieldInfo stringLengthField = typeof(string).GetField(
                "m_stringLength",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            var input = Expression.Parameter(typeof(string), "input");
            var length = Expression.Parameter(typeof(int), "length");

            var setLengthLambda = Expression.Lambda<Action<string, int>>(
                Expression.Assign(Expression.Field(input, stringLengthField), length),
                input,
                length
            );

            _setLength = setLengthLambda.Compile();
        }
    }

    public static void ChangeTo(this string text, string value)
    {
        _setLength(text, value.Length);
        for (int i = 0; i < value.Length; ++i)
            text.SetChar(i, value[i]);
    }

    public static void SetChar(this string text, int index, char value)
    {
        _setChar(text, index, value);
    }

    public static void ReverseInPlace(this string text)
    {
        int i = 0;
        int j = text.Length - 1;

        while (i < j)
        {
            char temp = text[j];

            _setChar(text, j--, text[i]);
            _setChar(text, i++, temp);
        }
    }

    //public static unsafe string ReverseOutOfPlace(this string text)
    //{
    //    int length = text.Length;
    //    char* reversed = stackalloc char[length];

    //    int i = 0, j = length - 1;
    //    fixed (char* p = text)
    //    {
    //        while (i < length)
    //        {
    //            reversed[i++] = p[j--];
    //        }
    //    }

    //    return new string(reversed, 0, length);
    //}
}