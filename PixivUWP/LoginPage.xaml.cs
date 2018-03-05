//PixivUniversal
//Copyright(C) 2017 Pixeez Plus Project

//This program is free software; you can redistribute it and/or
//modify it under the terms of the GNU General Public License
//as published by the Free Software Foundation; version 2
//of the License.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
using PixivUWP.Data;
using PixivUWP.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上有介绍

namespace PixivUWP
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class LoginPage : Page
    {
        Storyboard storyboard = new Storyboard();

        public LoginPage()
        {
            this.InitializeComponent();
            if (AppDataHelper.GetValue("uname") != null)
                txt_UserName.Text = AppDataHelper.GetValue("uname") as string;
            if (AppDataHelper.GetValue("upasswd") != null)
                txt_Password.Password = AppDataHelper.GetValue("upasswd") as string;
            if (AppDataHelper.GetValue("isauto") != null)
                s_auto.IsChecked = Convert.ToBoolean(AppDataHelper.GetValue("isauto") as string);
            if (AppDataHelper.GetValue("isrem") != null)
                s_remember.IsChecked = Convert.ToBoolean(AppDataHelper.GetValue("isrem") as string);
            if (s_auto.IsChecked == true)
                beginLoading();
            var curView = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
            curView.SetPreferredMinSize(new Windows.Foundation.Size(500, 630));
        }

        private async Task logoAnimation()
        {
            //Perform the animations
            BindableMargin margin = new Views.BindableMargin(logoimage_animated);
            margin.Top = -315;
            DoubleAnimationUsingKeyFrames animation = new DoubleAnimationUsingKeyFrames();
            animation.EnableDependentAnimation = true;
            EasingDoubleKeyFrame f1 = new EasingDoubleKeyFrame();
            f1.Value = -315;
            f1.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.2));
            animation.KeyFrames.Add(f1);
            EasingDoubleKeyFrame f2 = new EasingDoubleKeyFrame();
            f2.EasingFunction = new PowerEase() { EasingMode = EasingMode.EaseInOut, Power = 4 };
            f2.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.8));
            f2.Value = 0;
            animation.KeyFrames.Add(f2);
            Storyboard.SetTarget(animation, margin);
            Storyboard.SetTargetProperty(animation, "Top");
            //Windows Phones do not need the animation
            //if (DeviceTypeHelper.GetDeviceFormFactorType() != DeviceFormFactorType.Phone)
            {
                storyboard.Children.Add(animation);
            }
            //Only phones should have this step
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                //var appview = ApplicationView.GetForCurrentView();
                //appview.SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);//不能这样做，这样做可能会导致无法适应虚拟导航栏
                var statusbar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
                statusbar.ForegroundColor = Colors.White;
                statusbar.BackgroundOpacity = 0;
                await statusbar.HideAsync();
            }
            storyboard.Completed += Storyboard_Completed;
            storyboard.Begin();
        }

        private void Storyboard_Completed(object sender, object e)
        {
            //Main animation finish
            BindableMargin margin2 = new Views.BindableMargin(logoimage_animated);
            logoimage_animated.Opacity = 100;
            margin2.Top = 0;
            Data.TmpData.Username = txt_UserName.Text;
            Data.TmpData.Password = txt_Password.Password;
            storeData();
            (Window.Current.Content as Frame).Navigate(typeof(LoadingPage));
        }


        private void storeData()
        {
            AppDataHelper.SetValue("uname", txt_UserName.Text);
            if (s_remember.IsChecked==true)
                AppDataHelper.SetValue("upasswd", txt_Password.Password);
            else
                AppDataHelper.SetValue("upasswd", "");
            AppDataHelper.SetValue("upasswd4tile", txt_Password.Password);
            AppDataHelper.SetValue("isrem", s_remember.IsChecked.ToString());
            AppDataHelper.SetValue("isauto", s_auto.IsChecked.ToString());
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (txt_UserName.Text == "" || txt_Password.Password == "")
            {
                new Controls.MyToast("Please enter your account and password！").Show();
            }
            else
            {
                beginLoading();
            }
        }

        private async void RegButton_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://accounts.pixiv.net/signup"));
        }

        private async void beginLoading()
        {
            logoimage_animated.Opacity = 100;
            controls.Visibility = Visibility.Collapsed;
            await logoAnimation();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var appTitleBar = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TitleBar;
            if ((Background as SolidColorBrush).Color == Colors.Black) Data.TmpData.islight = false;
            if (Data.TmpData.islight)
            {
                logoimage_animated.Source = new BitmapImage(new Uri("ms-appx:///Assets/SplashScreen.scale-200.png"));
                logoimage.Source = new BitmapImage(new Uri("ms-appx:///Assets/SplashScreen.scale-200.png"));
                appTitleBar.ButtonBackgroundColor = Colors.White;
                appTitleBar.ButtonForegroundColor = Colors.Black;
                appTitleBar.ButtonHoverBackgroundColor = Colors.LightGray;
                appTitleBar.ButtonHoverForegroundColor = Colors.Black;
                appTitleBar.ButtonInactiveBackgroundColor = Colors.White;
                appTitleBar.ButtonInactiveForegroundColor = Colors.Black;
                appTitleBar.ButtonPressedBackgroundColor = Colors.Gray;
                appTitleBar.ButtonPressedForegroundColor = Colors.Black;
            }
            else
            {
                logoimage_animated.Source = new BitmapImage(new Uri("ms-appx:///Assets/DarkSplashScreen.scale-200.png"));
                logoimage.Source = new BitmapImage(new Uri("ms-appx:///Assets/DarkSplashScreen.scale-200.png"));
                appTitleBar.ButtonBackgroundColor = Colors.Black;
                appTitleBar.ButtonForegroundColor = Colors.White;
                appTitleBar.ButtonHoverBackgroundColor = Colors.DarkGray;
                appTitleBar.ButtonHoverForegroundColor = Colors.White;
                appTitleBar.ButtonInactiveBackgroundColor = Colors.Black;
                appTitleBar.ButtonInactiveForegroundColor = Colors.White;
                appTitleBar.ButtonPressedBackgroundColor = Colors.Gray;
                appTitleBar.ButtonPressedForegroundColor = Colors.White;
            }
        }

        private void s_auto_Toggled(object sender, RoutedEventArgs e)
        {
            if((sender as CheckBox).IsChecked==true)
                s_remember.IsChecked = true;
        }

        private void s_remember_Toggled(object sender, RoutedEventArgs e)
        {
            if((sender as CheckBox).IsChecked!=true)
                s_auto.IsChecked = false;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (txt_UserName.Text != "")
                txt_Password.Focus(FocusState.Programmatic);
        }

        private void updatenetset_Click(object sender, RoutedEventArgs e)
        {

        }

        private void resetnetset_Click(object sander, RoutedEventArgs e)
        {

        }
    }
}
