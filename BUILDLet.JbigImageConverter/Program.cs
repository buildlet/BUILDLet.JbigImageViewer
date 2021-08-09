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
using System.Drawing;  // for Bitmap
using System.Drawing.Imaging;  // for ImageFormat
using System.Diagnostics;  // for Debug
using System.Threading.Tasks;  // for Task

using Windows.ApplicationModel.AppService;  // for AppServiceConnection
using Windows.Foundation.Collections;       // for ValueSet

using BUILDLet.Imaging.Jbig;  // for Jbig

namespace BUILDLet.JbigImageConverter
{
    public class Program
    {
        // Retry Limit
        private static readonly int limit = 250;

        // Main
        public static void Main()
        {
            // New ImageSourceSender
            ImageSourceSender sender = new();

            // While Failure (or count limit)
            for (int i = 0; i < Program.limit; i++)
            {
#if DEBUG
                Debug.WriteLine($"{nameof(JbigImageConverter)}.{nameof(Program)}: Count = {i}");
#endif

                // Send ImageSource
                if (sender.SendImageSourceAsync().Result)
                {
                    // Break if Result is true
                    break;
                }
            }
        }
    }
}
