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
            var pjl_command_line_number = 0;

            // EOF Flag
            var eof = false;

            // for FileStream
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                // Read 1st Byte
                var next = (byte)stream.ReadByte();

                // Main Roop
                while (!eof)
                {
                    // Set EOF Flag
                    eof = !(stream.Position < stream.Length);

                    // Set Current Byte
                    var current = next;

                    // New Buffer for Read Bytes
                    List<byte> bytes = new();

                    // Clear FLAG for PJL Command Line
                    var pjl_command_line_is_found = false;


                    // Check 1st Byte
                    if (current == PJL.CommandLineStartText[0])
                    {
                        // Try to Read PJL Command Line
                        pjl_command_line_is_found = PjlParser.TryReadPjlCommandLine(stream, current, out var buffer, out next, this.LineBreakCode);

                        // Chack Result
                        if (pjl_command_line_is_found)
                        {
                            // PJL Command Line was found:

                            // Increament Number of PJL Command Lines
                            pjl_command_line_number++;

#if DEBUG
                            Debug.WriteLine($"Skip Reading PJL Command Line ({pjl_command_line_number})", DebugInfo.FullName);
#endif
                        }
                        else
                        {
                            // PJL Command Line was NOT found:

                            // Add Read Bytes to Buffer
                            bytes.AddRange(buffer);
                        }
                    }
                    else if (current == PJL.IdentifierText[0])
                    {
                        // Try to Read PJL Identifier Text
                        if (PjlParser.TryReadPjlIdentifierText(stream, current, out var buffer, out next))
                        {
                            // PJL Identifier Text was found:
#if DEBUG
                            Debug.WriteLine($"Skip PJL Identifier Text", DebugInfo.FullName);
#endif

                            // Buffer for Line Break Code
                            List<byte> br_buffer = new();

                            // Check Line Break Code
                            for (int i = 0; i < this.LineBreakCode.Length; i++)
                            {
                                // Check Next Byte
                                if (next != this.LineBreakCode[i])
                                {
                                    // Add Read Bytes to Buffer
                                    bytes.AddRange(br_buffer);

                                    // Break
                                    break;
                                }

                                // Add Byte to Buffer
                                br_buffer.Add(next);

                                // Read Next Byte
                                next = (byte)stream.ReadByte();
                            }
                        }
                        else
                        {
                            // PJL Identifier Text was NOT found:

                            // Add Read Bytes to Buffer
                            bytes.AddRange(buffer);
                        }
                    }
                    else
                    {
                        // Add Current Byte to Buffer
                        bytes.Add(current);

                        // Read Next Byte
                        next = (byte)stream.ReadByte();
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
                                // Current Page is Ended:
#if DEBUG
                                Debug.WriteLine($"Page [{images.Count}] is ended.", DebugInfo.FullName);
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


                    // Check EOF
                    if (eof)
                    {
                        if (image.Count > 0)
                        {
                            // Add image to images
                            images.Add(image.ToArray());
                        }

#if DEBUG
                        Debug.WriteLine($"EOF", DebugInfo.FullName);
#endif

                        // Break
                        break;
                    }


                    // Add Read Bytes Buffer to Image Buffer
                    image.AddRange(bytes);
                }
                // Main Roop
            }
            // for FileStream

#if DEBUG
            Debug.WriteLine($"{images.Count} image(s) will be returned.", DebugInfo.FullName);

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
