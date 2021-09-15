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
using System.Windows.Input;  // for ICommand

namespace BUILDLet.JbigImageViewer
{
    public class TurnPageToNextCommand : ICommand
    {
        private readonly ImageSourceViewModel view_model;

        // Constructor
        public TurnPageToNextCommand(ImageSourceViewModel viewModel)
        {
            // Set ViewModel
            this.view_model = viewModel;

            // Register Event(s)
            this.view_model.CurrentPageChanged += (object sender, EventArgs e) => this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        // CanExecuteChanged Event Handler
        public event EventHandler CanExecuteChanged;

        // CanExecute Method (= NextPageEnabled)
        public bool CanExecute(object parameter) => this.view_model.NextPageEnabled;

        // Execute Method: Increment Current Page
        public void Execute(object parameter) => this.view_model.CurrentPage++;
    }
}
