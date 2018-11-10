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
using Pixeez.Objects;
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
using System.Threading.Tasks;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace PixivUWP.Pages.DetailPage
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class Win_WorkImgs : Windows.UI.Xaml.Controls.Page
    {
        public Win_WorkImgs()
        {
            this.InitializeComponent();
        }
        IllustWork work;
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            work = e.Parameter as IllustWork;
            flipview.ItemsSource = work.meta_pages;
            flipview.SelectedIndex = 0;
        }
        Dictionary<Image, System.Threading.CancellationTokenSource> tokens = new Dictionary<Image, System.Threading.CancellationTokenSource>();
        private async void Image_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var page=args.NewValue as MetaPages;
            if(sender is Panel pl)
            {
                if(pl.FindName("img") is Image img)
                {
                    img.Source = null;
                    if (pl.FindName("pro") is ProgressRing pro)
                    {
                        var cancellationTokenSource = new System.Threading.CancellationTokenSource();
                        if (tokens.TryGetValue(img, out var token))
                        {
                            token.Cancel();
                            tokens.Remove(img);
                        }
                        tokens.Add(img, cancellationTokenSource);
                        ProgressBarVisualHelper.SetYFHelperVisibility(pro, true);
                        try
                        {
                            using (var stream = await Data.TmpData.CurrentAuth.Tokens.SendRequestAsync(Pixeez.MethodType.GET, page.ImageUrls.Original ?? page.ImageUrls.Large ?? 
                                page.ImageUrls.Medium,cancellationToken: cancellationTokenSource.Token))
                            {
                                var bitmap = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                                cancellationTokenSource.Token.ThrowIfCancellationRequested();
                                var stream2 = (await stream.GetResponseStreamAsync()).AsRandomAccessStream();
                                cancellationTokenSource.Token.ThrowIfCancellationRequested();
                                await bitmap.SetSourceAsync(stream2);
                                cancellationTokenSource.Token.ThrowIfCancellationRequested();
                                img.Source = bitmap;
                            }
                        }
                        catch(OperationCanceledException)
                        {

                        }
                        catch
                        {
                            new Controls.MyToast("有图片加载失败").Show();
                        }
                        finally
                        {
                            ProgressBarVisualHelper.SetYFHelperVisibility(pro, false);
                            tokens.Remove(img);
                        }
                    }
                }
                else
                {
                    //RoutedEventHandler reh= (se, ee) =>
                    //{
                    //    Image_DataContextChanged(sender, args);
                    //};
                    //pl.Loaded += reh;
                }

            }
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            if((sender as AppBarToggleButton).IsChecked==false)
            {
                Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().ExitFullScreenMode();
            }
            else
            {
                if (!Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TryEnterFullScreenMode())
                {
                    (sender as AppBarToggleButton).IsChecked = false;
                }
            }

        }

        private async void AppBarButton_Click_1(object sender, RoutedEventArgs e)
        {
            if(sender is AppBarButton downloadbutton&& flipview.SelectedValue is MetaPages sv)
            {
                downloadbutton.IsEnabled = false;
                try
                {
                    var filename = work.Id + "_p" + flipview.SelectedIndex.ToString();
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        await Data.DownloadManager.AddTaskAsync(sv.ImageUrls.Original ?? sv.ImageUrls.Large ?? sv.ImageUrls.Medium,filename);
                    });
                }
                finally
                {
                    downloadbutton.IsEnabled = true;
                }
            }
        }
    }
}
