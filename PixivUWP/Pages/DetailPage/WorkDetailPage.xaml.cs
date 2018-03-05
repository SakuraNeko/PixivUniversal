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
using System.IO;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using PixivUWP.ViewModels;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上有介绍

namespace PixivUWP.Pages.DetailPage
{
    //用于渲染WebView
    public static class WebViewExtensions
    {
        public static async Task ResizeToContentAsync(this WebView webView)
        {
            var heightString = await webView.InvokeScriptAsync("eval", new[] { "document.body.scrollHeight.toString()" });
            int height;
            if (int.TryParse(heightString, out height))
            {
                webView.Height = height;
            }
        }
    }

    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class WorkDetailPage : Windows.UI.Xaml.Controls.Page, IRefreshable
    {
        string err1 = "";
        string err2 = "";
        Frame MainFrame;

        public WorkDetailPage()
        {
            this.InitializeComponent();
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
            err1 = loader.GetString("ErrorLoading");
            err2 = loader.GetString("Error");
            commentList.ItemsSource = commentItem;
        }
        Work Work;
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            Work = e.Parameter as Work;
            if (Work is IllustWork iw)
            {
                if (iw.meta_pages != null && iw.meta_pages.Length > 1)
                    watchbigimg.Visibility = Visibility.Visible;
            }
            else
            {
                Work = (await Data.TmpData.CurrentAuth.Tokens.GetWorksAsync(Work.Id.Value))[0];
            }
            await RefreshAsync();
        }

        private async void fs_Click(object sender, RoutedEventArgs e)
        {
            fs.IsEnabled = false;
            try
            {
                if (fs.IsChecked != true)
                {
                    await PixivUWP.Data.TmpData.CurrentAuth.Tokens.DeleteMyFavoriteWorksAsync(Work.Id.Value);
                }
                else
                {
                    await PixivUWP.Data.TmpData.CurrentAuth.Tokens.AddMyFavoriteWorksAsync(Work.Id.Value);
                }
                Work.SetBookMarkedValue(fs.IsChecked == true);
            }
            catch (Exception ex)
            {
                if (!(ex is NullReferenceException))
                    fs.IsChecked = !fs.IsChecked;
            }
            finally
            {
                fs.IsEnabled = true;
            }
        }

