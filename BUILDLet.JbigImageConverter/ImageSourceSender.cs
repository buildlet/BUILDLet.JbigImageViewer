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
    public class ImageSourceSender
    {
        // New Connection
        private readonly AppServiceConnection connection = new()
        {
            AppServiceName = "com.BUILDLet.JbigImageViewerService",
            PackageFamilyName = Windows.ApplicationModel.Package.Current.Id.FamilyName
        };


        // Send ImageSource
        public async Task<bool> SendImageSourceAsync()
        {
            // Open Connection
            if (await this.connection.OpenAsync() == AppServiceConnectionStatus.Success)
            {
                // Send Message ("Path" = null)
                var response = await SendMessageAsync(new ValueSet { { "Path", null } });

                // Validation: Status of Response
                if (response.Status != AppServiceResponseStatus.Success)
                {
                    // RETURN false
                    return false;
                }

                // Get File Path
                var filepath = response.Message["Path"] as string;

                // Validation: File Existence
                if (!File.Exists(filepath))
                {
                    // RETURN false
                    return false;
                }

                // Get Bitmap from JBIG file
                var bitmap = Jbig.ToBitmap(filepath);

                // for serializing System.Drawing.Bitmap
                using (var stream = new MemoryStream { Position = 0 })
                {
                    // Save Bitmap to Memory Stream
                    bitmap.Save(stream, ImageFormat.Bmp);

                    // Send Message ("ImageSource" = bitmap)
                    response = await SendMessageAsync(new ValueSet { { "ImageSource", stream.ToArray() } });

                    if (response.Status == AppServiceResponseStatus.Success)
                    {
                        // RETURN true
                        return true;
                    }
                }
            }

            // RETURN false (Default)
            return false;


            // Local Function: Send Message
            async Task<AppServiceResponse> SendMessageAsync(ValueSet message)
            {
                // for Response of SendMessageAsync()
                AppServiceResponse response = null;

                try
                {
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
                    Debug.WriteLine($"{nameof(JbigImageConverter)}.{nameof(Program)}: Response = {response.Status} (\"{message.Keys.First()}\")");
#endif
                }

                // RETURN
                return response;
            }
        }
    }
}
