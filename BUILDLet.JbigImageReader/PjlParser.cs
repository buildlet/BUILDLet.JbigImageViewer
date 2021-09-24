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
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;  // for Debug

using BUILDLet.Standard.Diagnostics;  // for DebugInfo

namespace BUILDLet.JbigImageReader
{
    public class PjlParser
    {
        // Line Break Code
        public static char[] DefaultLineBreakCode { get; } = new char[] { '\r', '\n' };


        // Try Read PJL Identifier Text (w/o Line Break Code)
        public static bool TryReadPjlIdentifierText(Stream stream, byte first, out byte[] buffer, out byte next)
        {
            // Buffer for Fragments
            List<byte> fragments = new();

            // Set index to 0
            var index = 0;

            // Set 1st byte
            next = first;

            // Main Roop
            while (stream.Position < stream.Length)
            {
                // Add Read Byte to Fragment Buffer
                fragments.Add(next);

                // Keep Current Byte
                var current = next;

                // Read Next Byte
                next = (byte)stream.ReadByte();

                // Check 1st Byte of PJL Text
                if (current == PJL.IdentifierText[index++])
                {
                    // Check Length of PJL Text
                    if (index < PJL.IdentifierText.Length)
                    {
                        // Continue
                        continue;
                    }

#if DEBUG
                    Debug.WriteLine($"PJL Identifier Text \"{PJL.IdentifierText}\" was found.", DebugInfo.FullName);
#endif
                }

                // Break
                break;
            }
            // Main Roop

            // Set Buffer
            buffer = fragments.ToArray();

            // RETURN
            return (index >= PJL.IdentifierText.Length);
        }


        // Try to Read PJL Command Line
        public static bool TryReadPjlCommandLine(Stream stream, byte first, out byte[] buffer, out byte next, char[] brCode = null)
        {
            // Check Line Break Code
            brCode ??= PjlParser.DefaultLineBreakCode;

            // Buffer(s)
            List<byte> valid_command_line_buffer = new();
            List<byte> invalid_fragment_buffer = new();

            // Set Line Break Index to 0
            var br_index = 0;

            // Set 1st Byte
            next = first;

            // Main Roop
            while (stream.Position < stream.Length)
            {
                // Check Length of PJL Text
                if (valid_command_line_buffer.Count < PJL.CommandLineStartText.Length)
                {
                    // xxxxxxxxxx@PJLxxxxxxxxxx
                    // <-   Here   ->|

                    // Check 1st Byte of PJL Text
                    if (next == PJL.CommandLineStartText[valid_command_line_buffer.Count])
                    {
                        // xxxxxxxxxx@PJLxxxxxxxxxx
                        //        ->|    |<-
                        //           Here

                        // Add to PJL Command Line Buffer
                        valid_command_line_buffer.Add(next);
                    }
                    else
                    {
                        // xxxxxxxxxx@PJLxxxxxxxxxx
                        // <- Here ->|
                        //
                        //            or
                        //
                        // xxxxxxxxxx@PZLxxxxxxxxxx
                        //             ^
                        //           Here

                        // Add PJL Command Line Buffer & Read Byte to Read Byte Buffer
                        invalid_fragment_buffer.AddRange(valid_command_line_buffer);

                        // Break
                        break;
                    }
                }
                else
                {
                    // xxxxxxxxxx@PJLxxxxxxxxxx
                    //              |<- Here ->

                    if ((next >= 0x20) && (next <= 0x7e))
                    {
                        // @PJLxxxxxxxx\r\n
                        //    |<-    ->|
                        //       Here

                        // Add to PJL Command Line Buffer
                        valid_command_line_buffer.Add(next);
                    }
                    else if (next == brCode[br_index])
                    {
                        // @PJLxxxxxxxx\r\n
                        //           ->|   |<-
                        //             Here

                        // Add to PJL Command Line Buffer
                        valid_command_line_buffer.Add(next);

                        // Increment Line Break Index
                        if (++br_index >= brCode.Length)
                        {
#if DEBUG
                            Debug.WriteLine(
                                $"PJL Command Line \"" +
                                $"{Encoding.ASCII.GetString(valid_command_line_buffer.ToArray()).Replace("\r", "\\r").Replace("\n", "\\n")}" +
                                $"\" was found.", DebugInfo.FullName);
#endif

                            // Read Next Byte to be Returned
                            next = (byte)stream.ReadByte();

                            // Break
                            break;
                        }
                    }
                    else
                    {
                        // Add PJL Command Line Buffer & Read Byte to Read Byte Buffer
                        invalid_fragment_buffer.AddRange(valid_command_line_buffer);

                        // Break
                        break;
                    }
                }

                // Read Next Byte
                next = (byte)stream.ReadByte();
            }
            // Main Roop

            // Set Read Bytes to be Returned
            buffer = invalid_fragment_buffer.Count > 0 ? invalid_fragment_buffer.ToArray() : null;

            // RETURN Result
            return buffer is null;
        }
    }
}