        public async Task RefreshAsync()
        {
            PixivUWP.ProgressBarVisualHelper.SetYFHelperVisibility(pro, true);
            gz.IsEnabled = false;
            try
            {
                title.Text = Work.Title;
                user.Text = Work.User.Name;
                siz.Text = Work.Width?.ToString() + "×" + Work.Height?.ToString();
                try
                {
                    tool.Text = new Converter.TagsToStr().Convert(Work.Tools, null, null, null).ToString();
                }
                catch { }
                fs.IsChecked = Work.IsBookMarked();
                string url;
                var profile = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();
                bool wwanok;
                if (profile.IsWwanConnectionProfile)
                {
                    switch (profile.WwanConnectionProfileDetails.GetCurrentDataClass())
                    {
                        case Windows.Networking.Connectivity.WwanDataClass.Cdma1xRtt:
                        case Windows.Networking.Connectivity.WwanDataClass.None:
                        case Windows.Networking.Connectivity.WwanDataClass.Gprs:
                        case Windows.Networking.Connectivity.WwanDataClass.Edge:
                            wwanok = false;
                            break;
                        default:
                        case Windows.Networking.Connectivity.WwanDataClass.Cdma1xEvdo:
                        case Windows.Networking.Connectivity.WwanDataClass.Cdma1xEvdoRevA:
                        case Windows.Networking.Connectivity.WwanDataClass.Cdma1xEvdv:
                        case Windows.Networking.Connectivity.WwanDataClass.Custom:
                        case Windows.Networking.Connectivity.WwanDataClass.CdmaUmb:
                        case Windows.Networking.Connectivity.WwanDataClass.Umts:
                        case Windows.Networking.Connectivity.WwanDataClass.Hsdpa:
                        case Windows.Networking.Connectivity.WwanDataClass.Hsupa:
                        case Windows.Networking.Connectivity.WwanDataClass.LteAdvanced:
                        case Windows.Networking.Connectivity.WwanDataClass.Cdma3xRtt:
                            wwanok = true;
                            break;
                    }
                }
                else
                {
                    wwanok = true;
                }
                var connectioncost = profile.GetConnectionCost();
                if (connectioncost.ApproachingDataLimit == false && connectioncost.OverDataLimit == false &&
                    (connectioncost.NetworkCostType == Windows.Networking.Connectivity.NetworkCostType.Unrestricted || connectioncost.NetworkCostType == Windows.Networking.Connectivity.NetworkCostType.Unknown)
                    && wwanok)
                {
                    url = Work.ImageUrls.Original ?? Work.ImageUrls.Large ?? Work.ImageUrls.Medium;
                }
                else
                {
                    url = Work is IllustWork ? Work.ImageUrls.Large : Work.ImageUrls.Medium;
                }
                string htmldoc = "<html><body><div style=\"word-wrap: break-word\">" + (Work.Caption ?? string.Empty) + "</div></body></html>";
                des.NavigationCompleted += async (sender, args) => await sender.ResizeToContentAsync();
                //des.SizeChanged += async (sender, args) => await ((WebView)sender).ResizeToContentAsync();(暂时撤销改修改以防止出现更为严重的界面显示问题)
                des.NavigateToString(htmldoc);
                time.Text = Work.GetCreatedDate().ToString()  /* + "(创建与更新时间：" + Work.CreatedTime.LocalDateTime.ToString() + "," + Work.ReuploadedTime.ToString() + ")"*/;
                //tags.Text = new Converter.TagsToStr().Convert(Work.Tags, null, null, null).ToString();
                Tags.ItemsSource = new Converter.TagsToTagList().Convert(Work.Tags, null, null, null);
                using (var stream = await Data.TmpData.CurrentAuth.Tokens.SendRequestAsync(Pixeez.MethodType.GET, url))
                {
                    var bitmap = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                    await bitmap.SetSourceAsync((await stream.GetResponseStreamAsync()).AsRandomAccessStream());
                    bigimg.Source = bitmap;
                }
                string avimg = Work.User.GetAvatarUrl();
                if (Work.User.is_followed.HasValue && avimg != null)
                    gz.IsChecked = Work.User.is_followed;
                else
                {
                    var user = await Data.TmpData.CurrentAuth.Tokens.GetUsersAsync(Work.User.Id.Value);
                    avimg = user[0].GetAvatarUrl() ?? avimg;
                    if (user[0].IsFollowing.HasValue)
                        gz.IsChecked = user[0].IsFollowing;
                    else
                        gz.Visibility = Visibility.Collapsed;
                }
                #region 获取头像
                loadAvatar();
                async void loadAvatar()
                {
                    try
                    {
                        var asyncres = await Data.TmpData.CurrentAuth.Tokens.SendRequestAsync(Pixeez.MethodType.GET, avimg, null);
                        var img = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                        await img.SetSourceAsync((await asyncres.GetResponseStreamAsync()).AsInputStream() as Windows.Storage.Streams.IRandomAccessStream);
                        userpro.ImageSource = img;
                    }
                    catch { }
                    var rescomm = loadComment();
                }
                #endregion
                if(Work is Pixeez.Objects.IllustWork newwork)
                {
                    userviewnum.Text = newwork.total_view.ToString();
                    userlikenum.Text = newwork.total_bookmarks.ToString();
                }
                gz.IsEnabled = true;
            }
            catch
            {
                await new Windows.UI.Popups.MessageDialog(err1).ShowAsync();
            }
            finally
            {
                PixivUWP.ProgressBarVisualHelper.SetYFHelperVisibility(pro, false);
            }
        }

        private async void gz_Click(object sender, RoutedEventArgs e)
        {
            gz.IsEnabled = false;
            try
            {
                if (gz.IsChecked == true)
                    await Data.TmpData.CurrentAuth.Tokens.AddFavouriteUser(Work.User.Id.Value);
                else
                    await Data.TmpData.CurrentAuth.Tokens.DeleteFavouriteUser(Work.User.Id.Value.ToString());
                Work.User.is_followed = gz.IsChecked;
            }
            catch
            {
                gz.IsChecked = !gz.IsChecked;
            }
            finally
            {
                gz.IsEnabled = true;
            }
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            downloadbutton.IsEnabled = false;
            try
            {
                await Data.DownloadManager.AddTaskAsync(Work.ImageUrls.Original ?? (Work as Pixeez.Objects.IllustWork)?.meta_single_page.OriginalImageUrl ?? Work.ImageUrls.Large, Work.Id + "_p0");
            }
            finally
            {
                downloadbutton.IsEnabled = true;
            }
        }

