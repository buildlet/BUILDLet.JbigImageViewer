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

namespace BUILDLet.JbigImageReader
{
    public static class PJL
    {
        // PJL Identifier Text
        public static string IdentifierText { get; } = "\x1B%-12345X";

        // PJL Command Line Starting Text
        public static string CommandLineStartText { get; } = "@PJL";

        // File Starting Line Text: "\x1B%-12345X@PJL\r\n" (w/o Line Break Code)
        public static string FileStartLineText { get; } = IdentifierText + CommandLineStartText;

        // File Ending Line Text: "\x1B%-12345X\r\n" (w/o Line Break Code)
        public static string FileEndLineText { get; } = IdentifierText;
    }
}
