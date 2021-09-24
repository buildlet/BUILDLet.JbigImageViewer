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
using System.Collections.Generic;
using System.Linq;
using System.Drawing;  // for Bitmap
using System.Drawing.Imaging;  // for ImageFormat
using System.Diagnostics;  // for Debug
using System.Threading.Tasks;  // for Task
using Windows.ApplicationModel.AppService;  // for AppServiceConnection
using Windows.Foundation.Collections;       // for ValueSet

using BUILDLet.Imaging.Jbig;  // for Jbig
using BUILDLet.Standard.Diagnostics;  // for DebugInfo

namespace BUILDLet.JbigImageReader
{
    public class ImageSourceTransferClient
    {
        // New Connection
        private readonly AppServiceConnection connection = new()
        {
            AppServiceName = "com.BUILDLet.JbigImageViewerService",
            PackageFamilyName = Windows.ApplicationModel.Package.Current.Id.FamilyName
        };


        // Send ImageSource
        public async Task<bool> SendImageSourceAsync(bool advanced)
        {
            // Open Connection
            if (await this.connection.OpenAsync() == AppServiceConnectionStatus.Success)
            {
                // for AppServiceResponse
                AppServiceResponse response;


                // Path:

                // Send Message ("Path" = null)
                if ((response = await SendMessageAsync(new ValueSet { { "Path", null } })).Status != AppServiceResponseStatus.Success)
                {
                    // RETURN false
                    return false;
                }

                // Get File Path
                var filepath = response.Message["Path"] as string;

                // Validation: File Existence
                if (!File.Exists(filepath))
                {
                    // ERROR: Send Message ("ErrorMessage" = e.Message)
                    response = await this.SendMessageAsync(new ValueSet { { "ErrorMessage", new FileNotFoundException().Message } });
                }


                // BufferSize:

                // Send Message ("BufferSize" = null)
                if ((response = await SendMessageAsync(new ValueSet { { "BufferSize", null } })).Status != AppServiceResponseStatus.Success)
                {
                    // RETURN false
                    return false;
                }

                // Get Buffer Size (MB)
                var buffer_size_in_MB = (uint)response.Message["BufferSize"];


                // ImageSource:

                // New Bitmap List and Bytes Array List
                List<Bitmap> bitmaps = new();

                // Get Bitmap Image(s) from JBIG Image(s)
                try
                {
                    // Get Bitmap from JBIG file
                    try
                    {
                        // At first, try to read as plain JBIG file, even if Advanced Feature is enabled;

                        // Add Bitmap Image from a Spool file read as a simple JBIG file
                        bitmaps.Add(JbigImage.ToBitmap(filepath, (int)buffer_size_in_MB * 1000 * 1000));
                    }
                    catch (Exception)
                    {
                        if (advanced)
                        {
                            // Advanced Features is ON:

                            // Send Message ("PjlCommandLinesForNextPage" = null)
                            if ((response = await SendMessageAsync(new ValueSet { { "PjlCommandLinesForNextPage", null } })).Status != AppServiceResponseStatus.Success)
                            {
                                // RETURN false
                                return false;
                            }

                            // Get Number of PJL Command Lines
                            var pjl_command_lines = (int)response.Message["PjlCommandLinesForNextPage"];

                            // New PrnFile Parser
                            PrnFileParser prnFile = new(pjl_command_lines, new char[] { '\r', '\n' });

                            // Add Bitmap Image(s) from a Spool file read as multiple JBIG image(s)
                            prnFile.ReadAsJbigImages(filepath).ForEach(bytes => bitmaps.Add(JbigImage.ToBitmap(bytes, (int)buffer_size_in_MB * 1000 * 1000)));
                        }
                        else
                        {
                            // Advanced Features is OFF:

                            // Throw Exception
                            throw;
                        }
                    }
                }
                catch (Exception e)
                {
                    // ERROR: Send Message ("ErrorMessage" = e.Message)
                    response = await this.SendMessageAsync(new ValueSet { { "ErrorMessage", e.Message } });
                }

                // for Bitmap List
                for (int i = 0; i < bitmaps.Count; i++)
                {
                    // for serializing System.Drawing.Bitmap
                    using (var stream = new MemoryStream { Position = 0 })
                    {
                        // Save Bitmap to Memory Stream
                        bitmaps[i].Save(stream, ImageFormat.Bmp);

                        // New Message
                        var message = new ValueSet {
                            { "ImageSource", stream.ToArray() },
                            { "Index", i },
                            { "EOF", i >= (bitmaps.Count - 1) }
                        };

                        // Send Message ("ImageSource" = bitmap, "EOF" = true or false)
                        if ((response = await this.SendMessageAsync(message)).Status != AppServiceResponseStatus.Success)
                        {
                            // RETURN false
                            return false;
                        }
                    }
                }


                // RETURN true (Complete)
                return true;
            }

            // RETURN false (Default)
            return false;
        }


        // Send Message
        private async Task<AppServiceResponse> SendMessageAsync(ValueSet message)
        {
            // for Response of SendMessageAsync()
            AppServiceResponse response = null;

            try
            {
#if DEBUG
                var message_values = new List<string>();
                foreach (var key in message.Keys) { message_values.Add($"\"{key}\"={message[key] ?? "null"}"); }
                Debug.WriteLine($"SendMessageAsync({string.Join(", ", message_values)})", DebugInfo.FullName);
#endif

                // Send Message
                response = await this.connection.SendMessageAsync(message);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
#if DEBUG
                var message_values = new List<string>();
                foreach (var key in message.Keys) { message_values.Add($"\"{key}\"={message[key]??"null"}"); }
                Debug.WriteLine($"Response of SendMessageAsync({string.Join(", ", message_values)}) = {response.Status}", DebugInfo.FullName);
#endif
            }

            // RETURN
            return response;
        }
    }
}
