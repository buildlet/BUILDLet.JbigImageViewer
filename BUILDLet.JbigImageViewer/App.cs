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
using System.Diagnostics;  // for Debug
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;  // for overriding OnBackgroundActivated method
using Windows.ApplicationModel.AppService;  // for AppServiceConnection
using Windows.ApplicationModel.Background;  // for BackgroundTaskDeferral
using Windows.Foundation.Collections;       // for ValueSet
using Windows.Storage;          // for ApplicationData
using Windows.Storage.Streams;  // for InMemoryRandomAccessStream
using Windows.UI.Core;  // for Dispatcher
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;  // for ImageSource
using Windows.UI.Xaml.Media.Imaging;  // for BitmapImage

using BUILDLet.Standard.Diagnostics;  // for DebugInfo

namespace BUILDLet.JbigImageViewer
{
    sealed partial class App : Application
    {
        // ViewModel
        public ImageSourceViewModel ViewModel { get; set; } = new ImageSourceViewModel(5, 10);


        // for App Service Connection
        private AppServiceConnection connection = null;

        // for Deferral (BackgroundTaskDeferral)
        private BackgroundTaskDeferral task_deferral = null;


        // OnBackgroundActivated Event Handler
        protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            // Call base method
            base.OnBackgroundActivated(args);


            // Case of AppServiceTriggerDetails
            if (args.TaskInstance.TriggerDetails is AppServiceTriggerDetails)
            {
                // Get Deferral
                this.task_deferral = args.TaskInstance.GetDeferral();

                // Get Connection
                this.connection = (args.TaskInstance.TriggerDetails as AppServiceTriggerDetails).AppServiceConnection;

                // Register Event Handler (TaskInstance)
                args.TaskInstance.Canceled += (IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason) =>
                {
                    this.task_deferral?.Complete();
                };

                // Register Event Handler (AppService Connection)
                this.connection.ServiceClosed += (AppServiceConnection sender, AppServiceClosedEventArgs eventArgs) =>
                {
                    this.task_deferral?.Complete();
                };

                // Register RequestReceived Event Handler (AppService Connection)
                this.connection.RequestReceived += Connection_RequestReceived;
            }
        }


        // RequestReceived Event Handler
        private async void Connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            // Get Deferral
            var deferral = args.GetDeferral();


            // for Byte Array for Bitmap List
            List<byte[]> bitmaps = new List<byte[]>();

            // Get Request Message
            var request_message = args.Request.Message;

            // for Response Message
            ValueSet response_message = null;

            // Check Message
            if (request_message.ContainsKey("Path"))
            {
                // New Response Message
                response_message = new ValueSet { { "Path", this.ViewModel.FilePath } };
            }
            else if (request_message.ContainsKey("BufferSize"))
            {
                // New Response Message
                response_message = new ValueSet { { "BufferSize", this.ViewModel.BufferSize } };
            }
            else if (request_message.ContainsKey("PjlCommandLinesForNextPage"))
            {
                // New Response Message
                response_message = new ValueSet { { "PjlCommandLinesForNextPage", this.ViewModel.PjlCommandLinesForNextPage } };
            }
            else if (request_message.ContainsKey("ImageSource"))
            {
                // Get Byte Array for Bitmap and EOF Flag from Message
                var bytes = request_message["ImageSource"] as byte[];
                var eof = request_message.ContainsKey("EOF") ? (bool)request_message["EOF"] : true;

                // Set ImageSource to ViewModel
                await this.ViewModel.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    // Add Bitmap Image to ViewModel
                    await this.ViewModel.AddImageSourceAsync(bytes, eof);
                });

                // New Response Message
                response_message = new ValueSet { { "ImageSource", null } };
            }
            else if (request_message.ContainsKey("ErrorMessage"))
            {
                // Set Error to ViewModel
                await this.ViewModel.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    // Set Error
                    this.ViewModel.SetError(request_message["ErrorMessage"] as string);
                });
            }


            // Send Message
            if (response_message != null)
            {
                // for Response
                AppServiceResponseStatus response = AppServiceResponseStatus.Unknown;

                try
                {
                    // Send Response
                    response = await args.Request.SendResponseAsync(response_message);
                }
                catch (Exception e)
                {
                    // Set Error to ViewModel
                    await this.ViewModel.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        // Set Error
                        this.ViewModel.SetError(e.Message);
                    });


                    // Complete Deferral
                    deferral.Complete();
                }
                finally
                {
#if DEBUG
                    Debug.WriteLine(" " + $"Response of SendResponseAsync(\"{string.Join(", ", response_message.Keys)}\") = {response}", DebugInfo.FullName);
#endif
                }
            }


            // Complete Deferral
            deferral.Complete();
        }
    }
}
