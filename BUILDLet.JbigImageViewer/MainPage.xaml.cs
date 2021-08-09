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
using Windows.UI.ViewManagement;        // for ApplicationView
using Windows.UI.Xaml.Media.Animation;  // for EntranceNavigationTransitionInfo

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x411 を参照してください

namespace BUILDLet.JbigImageViewer
{
    public sealed partial class MainPage : Page
    {
        // Constructor
        public MainPage()
        {
            this.InitializeComponent();
        }


        // List of Pages
        private readonly List<(string Tag, Type PageType)> Pages = new List<(string Tag, Type PageType)>
        {
            ("Home", typeof(HomePage))
        };


        // NavigationView.Loaded Event Handler
        private void NavigationView_Loaded(object sender, RoutedEventArgs e)
        {
            // SET Initial Window Size
            // ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
            // ApplicationView.PreferredLaunchViewSize = new Size(500, 350);

            // Add Event Handler for ContentFrame Navigation
            this.ContentFrame.Navigated += On_Navigated;

            // Set Initial SelectedItem of NavigationView
            this.NavigationView.SelectedItem = this.NavigationView.MenuItems[0];

            // Navigate (Initial)
            this.Navigate_To("Home", new EntranceNavigationTransitionInfo());
        }


        // NavigationView.Invoked Event Handler
        private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
            => this.Navigate_To(args.IsSettingsInvoked ? "Settings" : args.InvokedItemContainer?.Tag?.ToString(), args.RecommendedNavigationTransitionInfo);


        // NavigationView.SelectionChanged Event Handler
        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
            => this.Navigate_To(args.IsSettingsSelected ? "Settings" : args.SelectedItemContainer?.Tag?.ToString(), args.RecommendedNavigationTransitionInfo);


        // Navigate Frame by NavigationView
        private void Navigate_To(string tag, NavigationTransitionInfo transitionInfo)
        {
            Type pageType =
                tag == "Settings"
                ? typeof(SettingsPage)
                : this.Pages.FirstOrDefault(source => source.Tag.Equals(tag)).PageType;

            if (!(pageType is null) && !Type.Equals(this.ContentFrame.CurrentSourcePageType, pageType))
            {
                // Navigate
                this.ContentFrame.Navigate(pageType, null, transitionInfo);
            }
            else
            {
                // Only Hide NavigationView
                this.NavigationView.IsPaneOpen = false;
            }
        }


        // Frame.Navigated Event Handler
        private void On_Navigated(object sender, NavigationEventArgs e)
        {
            // Hide NavigationView
            this.NavigationView.IsPaneOpen = false;

            if (ContentFrame.SourcePageType == typeof(SettingsPage))
            {
                // Set SelectedItem to Settings Page;
                // SettingsItem is not part of NavigationView.MenuItems, and doesn't have a Tag.
                this.NavigationView.SelectedItem = this.NavigationView.SettingsItem;

                // Set Header
                // this.NavigationView.Header = "Settings";
            }
            else if (ContentFrame.SourcePageType != null)
            {
                // Set SelectedItem
                this.NavigationView.SelectedItem = this.NavigationView.MenuItems
                    .OfType<NavigationViewItem>()
                    .First(menuItem => menuItem.Tag.Equals(this.Pages.FirstOrDefault(page => page.PageType == e.SourcePageType).Tag));

                // Set Header
                // this.NavigationView.Header = ((NavigationViewItem)this.NavigationView.SelectedItem)?.Content?.ToString();
            }
        }
    }
}
