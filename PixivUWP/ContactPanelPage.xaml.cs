using Pixeez;
using Pixeez.Objects;
using PixivUWP.Data;
using PixivUWP.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Contacts;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace PixivUWP
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class ContactPanelPage : Windows.UI.Xaml.Controls.Page
    {
        public ContactPanelPage()
        {
            this.InitializeComponent();
        }

        AuthResult token;
        long id;
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                (bool isAuthed, string username, string password) getAuth()
                    => (AppDataHelper.GetValue("uname") == null || AppDataHelper.GetValue("upasswd4tile") == null) ?
                       (false, "", "") :
                       (true, (string)AppDataHelper.GetValue("uname"), (string)AppDataHelper.GetValue("upasswd4tile"));
                (bool isAuthed, string username, string password) = getAuth();
                if (isAuthed)
                {
                    var args = (ContactPanelActivatedEventArgs)e.Parameter;
                    ContactStore store = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite);
                    var lst = (await store.FindContactListsAsync())[0];
                    string trueid = (Convert.ToInt32(lst.Id.Split(',')[0]) + 1).ToString() + "," 
                        + lst.Id.Split(',')[1] + "," + args.Contact.Id.Split('.', '}')[2];
                    Contact contact = await (await store.FindContactListsAsync())[0].GetContactAsync(trueid);
                    id = Convert.ToInt64(contact.RemoteId.Split('!')[1]);
                    async System.Threading.Tasks.Task 正常加载tokenAsync()
                    {
                        token = await Auth.AuthorizeAsync(username, password, null, AppDataHelper.GetDeviceId());
                    }
                    token = AppDataHelper.ContainKey(AppDataHelper.RefreshTokenKey) ? Newtonsoft.Json.JsonConvert.DeserializeObject<Pixeez.AuthResult>(AppDataHelper.GetValue(AppDataHelper.RefreshTokenKey).ToString()) : default;
                    if (username == token.Key.Username && password == token.Key.Password)
                    {
                        //不使用密码认证
                        if (DateTime.UtcNow >= token.Key.KeyExpTime)
                        {
                            //token 已过期
                            try
                            {
                                token = await Auth.AuthorizeAsync(username, password, token.Authorize.RefreshToken, AppDataHelper.GetDeviceId());
                            }
                            catch
                            {
                                await 正常加载tokenAsync();
                            }
                        }
                    }
                    else
                    {
                        await 正常加载tokenAsync();
                    }
                    TmpData.CurrentAuth = token;
                    AppDataHelper.SetValue(AppDataHelper.RefreshTokenKey, Newtonsoft.Json.JsonConvert.SerializeObject(token));
                    logininfo.Visibility = Visibility.Collapsed;
                    viewer.Visibility = Visibility.Visible;
                    WorksListView.ItemsSource = list;
                    var result = firstLoadAsync();
                }
                else
                {
                    viewer.Visibility = Visibility.Collapsed;
                    logininfo.Visibility = Visibility.Collapsed;
                    info.Visibility = Visibility.Visible;
                }
            }
            catch
            {
                viewer.Visibility = Visibility.Collapsed;
                logininfo.Visibility = Visibility.Collapsed;
                info.Visibility = Visibility.Visible;
            }
        }

        private async Task firstLoadAsync()
        {
            while (viewer.ScrollableHeight - 500 <= 10)
                if (await loadAsync() == false)
                    return;
        }

        bool _isLoading = false;
        ItemViewList<Work> list = new ItemViewList<Work>();
        string nexturl = null;
        private async Task<bool> loadAsync()
        {
            if (_isLoading) return true;
            Debug.WriteLine("loadAsync() called.");
            _isLoading = true;
            try
            {
                var root = nexturl == null ? await token.Tokens.GetUserWorksAsync(id) : await token.Tokens.AccessNewApiAsync<Illusts>(nexturl);
                nexturl = root.next_url ?? string.Empty;
                foreach (var one in root.illusts)
                {
                    if (!list.Contains(one, Data.WorkEqualityComparer.Default))
                        list.Add(one);
                }
                _isLoading = false;
                return true;
            }
            catch
            {
                _isLoading = false;
                return false;
            }
        }

        double _originHeight = 0;
        private void viewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (viewer.VerticalOffset == _originHeight) return;
            _originHeight = viewer.VerticalOffset;
            if (viewer.VerticalOffset <= viewer.ScrollableHeight - 500) return;
            var result = loadAsync();
        }

        List<FrameworkElement> loaded = new List<FrameworkElement>();
        List<Task> loadingPics = new List<Task>();
        Queue<FrameworkElement> loadQueue = new Queue<FrameworkElement>();

        private void img_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (loaded.Contains(sender)) return;
            var img = sender as Image;
            img.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/BlankHolder.png"));
            if (((int?)Data.AppDataHelper.GetValue("LoadPolicy")) == 0)
            {
                var tmptask = LoadPictureAsync(sender);
                loadingPics.Add(tmptask);
                var tmpwaiter = tmptask.GetAwaiter();
                tmpwaiter.OnCompleted(() => loadingPics.Remove(tmptask));
            }
            else
            {
                loadQueue.Enqueue(sender);
                var tmptask = QueuedLoad();
                loadingPics.Add(tmptask);
                var tmpwaiter = tmptask.GetAwaiter();
                tmpwaiter.OnCompleted(() => loadingPics.Remove(tmptask));
            }
        }

        bool isQueuedLoading = false;

        private async Task QueuedLoad()
        {
            if (isQueuedLoading) return;
            isQueuedLoading = true;
            while (loadQueue.Count > 0)
            {
                var tmpSender = loadQueue.Dequeue();
                await LoadPictureAsync(tmpSender);
            }
            isQueuedLoading = false;
        }

        public static string geturlbypolicy(ImageUrls urls)
        {
            switch (Data.AppDataHelper.GetValue("PreviewImageSize"))
            {
                default:
                case 0:
                    return urls.Medium;
                case 1:
                    return urls.SquareMedium ?? urls.Small;
            }
        }

        public async Task LoadPictureAsync(FrameworkElement sender)
        {
            if (loaded.Contains(sender)) return;
            try
            {
                CoreDispatcher dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
                await dispatcher.RunAsync(CoreDispatcherPriority.Low,
                     () =>
                     { }
                     );
                if (sender.Parent is Panel pl)
                {
                    if (pl.FindName("pro") is TextBlock ring)
                    {
                        ring.Visibility = Visibility.Visible;
                        try
                        {
                            var img = sender as Image;
                            if (img.DataContext != null)
                            {
                                var work = (img.DataContext as Work);
                                using (var stream = await Data.TmpData.CurrentAuth.Tokens.SendRequestToGetImageAsync(Pixeez.MethodType.GET, geturlbypolicy(work.ImageUrls)))
                                {
                                    var bitmap = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                                    await bitmap.SetSourceAsync((await stream.GetResponseStreamAsync()).AsRandomAccessStream());
                                    img.Source = bitmap;
                                    loaded.Add(sender);
                                }
                            }
                        }
                        finally
                        {
                            ring.Visibility = Visibility.Collapsed;
                        }
                    }
                }
            }
            catch
            {

            }
        }
    }
}
