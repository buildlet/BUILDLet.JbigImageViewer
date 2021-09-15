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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;  // for CallerMemberName
using System.Windows.Input;   // for ICommand
using System.ComponentModel;  // for INotifyPropertyChanged
using Windows.UI.Xaml.Media;  // for ImageSource
using Windows.UI.Xaml.Media.Imaging;  // for BitmapImage
using Windows.Storage;           // for StorageFile
using Windows.Storage.Pickers;   // for FileOpenPicker
using Windows.Storage.Streams;   // for IRandomAccessStream
using Windows.System.Profile;    // for AnalyticsInfo
using Windows.ApplicationModel;  // for FullTrustProcessLauncher
using Windows.UI.Core;  // for CoreDispatcher

namespace BUILDLet.JbigImageViewer
{
    public class OpenImageCommand : ICommand
    {
        private readonly ImageSourceViewModel view_model;
        private readonly bool desktop;

        // Constructor
        public OpenImageCommand(ImageSourceViewModel viewModel)
        {
            // Set ViewModel
            this.view_model = viewModel;

            // Register Event(s)
            this.view_model.ReadFileStarted += (object sender, EventArgs e) => this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            this.view_model.ReadFileCompleted += (object sender, EventArgs e) => this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);

            // Set Desktop Flag
            this.desktop = (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop");
        }


        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => !this.view_model.InProgress;

        public async void Execute(object parameter)
        {
            // New FileOpenPicker
            var picker = new FileOpenPicker()
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };

            // Add File Type(s) to FileOpenPicker
            ImageSourceFileTypes.Common.ToList().ForEach(type => picker.FileTypeFilter.Add(type));

            // only for Desktop
            if (desktop)
            {
                // Add Additional File Type(s)
                ImageSourceFileTypes.Desktop.ToList().ForEach(type => picker.FileTypeFilter.Add(type));

                // for Advanced Features
                if (this.view_model.AdvancedFeatures)
                {
                    // Additional File Type(s) 2
                    ImageSourceFileTypes.DesktopAdvanced.ToList().ForEach(type => picker.FileTypeFilter.Add(type));
                }
            }


            // Get File
            var file = await picker.PickSingleFileAsync();

            // Set Image Source
            if (file != null)
            {
                // Clear Error
                this.view_model.ClearError();

                // Clear Image
                this.view_model.ClearImage();

                // Set File Path
                this.view_model.FilePath = file.Path;

                // Begin Read Image File
                this.view_model.BeginReadImageFile();

                try
                {
                    // Check if ImageSourceFileTypes.Common or Desktop
                    if (ImageSourceFileTypes.Desktop.Any(type => string.Compare(type, file.FileType, true) == 0) ||
                        ImageSourceFileTypes.DesktopAdvanced.Any(type => string.Compare(type, file.FileType, true) == 0))
                    {
                        // Case of ImageSourceFileTypes.Desktop:

                        // Get Dispatcher
                        this.view_model.Dispatcher = parameter as CoreDispatcher;

                        // Launch JbigImageReader.exe
                        await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync(this.view_model.AdvancedFeatures ? "On" : "Off");
                    }
                    else
                    {
                        // Case of ImageSourceFileTypes.Common:

                        // Add Bitmap Image to View Model
                        await this.view_model.AddImageSourceAsync(file);
                    }
                }
                catch (Exception e)
                {
                    // Set Error
                    this.view_model.SetError(e.Message);
                }
            }
        }
    }
}
