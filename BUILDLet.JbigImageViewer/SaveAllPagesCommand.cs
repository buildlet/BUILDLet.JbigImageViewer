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
    public class SaveAllPagesCommand : ICommand
    {
        private readonly ImageSourceViewModel view_model;

        // Constructor
        public SaveAllPagesCommand(ImageSourceViewModel viewModel)
        {
            // Set ViewModel
            this.view_model = viewModel;

            // Register Event(s)
            this.view_model.ReadFileStarted += (object sender, EventArgs e) => this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            this.view_model.ReadFileCompleted += (object sender, EventArgs e) => this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => (!this.view_model.InProgress) && (this.view_model.Pages > 1);

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
            picker.SuggestedFileName = Path.GetFileNameWithoutExtension(this.view_model.FilePath) + "_001" + picker.DefaultFileExtension;


            // Get File Path to Save
            var file_1st = await picker.PickSaveFileAsync();

            // for File
            if (file_1st != null)
            {
                // Clear Error
                this.view_model.ClearError();

                // Set In Progress
                this.view_model.InProgress = true;


                // Get Extension
                var extension = Path.GetExtension(file_1st.Path);

                // Validation: File Name for 1st Page
                if (!file_1st.Name.EndsWith($"_001{extension}", StringComparison.OrdinalIgnoreCase))
                {
                    // Set Error
                    this.view_model.SetError($"File name of 1st Page should end with \"_001{extension}\".");
                }
                else
                {
                    // Defer Update till CompleteUpdatesAsync (for 1st file)
                    CachedFileManager.DeferUpdates(file_1st);


                    // Get Folder
                    var folder = await file_1st.GetParentAsync();

                    // Get File Name Base
                    var filename_base = file_1st.Name.Substring(0, file_1st.Name.Length - $"_001{extension}".Length);


                    // for File Existence Check
                    var duplicated_file_list = new List<string>();

                    // File Existence Check (for 1st file)
                    if ((await file_1st.GetBasicPropertiesAsync()).Size != 0)
                    {
                        duplicated_file_list.Add(file_1st.Name);
                    }

                    // File Existence Check (for 2nd file and after)
                    for (int i = 2; i <= this.view_model.Pages; i++)
                    {
                        var filename = $"{filename_base}_{i:000}{extension}";

                        if (await folder.TryGetItemAsync(filename) != null)
                        {
                            duplicated_file_list.Add(filename);
                        }
                    }

                    // File Existence Check
                    if (duplicated_file_list.Count > 0)
                    {
                        // Error Message
                        var message = new StringBuilder("The following file(s) already exist.\n");

                        // Add Duplicated File Path
                        duplicated_file_list.ForEach(filepath => message.AppendLine(filepath));

                        // Set Error
                        this.view_model.SetError(message.ToString());
                    }
                    else
                    {
                        // for Pages
                        for (int i = 1; i <= this.view_model.Pages; i++)
                        {
                            // Create File (or use 1st File)
                            var file = (i == 1) ?
                                file_1st :
                                await folder.CreateFileAsync($"{filename_base}_{i:000}{extension}");


                            if (i != 1)
                            {
                                // Defer Update till CompleteUpdatesAsync (NOT for 1st File)
                                CachedFileManager.DeferUpdates(file);
                            }


                            // Write to File
                            try
                            {
                                await FileIO.WriteBytesAsync(file, this.view_model.GetRawData(i));
                            }
                            catch (Exception e)
                            {
                                // Set Error
                                this.view_model.SetError(e.Message);
                            }


                            // CompleteUpdatesAsync (for all files)
                            var status = await CachedFileManager.CompleteUpdatesAsync(file);

                            // Check Result of CompleteUpdatesAsync
                            if (status != FileUpdateStatus.Complete)
                            {
                                // Set Error
                                this.view_model.SetError($"Status is not {nameof(FileUpdateStatus)}.{nameof(FileUpdateStatus.Complete)} ({status}).");
                            }
                        }
                    }
                }

                // Delete Zero Size 1st file
                if ((await file_1st.GetBasicPropertiesAsync()).Size == 0)
                {
                    await file_1st.DeleteAsync();
                }

                // Reset In Progress
                this.view_model.InProgress = false;
            }
        }
    }
}
