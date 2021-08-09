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
    public class ImageSourceViewModel : INotifyPropertyChanged
    {
        private string filename = "";
        private string filepath = null;
        private ImageSource source = null;

        // Constructor
        public ImageSourceViewModel()
        {
            this.OpenComamand = new OpenCommandContent(this);
        }

        // FileName
        public string FileName
        {
            get => this.filename;
            set
            {
                if (string.CompareOrdinal(value, this.filename) != 0)
                {
                    this.filename = value;
                    this.On_PropertyChanged();
                }
            }
        }

        // FilePath
        public string FilePath
        {
            get => this.filepath;
            set
            {
                if (string.CompareOrdinal(value, this.filepath) != 0)
                {
                    this.filepath = value;
                    this.On_PropertyChanged();
                }
            }
        }

        // ImageSource
        public ImageSource ImageSource
        {
            get => this.source;
            set
            {
                this.source = value;
                this.On_PropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void On_PropertyChanged([CallerMemberName] string propertyName = null)
            => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        // Dispatcher of HomePage
        public CoreDispatcher Dispatcher { get; set; }


        // Set BitmapImage to Source from StorageFile
        public async void SetBitmapImageAsync(StorageFile file)
        {
            // New BitmapImage
            var bitmap = new BitmapImage();

            // Read File Async
            using (var stream = await file.OpenReadAsync())
            {
                // Set Image Source
                await bitmap.SetSourceAsync(stream);
            }

            // Set BitmapImage
            this.ImageSource = bitmap;
        }


        // Set BitmapImage to Source from byte[]
        public async void SetBitmapImageAsync(byte[] bytes)
        {
            // New BitmapImage
            var image = new BitmapImage();

            // Convert byte[] to BitmapImage via InMemoryRandomAccessStream
            using (var stream = new InMemoryRandomAccessStream())
            {
                using (var writer = new DataWriter(stream))
                {
                    writer.WriteBytes(bytes);
                    await writer.StoreAsync();
                    await writer.FlushAsync();
                    writer.DetachStream();
                }
                stream.Seek(0);

                // Set Image Source
                await image.SetSourceAsync(stream);
            }

            // Set BitmapImage
            this.ImageSource = image;
        }


        // Command Implementation

        public ICommand OpenComamand { get; private set; }

        private class OpenCommandContent : ICommand
        {
            public OpenCommandContent(ImageSourceViewModel viewModel)
            {
                // Set ViewModel
                this.view_model = viewModel;

                // Set Desktop Flag
                this.desktop = (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop");
            }


            private readonly ImageSourceViewModel view_model;
            private readonly bool desktop;
            private bool can_execute = true;


            // File Type(s) for FileOpenPicker
            private static readonly string[] fileTypes = new string[]
            {
                    ".bmp",
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".gif"
            };

            // Additional File Type(s) only for Windows Desktop
            private static readonly string[] fileTypes2 = new string[]
            {
                    ".jbg",
                    ".jbig"
            };


            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter) => this.can_execute;

            public async void Execute(object parameter)
            {
                // Disable Control
                this.can_execute = false;
                this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);


                // New FileOpenPicker
                var picker = new FileOpenPicker()
                {
                    ViewMode = PickerViewMode.Thumbnail,
                    SuggestedStartLocation = PickerLocationId.PicturesLibrary
                };

                // Add File Type(s) to FileOpenPicker
                OpenCommandContent.fileTypes.ToList().ForEach(type => picker.FileTypeFilter.Add(type));

                // only for Desktop
                if (desktop)
                {
                    // Add Additional File Type(s)
                    OpenCommandContent.fileTypes2.ToList().ForEach(type => picker.FileTypeFilter.Add(type));
                }


                // Get File
                var file = await picker.PickSingleFileAsync();


                // Set Image Source
                if (file != null)
                {
                    // Clear Image
                    this.view_model.ImageSource = null;

                    // Set ViewModel (File Name & Path)
                    this.view_model.FileName = file.Name;
                    this.view_model.FilePath = file.Path;

                    // Check if FileTypes or FileTypes2
                    if (OpenCommandContent.fileTypes2.Any(type => string.Compare(type, file.FileType, true) == 0))
                    {
                        // Case of FileTypes2:

                        // Get Dispatcher
                        this.view_model.Dispatcher = parameter as CoreDispatcher;

                        // Launch JbigImageConverter.exe
                        await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
                    }
                    else
                    {
                        // Case of FileTypes:

                        // Set ViewModel (ImageSource)
                        this.view_model.SetBitmapImageAsync(file);
                    }
                }


                // Enable Control
                this.can_execute = true;
                this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
