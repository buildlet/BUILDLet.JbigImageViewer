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
using Windows.UI;  // for Color
using System.Runtime.InteropServices.WindowsRuntime;  // for AsBuffer
using System.IO;
using System.Diagnostics;  // for Debug

using BUILDLet.Standard.Diagnostics;  // for DebugInfo

namespace BUILDLet.JbigImageViewer
{
    public class ImageSourceViewModel : INotifyPropertyChanged
    {
        private bool error = false;
        private string error_message = "";

        private uint buffer_size_in_MB;

        private readonly ApplicationDataCompositeValue settings;
        private bool advanced_features;
        private int max_pjl_command_lines;

        private bool in_progress = false;
        private int current_page_number = 0;

        // Internal Image Source List
        private readonly ImageSourceRawDataCollection source_data_collection;


        // Constructor
        public ImageSourceViewModel(uint defaultBufferSizeInKB, int defaultPjlCommandLinesForNextPage)
        {
            // Check Roaming Settings
            if (ApplicationData.Current.RoamingSettings.Values.ContainsKey("BUILDLet.JbigImageViewer"))
            {
                // Get Roaming Settings
                this.settings = ApplicationData.Current.RoamingSettings.Values["BUILDLet.JbigImageViewer"] as ApplicationDataCompositeValue;

                // Get Buffer Size
                this.buffer_size_in_MB = (uint)(this.settings["BufferSize"] ?? defaultBufferSizeInKB);

                // Get Advanced Features Options from Roaming Settings
                this.advanced_features = (bool)(this.settings["AdvancedFeatures"] ?? false);
                this.max_pjl_command_lines = (int)(this.settings["PjlCommandLinesForNextPage"] ?? defaultPjlCommandLinesForNextPage);
            }
            else
            {
                // New Roaming Settings
                this.settings = new ApplicationDataCompositeValue();

                // Set Buffer Size (Default)
                this.buffer_size_in_MB = defaultBufferSizeInKB;

                // Set Advanced Features Options (Default)
                this.advanced_features = false;
                this.max_pjl_command_lines = defaultPjlCommandLinesForNextPage;
            }

            // New ImageSourceRawDataCollection (Set Buffer Size)
            this.source_data_collection = new ImageSourceRawDataCollection(this.buffer_size_in_MB * 1000 * 1000);

            // Register ReadFileStarted Event
            this.ReadFileStarted += (object sender, EventArgs e) => this.RaisePropertyChangedEvent("FileName");
            this.ReadFileStarted += (object sender, EventArgs e) => this.RaisePropertyChangedEvent("FilePath");
            this.ReadFileStarted += (object sender, EventArgs e) => this.RaisePropertyChangedEvent("Pages");
#if DEBUG
            this.ReadFileStarted += (object sender, EventArgs e) =>
            {
                Debug.WriteLine(" " + $"{nameof(ImageSourceViewModel)}.{nameof(ReadFileStarted)} Event is Called.", DebugInfo.FullName);
            };
#endif

            // Register ReadFileCompleted Event
            this.ReadFileCompleted += (object sender, EventArgs e) => this.RaisePropertyChangedEvent("FileName");
            this.ReadFileCompleted += (object sender, EventArgs e) => this.RaisePropertyChangedEvent("FilePath");
            this.ReadFileCompleted += (object sender, EventArgs e) => this.RaisePropertyChangedEvent("Pages");
#if DEBUG
            this.ReadFileCompleted += (object sender, EventArgs e) =>
            {
                Debug.WriteLine(" " + $"{nameof(ImageSourceViewModel)}.{nameof(ReadFileCompleted)} Event is Called.", DebugInfo.FullName);
            };
#endif

            // Register CurrentPageChanged Event
            this.CurrentPageChanged += (object sender, EventArgs e) => this.RaisePropertyChangedEvent("ImageSource");
            this.CurrentPageChanged += (object sender, EventArgs e) => this.RaisePropertyChangedEvent("CurrentPageEnabled");
            this.CurrentPageChanged += (object sender, EventArgs e) => this.RaisePropertyChangedEvent("NextPageEnabled");
            this.CurrentPageChanged += (object sender, EventArgs e) => this.RaisePropertyChangedEvent("PreviousPageEnabled");
            this.CurrentPageChanged += (object sender, EventArgs e) => this.RaisePropertyChangedEvent("NextPageArrowColor");
            this.CurrentPageChanged += (object sender, EventArgs e) => this.RaisePropertyChangedEvent("PreviousPageArrowColor");
#if DEBUG
            this.CurrentPageChanged += (object sender, EventArgs e) =>
            {
                Debug.WriteLine(" " + $"{nameof(ImageSourceViewModel)}.{nameof(CurrentPageChanged)} Event is Called.", DebugInfo.FullName);
            };
#endif
        }


