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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;  // for ICommand
using Windows.Graphics.Imaging;  // for BitmapEncoder
using Windows.Storage;           // for CachedFileManager
using Windows.Storage.Pickers;   // for FileSavePicker
using Windows.Storage.Provider;  // for FileUpdateStatus
using Windows.Storage.Streams;   // for IRandomAccessStream
using Windows.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;  // for AsBuffer

namespace BUILDLet.JbigImageViewer
{
    public class SavePageAsFileCommand : ICommand
    {
        private readonly ImageSourceViewModel view_model;

        // Constructor
        public SavePageAsFileCommand(ImageSourceViewModel viewModel)
        {
            // Set ViewModel
            this.view_model = viewModel;

            // Register Event(s)
            this.view_model.ReadFileStarted += (object sender, EventArgs e) => this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            this.view_model.ReadFileCompleted += (object sender, EventArgs e) => this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => (!this.view_model.InProgress) && (this.view_model.Pages > 0);

        public async void Execute(object parameter)
        {
            // New FileSavePicker
            var picker = new FileSavePicker()
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                DefaultFileExtension = ".bmp"
            };

            // Add File Type (Bitmap)
            picker.FileTypeChoices.Add("Bitmap", new List<string>(new string[] { ".bmp" }));

            // Set Suggested File Name
            picker.SuggestedFileName = Path.GetFileNameWithoutExtension(this.view_model.FilePath) + picker.DefaultFileExtension;


            // Get File Path to Save
            var file = await picker.PickSaveFileAsync();

            // for File
            if (file != null)
            {
                // Clear Error
                this.view_model.ClearError();

                // Set In Progress
                this.view_model.InProgress = true;

                // Defer Update till CompleteUpdatesAsync
                CachedFileManager.DeferUpdates(file);


                // Write to File
                try
                {
                    await FileIO.WriteBytesAsync(file, this.view_model.RawData);
                }
                catch (Exception e)
                {
                    // Set Error
                    this.view_model.SetError(e.Message);
                }


                // CompleteUpdatesAsync
                var status = await CachedFileManager.CompleteUpdatesAsync(file);

                // Check Result of CompleteUpdatesAsync
                if (status != FileUpdateStatus.Complete)
                {
                    // Set Error
                    this.view_model.SetError($"Status is not {nameof(FileUpdateStatus)}.{nameof(FileUpdateStatus.Complete)} ({status}).");
                }

                // Reset In Progress
                this.view_model.InProgress = false;
            }
        }
    }
}
