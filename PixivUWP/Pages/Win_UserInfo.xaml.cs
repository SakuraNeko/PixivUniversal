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
using PixivUWP.Data;
using PixivUWP.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Contacts;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Yinyue200.OperationDeferral;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace PixivUWP.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class Win_UserInfo : Windows.UI.Xaml.Controls.Page, DetailPage.IRefreshable,IBackable,IBackHandlable
    {
        public Win_UserInfo()
        {
            this.InitializeComponent();
            //list.LoadingMoreItems += List_LoadingMoreItems;
            //list.HasMoreItemsEvent += List_HasMoreItemsEvent;
            //list_fav.LoadingMoreItems += List_fav_LoadingMoreItems;
            //list_fav.HasMoreItemsEvent += List_fav_HasMoreItemsEvent;
            mdc.MasterListView = MasterListView;
            mdc_fav.MasterListView = MasterListView_fav;
        }

        public BackInfo GenerateBackInfo()
            => new BackInfo { list = this.list, param = new object[] { this.list_fav, this.nexturl, this.nexturl_fav, this.pix_user } };

        private async Task firstLoadAsync()
        {
            while (scrollRoot.ScrollableHeight - 500 <= 10)
                if (await loadAsync() == false)
                    return;
        }

        private async Task firstLoadAsync_fav()
        {
            while (scrollRoot_fav.ScrollableHeight - 500 <= 10)
                if (await loadAsync_fav() == false)
                    return;
        }

        string nexturl = null;
        string nexturl_fav = null;

        bool _isLoading_fav = false;
        private async Task<bool> loadAsync_fav()
        {
            if (_isLoading_fav) return true;
            Debug.WriteLine("loadAsync_fav() called.");
            _isLoading_fav = true;
            try
            {
                var root = nexturl_fav == null ? await Data.TmpData.CurrentAuth.Tokens.GetUserFavoriteWorksAsync(pix_user.Id.Value) : await Data.TmpData.CurrentAuth.Tokens.AccessNewApiAsync<Illusts>(nexturl_fav);
                nexturl_fav = root.next_url ?? string.Empty;
                foreach (var one in root.illusts)
                {
                    if (!list_fav.Contains(one, Data.WorkEqualityComparer.Default))
                        list_fav.Add(one);
                }
                _isLoading_fav = false;
                return true;
            }
            catch
            {
                _isLoading_fav = false;
                return false;
            }
        }

        bool _isLoading = false;
        private async Task<bool> loadAsync()
        {
            if (_isLoading) return true;
            Debug.WriteLine("loadAsync() called.");
            _isLoading = true;
            try
            {
                var root = nexturl == null ? await Data.TmpData.CurrentAuth.Tokens.GetUserWorksAsync(pix_user.Id.Value) : await Data.TmpData.CurrentAuth.Tokens.AccessNewApiAsync<Illusts>(nexturl);
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

        //private void List_fav_HasMoreItemsEvent(ItemViewList<Work> sender, PackageTuple.WriteableTuple<bool> args)
        //{
        //    args.Item1 = nexturl_fav != string.Empty;
        //}

        //private async void List_fav_LoadingMoreItems(ItemViewList<Work> sender, Tuple<OperationDeferral<uint>, uint> args)
        //{
        //    var nowcount = list.Count;
        //    try
        //    {
        //        var root = nexturl_fav == null ? await Data.TmpData.CurrentAuth.Tokens.GetUserFavoriteWorksAsync(pix_user.Id.Value) : await Data.TmpData.CurrentAuth.Tokens.AccessNewApiAsync<Illusts>(nexturl_fav);
        //        nexturl = root.next_url ?? string.Empty;
        //        foreach (var one in root.illusts)
        //        {
        //            if (!list.Contains(one, Data.WorkEqualityComparer.Default))
        //                list.Add(one);
        //        }
        //    }
        //    catch
        //    {
        //        nexturl = string.Empty;
        //    }
        //    finally
        //    {
        //        args.Item1.Complete((uint)(list.Count - nowcount));
        //    }
        //}

        //private async void List_LoadingMoreItems(ItemViewList<Work> sender, Tuple<OperationDeferral<uint>, uint> args)
        //{
        //    var nowcount = list.Count;
        //    try
        //    {
        //        var root = nexturl == null ? await Data.TmpData.CurrentAuth.Tokens.GetUserWorksAsync(pix_user.Id.Value) : await Data.TmpData.CurrentAuth.Tokens.AccessNewApiAsync<Illusts>(nexturl);
        //        nexturl = root.next_url ?? string.Empty;
        //        foreach (var one in root.illusts)
        //        {
        //            if (!list.Contains(one, Data.WorkEqualityComparer.Default))
        //                list.Add(one);
        //        }
        //    }
        //    catch
        //    {
        //        nexturl = string.Empty;
        //    }
        //    finally
        //    {
        //        args.Item1.Complete((uint)(list.Count - nowcount));
        //    }
        //}

        //private void List_HasMoreItemsEvent(ItemViewList<Work> sender, PackageTuple.WriteableTuple<bool> args)
        //{
        //    args.Item1= nexturl != string.Empty;
        //}

        private void MasterListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            (sender == MasterListView?mdc:mdc_fav).MasterListView_ItemClick(typeof(DetailPage.WorkDetailPage), e.ClickedItem);
        }


        UserBase pix_user;
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            Data.TmpData.menuItem.SelectedIndex = -1;
            Data.TmpData.menuBottomItem.SelectedIndex = -1;
            try
            {
                if ((bool)((object[])e.Parameter)[0])
                {
                    list = ((BackInfo)((object[])e.Parameter)[1]).list as ItemViewList<Work>;
                    list_fav = ((Object[])((BackInfo)((object[])e.Parameter)[1]).param)[0] as ItemViewList<Work>;
                    nexturl = ((Object[])((BackInfo)((object[])e.Parameter)[1]).param)[1] as string;
                    nexturl_fav = ((Object[])((BackInfo)((object[])e.Parameter)[1]).param)[2] as string;
                    pix_user = ((Object[])((BackInfo)((object[])e.Parameter)[1]).param)[3] as UserBase;
                }
                else
                {
                    list = new ItemViewList<Work>();
                    list_fav = new ItemViewList<Work>();
                    pix_user = e.Parameter as UserBase;
                }
            }
            catch (NullReferenceException)
            {
                Debug.WriteLine("NullException");
                list = new ItemViewList<Work>();
                list_fav = new ItemViewList<Work>();
                pix_user = e.Parameter as UserBase;
            }
            catch (InvalidCastException)
            {
                Debug.WriteLine("InvalidCastException");
                list = new ItemViewList<Work>();
                list_fav = new ItemViewList<Work>();
                pix_user = e.Parameter as UserBase;
            }
            finally
            {
                await RefreshAsync();
            }
        }

        string getinfostr(string value,string head)
        {
            if(value!=null)
            {
                return head + ":" + value + Environment.NewLine;
            }
            else
            {
                return string.Empty;
            }
        }

        public async Task RefreshAsync()
        {
            try
            {
                pivot.Title = pix_user.Name + (!string.IsNullOrEmpty(pix_user.Email) ? "(" + pix_user.Email + ")" : string.Empty);
                btn_Pin.IsEnabled = false;
                //string imgurl = pix_user.ProfileImageUrls.Px170x170 ?? pix_user.ProfileImageUrls.Px50x50 ?? pix_user.ProfileImageUrls.Px16x16;
                var newuserinfo = await Data.TmpData.CurrentAuth.Tokens.GetUserInfoAsync(pix_user.Id.Value.ToString());
                //if (imgurl==null)
                //{
                //    pix_user = ().Single();
                //    imgurl = pix_user.ProfileImageUrls.Px170x170;
                //}
                using (var res = await PixivUWP.Data.TmpData.CurrentAuth.Tokens.SendRequestToGetImageAsync(Pixeez.MethodType.GET, newuserinfo.user.profile_image_urls.Medium))
                {
                    var bitmap = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                    await bitmap.SetSourceAsync((await res.GetResponseStreamAsync()).AsRandomAccessStream());
                    userpro.ImageSource = bitmap;
                    contact.Id = "PIXIV!" + pix_user.Id.ToString();
                    contact.RemoteId = "PIXIV!" + pix_user.Id.ToString();
                    contact.Name = pix_user.Name;
                    contact.SourceDisplayPicture = RandomAccessStreamReference.CreateFromStream((await res.GetResponseStreamAsync()).AsRandomAccessStream());
                    if (pix_user.Email != null)
                    {
                        ContactEmail contactEmail = new ContactEmail();
                        contactEmail.Address = pix_user.Email;
                        contact.Emails.Add(contactEmail);
                    }
                    btn_Pin.IsChecked = await Data.AppDataHelper.checkContactAsync(contact);
                    pinned = btn_Pin.IsChecked.Value;
                    btn_Pin.IsEnabled = true;
                }
                if (newuserinfo.profile != null)
                {
                    userinfo.Text = string.Concat(getinfostr(newuserinfo.profile.birth, "BirthDate"),
    getinfostr(newuserinfo.profile.gender, "Gender"), getinfostr(newuserinfo.profile.webpage, "homepage"), getinfostr(newuserinfo.user.comment, "intro"),
    getinfostr(newuserinfo.profile.job, "job"), getinfostr(newuserinfo.profile.region, "loc"));
                }

                //TODO:国际化
                //            if (pix_user.Profile != null)
                //            {
                //                userinfo.Text = string.Concat(getinfostr(pix_user.Profile.BirthDate, "BirthDate"), getinfostr(pix_user.Profile.BloodType, "BloodType"),
                //getinfostr(pix_user.Profile.Gender, "Gender"), getinfostr(pix_user.Profile.Homepage, "homepage"), getinfostr(pix_user.Profile.Introduction, "intro"),
                //getinfostr(pix_user.Profile.Job, "job"), getinfostr(pix_user.Profile.Location, "loc"));
                //            }
                //            else
                //            {

                //            }
            }
            catch { }
        }
        ItemViewList<Work> list;
        ItemViewList<Work> list_fav;
        private void PivotItem_Loaded(object sender, RoutedEventArgs e)
        {
            //MasterListView.ItemsSource = await Data.TmpData.CurrentAuth.Tokens.GetUsersWorksAsync(pix_user.Id.Value);
        }

        private void PivotItem_Loaded_1(object sender, RoutedEventArgs e)
        {
            //MasterListView_fav.ItemsSource = await Data.TmpData.CurrentAuth.Tokens.GetUserFavoriteWorksAsync(pix_user.Id.Value);
        }

        public bool GoBack()
        {
            switch(pivot.SelectedIndex)
            {
                default:
                case 0:
                    return false;
                case 1:
                    return mdc.GoBack();
                case 2:
                    return mdc_fav.GoBack();
            }
        }

        double _originHeight_fav = 0;
        private void scrollRoot_fav_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (scrollRoot_fav.VerticalOffset == _originHeight_fav) return;
            _originHeight_fav = scrollRoot_fav.VerticalOffset;
            if (scrollRoot_fav.VerticalOffset <= scrollRoot_fav.ScrollableHeight - 500) return;
            var result = loadAsync_fav();
        }

        double _originHeight = 0;
        private void scrollRoot_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (scrollRoot.VerticalOffset == _originHeight) return;
            _originHeight = scrollRoot.VerticalOffset;
            if (scrollRoot.VerticalOffset <= scrollRoot.ScrollableHeight - 500) return;
            var result = loadAsync();
        }

        private void pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch(pivot.SelectedIndex)
            {
                case 1:
                    Data.TmpData.StopLoading();
                    MasterListView_fav.ItemsSource = null;
                    MasterListView.ItemsSource = list;
                    var result = firstLoadAsync();
                    break;
                case 2:
                    Data.TmpData.StopLoading();
                    MasterListView.ItemsSource = null;
                    MasterListView_fav.ItemsSource = list_fav;
                    var result_fav = firstLoadAsync_fav();
                    break;
            }
        }


        private bool pinned = false;
        Contact contact = new Contact();
        private void btn_Pin_Click(object sender, RoutedEventArgs e)
        {
            if (!pinned) Data.AppDataHelper.PinContact(contact);
            else Data.AppDataHelper.UnpinContact(contact);
        }
    }
}