        private void Hyperlink_Click(Windows.UI.Xaml.Documents.Hyperlink sender, Windows.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            var tmpInfo = (MainFrame.Content as Data.IBackHandlable).GenerateBackInfo();
            Data.UniversalBackHandler.AddPage(MainFrame.Content.GetType(), tmpInfo);
            Data.TmpData.StopLoading();
            MainFrame.Navigate(typeof(Win_UserInfo), Work.User);
            //弹出该作者的信息
            //await CreateNewWindowAsync(Work.User.Id.Value.ToString(), typeof(Win_UserInfo), Work.User);

        }

        private async void watchbigimg_Click(object sender, RoutedEventArgs e)
        {
            await CreateNewWindowAsync("work" + Work.Id, typeof(DetailPage.Win_WorkImgs), Work);
        }

        private static async Task CreateNewWindowAsync(string id, Type page, object par)
        {
            await CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                //if (Data.TmpData.OpenedWindows.TryGetValue(id, out int value))
                //{
                //    await ApplicationViewSwitcher.TryShowAsStandaloneAsync(value);
                //}
                //else
                {
                    int newViewId = -1;

                    CoreApplicationView newView = CoreApplication.CreateNewView();

                    await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        var newWindow = Window.Current;
                        var newAppView = ApplicationView.GetForCurrentView();
                        var sysnm = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();
                        var frame = new Frame();
                        frame.Navigated += (s, e) =>
                        {
                            if (frame.Content is IBackable)
                                sysnm.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
                            else
                                sysnm.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
                        };
                        sysnm.BackRequested += (s, e) =>
                        {
                            if (frame.Content is IBackable iba)
                            {
                                if (!iba.GoBack())
                                {
                                    var a = Window.Current.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                                      {
                                          Window.Current.Close();
                                      });
                                }
                                else
                                {
                                    e.Handled = true;
                                }
                            }
                        };
                        newWindow.Closed += async (s, e) =>
                        {
                            Window.Current.Content = null;
                            await CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                Data.TmpData.OpenedWindows.Remove(id);
                            });
                        };
                        frame.Navigate(page, par);
                        newWindow.Content = frame;
                        newWindow.Activate();
                        newViewId = newAppView.Id;
                    });
                    Data.TmpData.OpenedWindows[id] = newViewId;
                    var viewShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId);

                }
            });
        }

        private void relate_Click(object sender, RoutedEventArgs e)
        {
            var tmpInfo = (MainFrame.Content as Data.IBackHandlable).GenerateBackInfo();
            Data.UniversalBackHandler.AddPage(MainFrame.Content.GetType(), tmpInfo);
            Data.TmpData.StopLoading();
            MainFrame.Navigate(typeof(Win_Related), Work);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var tags = inputbox.Text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            fs.IsEnabled = false;
            try
            {
                await PixivUWP.Data.TmpData.CurrentAuth.Tokens.AddMyFavoriteWorksAsync(Work.Id.Value, tags);
                fs.IsChecked = true;
                Work.SetBookMarkedValue(fs.IsChecked == true);
            }
            catch
            {
                new Controls.MyToast(err2).Show();
            }
            finally
            {
                fs.IsEnabled = true;
            }
        }

        private void fs_Loaded(object sender, RoutedEventArgs e)
        {
            bool allowFocusOnInteractionAvailable =
    Windows.Foundation.Metadata.ApiInformation.IsPropertyPresent(
        "Windows.UI.Xaml.FrameworkElement",
        "AllowFocusOnInteraction");

            if (allowFocusOnInteractionAvailable)
            {
                if (sender is FrameworkElement s)
                {
                    s.AllowFocusOnInteraction = true;
                }
            }
        }

        private async void inputbox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var one in (await Data.TmpData.CurrentAuth.Tokens.GetBookMarkedDetailAsync(Work.Id.Value)).bookmark_detail.tags)
                {
                    if (one.is_registered == true)
                    {
                        if (sb.Length != 0)
                        {
                            sb.Append(',');
                        }
                        sb.Append(one.Name);
                    }
                }
                tb.Text = sb.ToString();
            }
        }

        private void zoomin_Click(object sender, RoutedEventArgs e)
            => scalable.ZoomIn();

        private void zoomout_Click(object sender, RoutedEventArgs e)
            => scalable.ZoomOut();

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            MainFrame = Data.TmpData.mainFrame;
        }

        private void shared_button_Click(object sender, RoutedEventArgs e)
        {
            var dataTransferManager = Windows.ApplicationModel.DataTransfer.DataTransferManager.GetForCurrentView();
            Windows.Foundation.TypedEventHandler<Windows.ApplicationModel.DataTransfer.DataTransferManager, Windows.ApplicationModel.DataTransfer.DataRequestedEventArgs> act2 = null;
            Windows.Foundation.TypedEventHandler<Windows.ApplicationModel.DataTransfer.DataTransferManager, Windows.ApplicationModel.DataTransfer.DataRequestedEventArgs> act = (Windows.ApplicationModel.DataTransfer.DataTransferManager sender1, Windows.ApplicationModel.DataTransfer.DataRequestedEventArgs args) =>
            {
                Windows.ApplicationModel.DataTransfer.DataRequest request = args.Request;
                Controls.ShareHelper.GenPackage(request.Data, Controls.ShareHelper.ShareType.Link, new Uri($"https://www.pixiv.net/member_illust.php?mode=medium&illust_id={Work.Id}", UriKind.Absolute));
                args.Request.Data.Properties.Title = Work.Title;
                dataTransferManager.DataRequested -= act2;
            };
            act2 = act;
            dataTransferManager.DataRequested += act2;
            Windows.ApplicationModel.DataTransfer.DataTransferManager.ShowShareUI();
        }

        //防止显示的图片大小异常
        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            const int MarginDown = 110;
            var grid = (sender as Grid);
            if (grid.ActualHeight > MarginDown)
            {
                scalable.Height = grid.ActualHeight - MarginDown;
            }
            else
            {
                ;
            }
        }

        private ObservableCollection<CommentListItem> commentItem = new ObservableCollection<CommentListItem>();

        //加载评论
        private async Task loadComment(int offset = 0)
        {
            if (offset == 0)
                commentItem.Clear();
            var res = await Data.TmpData.CurrentAuth.Tokens.GetIllustComments(Work.Id.ToString(), offset.ToString(), true);
            foreach (var comm in res.comments)
            {
                CommentListItem item = new CommentListItem();
                item.Avatar = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                item.LeftMargin = "0";
                item.Comment = comm;
                await fillAvatar(item);
                commentItem.Add(item);
            }
            if (res.next_url != null)
            {
                var rescomm = loadComment(offset + 30);
            }
            else if (commentItem.Count == 0)
                txtComment.Text = "No comments";
        }

        //填充头像
        private async Task fillAvatar(CommentListItem item)
        {

            using (var stream = await Data.TmpData.CurrentAuth.Tokens.SendRequestToGetImageAsync(Pixeez.MethodType.GET, item.Comment.user.profile_image_urls.Medium))
            {
                await item.Avatar.SetSourceAsync((await stream.GetResponseStreamAsync()).AsRandomAccessStream());
            }
        }

        private void Ellipse_PointerReleased(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is CommentListItem)
            {
                var commentobj = ((CommentListItem)(((FrameworkElement)sender).DataContext)).Comment.user;
                var tmpInfo = (MainFrame.Content as Data.IBackHandlable).GenerateBackInfo();
                Data.UniversalBackHandler.AddPage(MainFrame.Content.GetType(), tmpInfo);
                Data.TmpData.StopLoading();
                MainFrame.Navigate(typeof(Win_UserInfo), commentobj);
            }
            else
                Hyperlink_Click(null, null);
        }

        private void tagbutton_Click(object sender, RoutedEventArgs e)
            => Data.TmpData.mainPage.Query(((Button)sender).Content as string);
    }
}
