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
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Core;  // for Dispatcher

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace BUILDLet.JbigImageViewer
{
    public sealed partial class ViewPage : Page
    {
        public ViewPage()
        {
            this.InitializeComponent();

            // Set ViewModel
            this.ViewModel = ((App)App.Current).ViewModel;

            // New ImageSourceOpenCommand
            this.OpenComamand = new OpenImageCommand(this.ViewModel);

            // New TurnPageToNextCommand
            this.NextPageCommand = new TurnPageToNextCommand(this.ViewModel);

            // New TurnPageToPreviousCommand
            this.PreviousPageCommand = new TurnPageToPreviousCommand(this.ViewModel);

            // New SavePageAsFileCommand
            this.SavePageCommand = new SavePageAsFileCommand(this.ViewModel);

            // New SaveAllPagesCommand
            this.SaveAllPagesCommand = new SaveAllPagesCommand(this.ViewModel);

            // New ZoomInCommand
            this.ZoomInCommand = new ZoomInCommand(this.ViewModel, this);

            // New ZoomOutCommand
            this.ZoomOutCommand = new ZoomOutCommand(this.ViewModel, this);

            // Register Event(s) to Raise CanExecuteChanged Event of Zoom In/Out Command
            this.ScrollViewer.ViewChanged += (object sender, ScrollViewerViewChangedEventArgs e) => this.RaiseZoomInCommandCanExecuteChangedEvent();
            this.ScrollViewer.ViewChanged += (object sender, ScrollViewerViewChangedEventArgs e) => this.RaiseZoomOutCommandCanExecuteChangedEvent();
        }

        // for Data Binding
        public CoreDispatcher CoreDispatcher => this.Dispatcher;


        // ViewModel
        public ImageSourceViewModel ViewModel { get; private set; }

        // Open Command
        public OpenImageCommand OpenComamand { get; private set; }

        // Next Page Command
        public TurnPageToNextCommand NextPageCommand { get; private set; }

        // Previous Page Command
        public TurnPageToPreviousCommand PreviousPageCommand { get; private set; }

        // Save Page Command
        public SavePageAsFileCommand SavePageCommand { get; private set; }

        // Save All Pages Command
        public SaveAllPagesCommand SaveAllPagesCommand { get; private set; }

        // Zoom In Command
        public ZoomInCommand ZoomInCommand;

        // Zoom Out Command
        public ZoomOutCommand ZoomOutCommand;


        // Zoom Scale
        public readonly float ZoomScale = (float)0.25;

        // Can Zoom In
        public bool CanZoomIn => (this.ScrollViewer.ZoomFactor * (float)(1 + this.ZoomScale)) < this.ScrollViewer.MaxZoomFactor;

        // Can Zoom Out
        public bool CanZoomOut => (this.ScrollViewer.ZoomFactor * (float)(1 / (1 + this.ZoomScale))) > this.ScrollViewer.MinZoomFactor;

        // Zoom In/Out Method
        public void Zoom(float scale)
        {
            var horizontal_offset = this.ScrollViewer.HorizontalOffset;
            var vertical_offset = this.ScrollViewer.VerticalOffset;
            var viewport_width = this.ScrollViewer.ViewportWidth;
            var viewport_height = this.ScrollViewer.ViewportHeight;

            this.ScrollViewer.ChangeView(
                (horizontal_offset * scale) + ((viewport_width * scale - viewport_width) / 2),
                (vertical_offset * scale) + ((viewport_height * scale - viewport_height) / 2),
                this.ScrollViewer.ZoomFactor * scale);
        }

        // Delegate(s) to Raise CanExecutedChanged Event of Command
        public Action RaiseZoomInCommandCanExecuteChangedEvent;
        public Action RaiseZoomOutCommandCanExecuteChangedEvent;
    }
}