        // Dispatcher for ViewPage
        public CoreDispatcher Dispatcher { get; set; }


        // FileName
        public string FileName => string.IsNullOrEmpty(this.FilePath) ? string.Empty : Path.GetFileName(this.FilePath);

        // FilePath
        public string FilePath { get; set; } = string.Empty;

        // Pages
        public int Pages => this.source_data_collection.Count;

        // ImageSource
        public ImageSource ImageSource =>
            (this.source_data_collection.Count > 0 && this.current_page_number > 0)
            ? this.source_data_collection.GetImageSourceAsync(this.current_page_number - 1).Result
            : null;

        // Current Page Enabled
        public bool CurrentPageEnabled => (!this.InProgress) && (this.source_data_collection.Count > 0);

        // Next Page Enabled
        public bool NextPageEnabled => (!this.InProgress) && (this.current_page_number > 0) && (this.current_page_number < this.source_data_collection.Count);

        // Previous Page Enabled
        public bool PreviousPageEnabled => (!this.InProgress) && (this.current_page_number > 1) && (this.current_page_number <= this.source_data_collection.Count);

        // Color of Arrow in Next Page Button
        public Brush NextPageArrowColor => new SolidColorBrush(this.NextPageEnabled ? Colors.DimGray : Colors.LightGray);

        // Color of Arrow in Previous Page Button
        public Brush PreviousPageArrowColor => new SolidColorBrush(this.PreviousPageEnabled ? Colors.DimGray : Colors.LightGray);

        // Current Page
        public int CurrentPage
        {
            get => this.current_page_number;
            set
            {
                // Validation (Range)
                if ((value == this.source_data_collection.Count) || (value > 0 && value <= this.source_data_collection.Count))
                {
                    if (value != this.current_page_number)
                    {
                        // Set value and Raise Event
                        this.current_page_number = value;

                        // Raise CurrentPageChanged Event
                        this.CurrentPageChanged?.Invoke(this, EventArgs.Empty);
                    }
                }

                // [IMPORTANT]
                // ALWAYS Raise PropertyChangedEvent Event of "CurrentPage" Property
                this.RaisePropertyChangedEvent();
            }
        }

        // In Progress
        public bool InProgress
        {
            get => this.in_progress;
            set
            {
                if (value != this.in_progress)
                {
                    // Set value and Raise Event
                    this.in_progress = value;
                    this.RaisePropertyChangedEvent();
                }
            }
        }


        // Error
        public bool Error
        {
            get => this.error;
            set
            {
                // Always set value and raise event
                this.error = value;
                this.RaisePropertyChangedEvent();
            }
        }

        // Error Message
        public string ErrorMessage
        {
            get => this.error_message;
            set
            {
                if (string.Compare(value, this.error_message, true) != 0)
                {
                    this.error_message = value;
                    this.RaisePropertyChangedEvent();
                }
            }
        }


        // Buffer Size (MB)
        public uint BufferSize
        {
            get => this.buffer_size_in_MB;
            set
            {
                if (value != this.buffer_size_in_MB)
                {
                    // Set value and Raise Event
                    this.buffer_size_in_MB = value;
                    this.RaisePropertyChangedEvent();

                    // Save Buffer Size to Roaming Settings
                    this.settings["BufferSize"] = this.buffer_size_in_MB;

                    // Save settings to ApplicationDataCompositeValue
                    ApplicationData.Current.RoamingSettings.Values["BUILDLet.JbigImageViewer"] = this.settings;
                }
            }
        }

        // Advanced Features option
        public bool AdvancedFeatures
        {
            get => this.advanced_features;
            set
            {
                if (value != this.advanced_features)
                {
                    // Set value and Raise Event
                    this.advanced_features = value;
                    this.RaisePropertyChangedEvent();

                    // Save Advanced Features option to Roaming Settings
                    this.settings["AdvancedFeatures"] = this.advanced_features;

                    // Save settings to ApplicationDataCompositeValue
                    ApplicationData.Current.RoamingSettings.Values["BUILDLet.JbigImageViewer"] = this.settings;
                }
            }
        }

