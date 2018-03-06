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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.HockeyApp;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.ViewManagement;

namespace PixivUWP
{
    /// <summary>
    /// 提供特定于应用程序的行为，以补充默认的应用程序类。
    /// </summary>
    sealed partial class App : Application
    {
        Windows.UI.ViewManagement.ApplicationView mainview;
        /// <summary>
        /// 初始化单一实例应用程序对象。这是执行的创作代码的第一行，
        /// 已执行，逻辑上等同于 main() 或 WinMain()。
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            //UnhandledException += App_UnhandledException;
            Microsoft.HockeyApp.HockeyClient.Current.Configure("dd4a5cc7b28845c9804fc0cc29beb64b");//如果你创建了一个新的项目根据本项目，请删除这行代码
            try
            {
                switch ((int)Data.AppDataHelper.GetValue("软件主题"))
                {
                    case 0:
                        break;
                    case 1:
                        RequestedTheme = ApplicationTheme.Light;
                        break;
                    case 2:
                        RequestedTheme = ApplicationTheme.Dark;
                        break;
                }
            }
            catch
            {
                RequestedTheme = ApplicationTheme.Light;
            }
        }

        private async void CurrentView_Consolidated(Windows.UI.ViewManagement.ApplicationView sender, Windows.UI.ViewManagement.ApplicationViewConsolidatedEventArgs args)
        {
            if(Data.TmpData.OpenedWindows.Count>0)
            {
                await SaveDataAsync();
                Exit();
            }
        }

        override protected void OnActivated(IActivatedEventArgs e)
        {
            if (e.Kind == ActivationKind.ContactPanel)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                var rootFrame = new Frame();

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;

                // Navigate to the page that shows the Contact UI.
                rootFrame.Navigate(typeof(ContactPanelPage), e);

                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// 在应用程序由最终用户正常启动时进行调用。
        /// 将在启动应用程序以打开特定文件等情况下使用。
        /// </summary>
        /// <param name="e">有关启动请求和过程的详细信息。</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif
            if (e.Arguments == "") Data.TmpData.jumpList = "null";
            else Data.TmpData.jumpList = e.Arguments;
            if (mainview == null)
            {
                mainview = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
                mainview.Consolidated += CurrentView_Consolidated;
            }
            Frame rootFrame = Window.Current.Content as Frame;

            // 不要在窗口已包含内容时重复应用程序初始化，
            // 只需确保窗口处于活动状态
            if (rootFrame == null)
            {
                // 创建要充当导航上下文的框架，并导航到第一页
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: 从之前挂起的应用程序加载状态
                }

                // 将框架放在当前窗口中
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // 当导航堆栈尚未还原时，导航到第一页，
                    // 并通过将所需信息作为导航参数传入来配置
                    // 参数
                    rootFrame.Navigate(typeof(LoginPage));
                    args = e.Arguments;
                }
                // 确保当前窗口处于活动状态
                Window.Current.Activate();
            }
        }
        object args;

        //public (object args,Type page) getpagetonav(object args)
        //{
        //    switch (args)
        //    {
        //        case "viewcurrent":
        //        case "markcurrent":
        //        case "sharecurrent":
        //            var p=new Windows.UI.Popups.MessageDialog("该功能待开发").ShowAsync();
        //            break;
        //            case ""
        //    }
        //}


        /// <summary>
        /// 导航到特定页失败时调用
        /// </summary>
        ///<param name="sender">导航失败的框架</param>
        ///<param name="e">有关导航失败的详细信息</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// 在将要挂起应用程序执行时调用。  在不知道应用程序
        /// 无需知道应用程序会被终止还是会恢复，
        /// 并让内存内容保持不变。
        /// </summary>
        /// <param name="sender">挂起的请求的源。</param>
        /// <param name="e">有关挂起请求的详细信息。</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: 保存应用程序状态并停止任何后台活动
            await SaveDataAsync();
            deferral.Complete();
        }
        public Task SaveDataAsync()
        {
            return Task.CompletedTask;
        }
    }
}
