// A simple logger class that uses Console.WriteLine by default.
// Can also do Logger.LogMethod = Debug.Log for Unity etc.
// (this way we don't have to depend on UnityEngine.DLL and don't need a
//  different version for every UnityEngine version here)
using System;

namespace Telepathy
{
    public static class Log
    {
        public static Action<string> Info = UnityEngine.Debug.Log;// Console.WriteLine;
        public static Action<string> Warning = UnityEngine.Debug.LogWarning;// Console.WriteLine;
        public static Action<string> Error = UnityEngine.Debug.LogError;// Console.Error.WriteLine;
    }
}