        // Number of PJL Command Lines to find the Next Page
        public int PjlCommandLinesForNextPage
        {
            get => this.max_pjl_command_lines;
            set
            {
                if (value != this.max_pjl_command_lines)
                {
                    // Set value and Raise Event
                    this.max_pjl_command_lines = value;
                    this.RaisePropertyChangedEvent();

                    // Save PJL Command Lines to Roaming Settings
                    this.settings["PjlCommandLinesForNextPage"] = this.max_pjl_command_lines;

                    // Save settings to ApplicationDataCompositeValue
                    ApplicationData.Current.RoamingSettings.Values["BUILDLet.JbigImageViewer"] = this.settings;
                }
            }
        }


        // PropertyChanged Event
        public event PropertyChangedEventHandler PropertyChanged;


        // Current Page Changed Event
        public event EventHandler CurrentPageChanged;

        // Read File Started / Completed Event(s)
        public event EventHandler ReadFileStarted;
        public event EventHandler ReadFileCompleted;


        // Raise PropertyChanged Event
        protected void RaisePropertyChangedEvent([CallerMemberName] string propertyName = null) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        // Raw Data of ImageSource (Current Page)
        public byte[] RawData => this.source_data_collection[this.current_page_number - 1];

        // Get Raw Data of Page
        public byte[] GetRawData(int page) => this.source_data_collection[page - 1];


        // Add BitmapImage from StorageFile
        public async Task AddImageSourceAsync(StorageFile file)
        {
#if DEBUG
            Debug.WriteLine(" " + "Start", DebugInfo.FullName);
#endif

            try
            {
                // Add Image Source
                await this.source_data_collection.AddImageSourceAsync(file);
            }
            catch (Exception e)
            {
                // Set Error
                this.SetError(e.Message);
            }

            // End Read Image File
            this.EndReadImageFile();

            // Reset Current Page to 1
            this.CurrentPage = 1;

#if DEBUG
            Debug.WriteLine(" " + "End", DebugInfo.FullName);
#endif
        }


        // Add BitmapImage from byte array
        public async Task AddImageSourceAsync(byte[] bytes, int index, bool eof = true)
        {
#if DEBUG
            Debug.WriteLine(" " + "Start", DebugInfo.FullName + $"({nameof(index)}={index}, {nameof(eof)}={eof})");
#endif

            try
            {
                // Add Raw Data
                await this.source_data_collection.AddImageSourceRawDataAsync(bytes);
            }
            catch (Exception e)
            {
                // Set Error
                this.SetError(e.Message);
            }

#if DEBUG
            Debug.WriteLine(" " + "Before Raise Event(s) for UI (XAML)", DebugInfo.FullName + $"({nameof(index)}={index}, {nameof(eof)}={eof})");
#endif

            if (eof)
            {
                // End Read Image File
                this.EndReadImageFile();

                // Reset Current Page to 1
                this.CurrentPage = 1;
            }

#if DEBUG
            Debug.WriteLine(" " + "End", DebugInfo.FullName + $"({nameof(index)}={index}, {nameof(eof)}={eof})");
#endif
        }


        // Clear Image
        public void ClearImage()
        {
            // Clear Internal Image List
            this.source_data_collection.Clear();

            // Reset Current Page to 0
            this.CurrentPage = 0;
        }


        // Begin Read Image File
        public void BeginReadImageFile()
        {
            // Set In Progress
            this.InProgress = true;

            // Raise ReadFileStarted Event
            this.ReadFileStarted?.Invoke(this, EventArgs.Empty);
        }


        // End Read Image File
        public void EndReadImageFile()
        {
            // Unset In Progress
            this.InProgress = false;

            // Raise ReadFileCompleted Event
            this.ReadFileCompleted?.Invoke(this, EventArgs.Empty);
        }


        // Set Error
        public void SetError(string message)
        {
            // Set Error
            this.Error = true;

            // Set Error Message
            this.ErrorMessage = message;

            // End Read Image File
            this.EndReadImageFile();
        }


        // Clear Error
        public void ClearError()
        {
            // Unset Error
            this.Error = false;

            // Clear Error Message
            this.ErrorMessage = "";
        }
    }
}
