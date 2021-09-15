/***************************************************************************************************
The MIT License (MIT)

Copyright 2021 Daiki Sakamoto

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
associated documentation files (the "Software"), to deal in the Software without restriction, 
including without limitation the rights to use, copy, modify, merge, publish, distribute, 
sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is 
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or 
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT 
NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
***************************************************************************************************/
using System;
using System.IO;
using System.Linq;
using System.Diagnostics;  // for Debug

using BUILDLet.Standard.Diagnostics;  // for DebugInfo

namespace BUILDLet.JbigImageReader
{
    public class Program
    {
        // Retry Count Limit
        private static readonly int limit = 255;

        // Main
        public static void Main(string[] args)
        {
            // Get Advanced Features option from args[2]
            var advanced = string.Compare(args[2], "On") == 0;

            // New JbigImageSourceProvider
            ImageSourceTransferClient provider = new();

            // While Failure (or count limit)
            for (int i = 0; i < Program.limit; i++)
            {
#if DEBUG
                Debug.WriteLine($"Count = {i}", DebugInfo.FullName);
#endif

                // Send ImageSource
                if (provider.SendImageSourceAsync(advanced).Result)
                {
                    // Break if Result is true
                    break;
                }
            }
        }
    }
}
