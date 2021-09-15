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
        private readonly string fileStartLineText;
        private readonly string fileEndLineText;


        // Constructor
        public PjlParser(char[] brCode)
        {
            this.LineBreakCode = brCode;
            this.fileStartLineText = PJL.FileStartLineText + new string(this.LineBreakCode);
            this.fileEndLineText = PJL.FileEndLineText + new string(this.LineBreakCode);
        }


        // Line Break Code
        public char[] LineBreakCode { get; }


        // Try to Read File Start Line
        public bool TryReadFileStartLine(Stream stream)
        {
            // buffer for File Start Line
            var header = new byte[this.fileStartLineText.Length];

            // Read Start Line
            return (stream.Read(header) == this.fileStartLineText.Length) && (string.Compare(this.fileStartLineText, Encoding.ASCII.GetString(header)) == 0);
        }


        // Try to Read as PJL Command Line
        public bool TryReadAsPjlCommandLine(Stream stream, byte first, out byte[] buffer)
        {
            // Buffer(s)
            List<byte> command_line_buffer = new();
            List<byte> bytes = new();

            // Line Break Index
            var br_index = 0;

            // Set 1st Byte
            var read_byte = first;

            // Main Roop
            while (stream.Position < stream.Length)
            {
                // Check PJL Text
                if (command_line_buffer.Count < PJL.CommandLineStartText.Length)
                {
                    // xxxxxxxxxx@PJLxxxxxxxxxx
                    // <-   Here   ->|

                    if (read_byte == PJL.CommandLineStartText[command_line_buffer.Count])
                    {
                        // xxxxxxxxxx@PJLxxxxxxxxxx
                        //        ->|    |<-
                        //           Here

                        // Add to PJL Command Line Buffer
                        command_line_buffer.Add(read_byte);
                    }
                    else
                    {
                        // xxxxxxxxxx@PJLxxxxxxxxxx
                        // <- Here ->|
                        //
                        //            or
                        //
                        // xxxxxxxxxx@PALxxxxxxxxxx
                        //             ^
                        //           Here

                        // Add PJL Command Line Buffer & Read Byte to Read Byte Buffer
                        bytes.AddRange(command_line_buffer);
                        bytes.Add(read_byte);

                        // Break
                        break;
                    }
                }
                else
                {
                    // xxxxxxxxxx@PJLxxxxxxxxxx
                    //              |<- Here ->

                    if ((read_byte >= 0x20) && (read_byte <= 0x7e))
                    {
                        // @PJLxxxxxxxx\r\n
                        //    |<-    ->|
                        //       Here

                        // Add to PJL Command Line Buffer
                        command_line_buffer.Add(read_byte);
                    }
                    else if (read_byte == this.LineBreakCode[0])
                    {
                        // @PJLxxxxxxxx\r\n
                        //              ^
                        //             Here

                        // Add to PJL Command Line Buffer
                        command_line_buffer.Add(read_byte);

                        // Increment Line Break Index
                        br_index++;
                    }
                    else if ((read_byte == this.LineBreakCode[1]) && (br_index > 0))
                    {
                        // @PJLxxxxxxxx\r\n
                        //                ^
                        //               Here

                        // Add to PJL Command Line Buffer
                        command_line_buffer.Add(read_byte);

#if DEBUG
                        Debug.WriteLine($"PJL Command Line \"{Encoding.ASCII.GetString(command_line_buffer.ToArray())}\" is found.", DebugInfo.FullName);
#endif

                        // Break
                        break;
                    }
                    else
                    {
                        // Add PJL Command Line Buffer & Read Byte to Read Byte Buffer
                        bytes.AddRange(command_line_buffer);
                        bytes.Add(read_byte);

                        // Break
                        break;
                    }
                }

                // Read Byte from Stream
                read_byte = (byte)stream.ReadByte();
            }
            // Main Roop

            // Set Read Bytes
            buffer = bytes.Count > 0 ? bytes.ToArray() : null;

            // RETURN Result
            return buffer is null;
        }


        // Try to Read as File End Line
        public bool TryReadAsFileEndLine(Stream stream, byte first, out byte[] buffer)
        {
            // Buffer(s)
            List<byte> file_end_line_buffer = new();
            List<byte> bytes = new();

            // Set 1st Byte
            var read_byte = first;

            // Main Roop
            while (stream.Position < stream.Length)
            {
                // Check File Ending Text
                if (read_byte == PJL.FileEndLineText[file_end_line_buffer.Count])
                {
                    // Add to File Ending Line Buffer
                    file_end_line_buffer.Add(read_byte);

                    // Check Length
                    if (file_end_line_buffer.Count == PJL.FileEndLineText.Length)
                    {
                        // Match File Ending Line:
                        
#if DEBUG
                        Debug.WriteLine($"File End Line \"{Encoding.ASCII.GetString(file_end_line_buffer.ToArray())}\" is found.", DebugInfo.FullName);
#endif

                        // Break
                        break;
                    }
                }
                else
                {
                    // NOT Match to File Ending Line:

                    // Add PJL Command Line Buffer & Read Byte to Read Byte Buffer
                    bytes.AddRange(file_end_line_buffer);
                    bytes.Add(read_byte);

                    // Break
                    break;
                }

                // Read Byte from Stream
                read_byte = (byte)stream.ReadByte();
            }
            // Main Roop

            // Set Read Bytes
            buffer = bytes.Count > 0 ? bytes.ToArray() : null;

            // RETURN Result
            return buffer is null;
        }
    }
}
