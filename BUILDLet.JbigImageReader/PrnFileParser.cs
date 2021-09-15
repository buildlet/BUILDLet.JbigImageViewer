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
    public class PrnFileParser
    {
        // Constructor
        public PrnFileParser(int pjlCommandLinesForNextPage, char[] lineBreakCode = null)
        {
            // Set Number of PJL Command Lines to Find the Next Page
            this.PjlCommandLinesForNextPage = pjlCommandLinesForNextPage;

            if (lineBreakCode != null)
            {
                // Set Line Break Code
                this.LineBreakCode = lineBreakCode;
            }
        }

        // Line Break Code
        public char[] LineBreakCode { get; } = new char[] { '\r', '\n' };

        // Number of PJL Command Lines to Find the Next Page
        public int PjlCommandLinesForNextPage { get; }

        // Read Method
        public List<byte[]> ReadAsJbigImages(string path)
        {
            // Buffer(s)
            List<byte[]> images = new();
            List<byte> image = new();

            // Number of PJL Command Lines
            int pjl_command_line_number = 0;

            // New PJL Parser
            PjlParser pjl = new(this.LineBreakCode);

            // for FileStream
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                // Try to Read File Start Line
                if (!pjl.TryReadFileStartLine(stream))
                {
                    // RETURN All Bytes of file
                    return new List<byte[]> { File.ReadAllBytes(path) };
                }

#if DEBUG
                Debug.WriteLine($"Skip Reading File Start Line", DebugInfo.FullName);
#endif

                // Main Roop
                while (stream.Position < stream.Length)
                {
                    // Read Byte from Stream
                    var read_byte = (byte)stream.ReadByte();

                    // Read Bytes Buffer
                    List<byte> bytes = new();

                    // FLAG for PJL Command Line
                    var pjl_command_line_is_found = false;

                    // Check only 1st Byte (for Performance)
                    if (read_byte == PJL.CommandLineStartText[0])
                    {
                        // Try to Read PJL Command Line
                        pjl_command_line_is_found = pjl.TryReadAsPjlCommandLine(stream, read_byte, out var buffer);

                        // Chack Result
                        if (pjl_command_line_is_found)
                        {
                            // Read as PJL Command Line:

                            // Increament Number of PJL Command Lines
                            pjl_command_line_number++;

#if DEBUG
                            Debug.WriteLine($"Skip Reading PJL Command Line ({pjl_command_line_number})", DebugInfo.FullName);
#endif
                        }
                        else
                        {
                            // NOT Read as PJL Command Line:

                            // Add to Read Bytes Buffer
                            bytes.AddRange(buffer);
                        }
                    }
                    else if (read_byte == PJL.FileEndLineText[0])
                    {
                        // Try to Read File End Line
                        if (pjl.TryReadAsFileEndLine(stream, read_byte, out var buffer))
                        {
                            // File End Line was found;
#if DEBUG
                            Debug.WriteLine($"Skip Reading File End Line", DebugInfo.FullName);
#endif

                            if (image.Count > 0)
                            {
                                // Add image to images
                                images.Add(image.ToArray());
                            }

                            // Break
                            break;
                        }
                        else
                        {
                            // File End Line was NOT found;

                            // Add to Read Bytes Buffer
                            bytes.AddRange(buffer);
                        }
                    }
                    else
                    {
                        // Add to Read Bytes Buffer
                        bytes.Add(read_byte);
                    }


                    // Check if PJL Command Line is now counting
                    if (!pjl_command_line_is_found)
                    {
                        // Check Number of PJL Command Line
                        if (pjl_command_line_number >= this.PjlCommandLinesForNextPage)
                        {
                            // Check read bytes as image
                            if (image.Count > 0)
                            {
                                // Current Page is Ended;
#if DEBUG
                                Debug.WriteLine($"Current Page is Ended.", DebugInfo.FullName);
#endif

                                // Add image to images
                                images.Add(image.ToArray());

                                // Clear image
                                image.Clear();
                            }
                        }

                        // Reset Number of PJL Command Lines
                        pjl_command_line_number = 0;
                    }


                    // Add Read Bytes Buffer to Image Buffer
                    image.AddRange(bytes);
                }
                // Main Roop
            }
            // for FileStream

#if DEBUG
            Debug.WriteLine($"{images.Count} image(s) returned.", DebugInfo.FullName);

            for (int i = 0; i < images.Count; i++)
            {
                Debug.WriteLine($"Image[{i}] = {images[i].Length} bytes", DebugInfo.FullName);
            }
#endif

            // RETURN
            return images;
        }
    }
}
