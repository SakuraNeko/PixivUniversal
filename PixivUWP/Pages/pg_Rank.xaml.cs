﻿using Pixeez.Objects;
using PixivUWP.Data;
using PixivUWP.Pages.DetailPage;
using PixivUWP.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace PixivUWP.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class pg_Rank : Windows.UI.Xaml.Controls.Page, DetailPage.IRefreshable, IBackable, IBackHandlable
    {
        ItemViewList<Work> list;

        public pg_Rank()
        {
            this.InitializeComponent();
            //list.LoadingMoreItems += List_LoadingMoreItems;
            //list.HasMoreItemsEvent += List_HasMoreItemsEvent;
            MasterListView.ItemsSource = list;
            mdc.MasterListView = MasterListView;
        }

        int selectedindex = -1;

        public BackInfo GenerateBackInfo()
            => new BackInfo { list = this.list, param = new object[] { this.nowpage }, selectedIndex = MasterListView.SelectedIndex };

        private async Task firstLoadAsync()
        {
            while (scrollRoot.ScrollableHeight - 500 <= 10)
                if (await loadAsync() == false)
                    return;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                if ((bool)((object[])e.Parameter)[0])
                {
                    Data.TmpData.isBackTrigger = true;
                    Data.TmpData.menuItem.SelectedIndex = 1;
                    Data.TmpData.menuBottomItem.SelectedIndex = -1;
                    list = ((BackInfo)((object[])e.Parameter)[1]).list as ItemViewList<Work>;
                    nowpage = (int)(((Object[])((BackInfo)((object[])e.Parameter)[1]).param)[0]);
                    selectedindex = ((BackInfo)((object[])e.Parameter)[1]).selectedIndex;
                }
                else
                {
                    list = new ItemViewList<Work>();
                }
            }
            catch (NullReferenceException)
            {
                Debug.WriteLine("NullException");
                list = new ItemViewList<Work>();
            }
            catch (InvalidCastException)
            {
                Debug.WriteLine("InvalidCastException");
                list = new ItemViewList<Work>();
            }
            finally
            {
                MasterListView.ItemsSource = list;
                var result = firstLoadAsync();
                if (selectedindex != -1)
                {
                    MasterListView.SelectedIndex = selectedindex;
                    mdc.MasterListView_ItemClick(typeof(DetailPage.WorkDetailPage), MasterListView.Items[selectedindex]);
                }
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
                foreach (var rone in (await Data.TmpData.CurrentAuth.Tokens.GetRankingAllAsync("daily", nowpage, 30))[0].Works)
                {
                    var one = rone.Work;
                    if (!list.Contains(one, Data.WorkEqualityComparer.Default))
                        list.Add(one);
                }
                nowpage++;
                _isLoading = false;
                return true;
            }
            catch
            {
                _isLoading = false;
                return false;
            }
        }

        //private void List_HasMoreItemsEvent(ItemViewList<Work> sender, PackageTuple.WriteableTuple<bool> args)
        //{
        //    args.Item1 = !isfinish;
        //}

        int nowpage = 1;
        //bool isfinish = false;
        //private async void List_LoadingMoreItems(ItemViewList<Work> sender, Tuple<Yinyue200.OperationDeferral.OperationDeferral<uint>, uint> args)
        //{
        //    //var list1=await Data.TmpData.CurrentAuth.Tokens.GetRecommendedWorks();
        //    var nowcount = list.Count;
        //    try
        //    {
        //        foreach (var one in await Data.TmpData.CurrentAuth.Tokens.SearchWorksAsync(_query, nowpage, 30, "text", "all", "desc", _bypopular ? "popular" : "date"))
        //        {
        //            if (!list.Contains(one, Data.WorkEqualityComparer.Default))
        //                list.Add(one);
        //        }
        //        nowpage++;
        //    }
        //    catch
        //    {
        //        isfinish = true;
        //    }
        //    finally
        //    {
        //        args.Item1.Complete((uint)(list.Count - nowcount));
        //    }
        //}

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private async void MasterListView_ItemClickAsync(object sender, ItemClickEventArgs e)
        {
            mdc.MasterListView_ItemClick(typeof(DetailPage.WorkDetailPage), (await Data.TmpData.CurrentAuth.Tokens.GetWorksAsync(((NormalWork)e.ClickedItem).Id.Value))[0]);
        }

        public Task RefreshAsync()
        {
            list.Clear();
            MasterListView.ItemsSource = list;
            return ((IRefreshable)mdc).RefreshAsync();
        }

        public bool GoBack()
        {
            return ((IBackable)mdc).GoBack();
        }

        double _originHeight = 0;
        private void scrollRoot_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (scrollRoot.VerticalOffset == _originHeight) return;
            _originHeight = scrollRoot.VerticalOffset;
            if (scrollRoot.VerticalOffset <= scrollRoot.ScrollableHeight - 500) return;
            var result = loadAsync();
        }
    }
}
