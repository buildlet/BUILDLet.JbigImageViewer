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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;  // for Windows.Storage.Streams.Buffer.ToArray()
using Windows.Storage;  // for StorageFile
using Windows.Storage.Streams;  // for Buffer, InputStreamOptions
using Windows.UI.Xaml.Media.Imaging;  // for BitmapImage

namespace BUILDLet.JbigImageViewer
{
    public class ImageSourceRawDataCollection : ICollection<byte[]>
    {
        // Inner Collection (List)
        private readonly List<byte[]> data_list = new List<byte[]>();

        // BitmapImage Cache
        private readonly BitmapImage bitmap = new BitmapImage();

        // Buffer for BitmapImage
        private readonly Windows.Storage.Streams.Buffer buffer;

        // Current Index of Image
        private int current_image_index = -1;


        // Constructor
        public ImageSourceRawDataCollection(uint bufferSize)
        {
            // Set Buffer Size
            this.BufferSize = bufferSize;

            // New Buffer
            this.buffer = new Windows.Storage.Streams.Buffer(this.BufferSize);
        }


        // Buffer Size
        public uint BufferSize { get; private set; }

        // Indexer (Additional to ICollection)
        public virtual byte[] this[int index] => this.data_list[index];


        // Count (ICollection)
        public int Count => this.data_list.Count;

        // IsReadOnly (ICollection)
        public bool IsReadOnly { get; } = false;


        // Add Data from Storage File
        public async Task AddImageSourceAsync(StorageFile file)
        {
            // Read File Async
            using (var stream = await file.OpenReadAsync())
            {
                // Read Stream to Buffer Async
                await stream.ReadAsync(this.buffer, this.buffer.Capacity, InputStreamOptions.None);

                // Set BitmapImage only for 1st element
                if (this.data_list.Count < 1)
                {
                    // Set Current Index of Image to 0
                    this.current_image_index = 0;

                    // Seek to HEAD
                    stream.Seek(0);

                    // Set Source Async
                    await bitmap.SetSourceAsync(stream);
                }
            }

            // Add to Data List
            this.data_list.Add(this.buffer.ToArray());
        }


        // Add Raw Data
        public async Task AddImageSourceRawDataAsync(byte[] item)
        {
            // Add to Data List
            this.data_list.Add(item);

            // Set BitmapImage only for 1st element
            if (this.data_list.Count < 2)
            {
                // Set Current Index of Image to 0
                this.current_image_index = 0;

                // Convert byte[] to BitmapImage via InMemoryRandomAccessStream
                using (var stream = new InMemoryRandomAccessStream())
                {
                    // Write to Stream Async
                    _ = await stream.WriteAsync(item.AsBuffer());

                    // Flush Stream Async
                    // _ = await stream.FlushAsync();

                    // Seek to HEAD
                    stream.Seek(0);

                    // Set Source Async
                    await bitmap.SetSourceAsync(stream);
                }
            }
        }


        // Get Bitmap Image Srouce Async
        public async Task<BitmapImage> GetImageSourceAsync(int index)
        {
            if (this.current_image_index < 0)
            {
                // RETURN null
                return null;
            }
            else
            {
                if (index != this.current_image_index)
                {
                    // Set Current Index of Image
                    this.current_image_index = index;

                    // Convert byte[] to BitmapImage via InMemoryRandomAccessStream
                    using (var stream = new InMemoryRandomAccessStream())
                    {
                        // Write to Stream Async
                        _ = await stream.WriteAsync(this.data_list[this.current_image_index].AsBuffer());

                        // Flush Stream Async
                        // _ = await stream.FlushAsync();

                        // Seek to HEAD
                        stream.Seek(0);

                        // Set Source (NOT await: Workaround)
                        _ = bitmap.SetSourceAsync(stream);
                    }
                }

                // RETURN
                return this.bitmap;
            }
        }


        // Add (ICollection)
        public void Add(byte[] item) => this.AddImageSourceRawDataAsync(item).Wait();

        // Clear (ICollection)
        public void Clear()
        {
            // Clear Data List
            this.data_list.Clear();

            // Set Index to -1
            this.current_image_index = -1;
        }

        // Contains (ICollection)
        public bool Contains(byte[] item) => this.data_list.Contains(item);

        // CopyTo (ICollection)
        public void CopyTo(byte[][] array, int arrayIndex) => this.data_list.CopyTo(array, arrayIndex);

        // GetEnemurator (ICollection)
        public IEnumerator<byte[]> GetEnumerator() => this.data_list.GetEnumerator();

        // Remove (ICollection)
        public bool Remove(byte[] item)
        {
            // Set Index to -1
            this.current_image_index = -1;

            // RETURN: result of Clear Data List
            return this.data_list.Remove(item);
        }

        // IEnumerable.GetEnumerator (ICollection)
        IEnumerator IEnumerable.GetEnumerator() => this.data_list.GetEnumerator() as IEnumerator;
    }
}
