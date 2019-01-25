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
using Windows.ApplicationModel.Background;
using Pixeez;
using Windows.UI.Notifications;
using System.IO;
using Windows.Storage;
using Microsoft.Toolkit.Uwp.Notifications;
using PixivUWP.Data;

namespace TileBackground
{
    public sealed class TileBackground : IBackgroundTask
    {
        public static void PassAuth(string Username,string Password)
        {
            AppDataHelper.SetValue("uname", Username);
            AppDataHelper.SetValue("upasswd", Password);
        }

        private static (bool isAuthed, string username, string password) getAuth()
            => (AppDataHelper.GetValue("uname") == null || AppDataHelper.GetValue("upasswd4tile") == null) ?
               (false, "", "") :
               (true, (string)AppDataHelper.GetValue("uname"), (string)AppDataHelper.GetValue("upasswd4tile"));

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            //return; //解决登录邮件问题之前的临时解决方案
            BackgroundTaskDeferral deferral = taskInstance?.GetDeferral();
            try
            {
                InternetConnectionHelper.ConnectionType connectionType = InternetConnectionHelper.GetConnectionType();
                if (connectionType == InternetConnectionHelper.ConnectionType.WWan)
                {
                    deferral?.Complete();
                    return;
                }
            }
            catch { }
            try
            {
                object numToday = AppDataHelper.GetValue("numtoday");
                int numtoday;
                if (numToday == null)
                    numtoday = 0;
                else
                    numtoday = (int)numToday;
                object dateToday = AppDataHelper.GetValue("dateToday");
                bool datetoday;
                if (dateToday == null)
                    datetoday = false;
                else if ((int)dateToday == DateTime.Now.Year * 10000 + DateTime.Now.Month * 100 + DateTime.Now.Day)
                    datetoday = true;
                else
                    datetoday = false;
                AppDataHelper.SetValue("datetoday", DateTime.Now.Year * 10000 + DateTime.Now.Month * 100 + DateTime.Now.Day);
                if (!datetoday)
                    numtoday = 1;
                else
                    numtoday++;
                if (numtoday > 20)
                    numtoday = 1;
                AppDataHelper.SetValue("numtoday", numtoday);
                (bool isAuthed, string username, string password) = getAuth();
                if (!isAuthed)
                {
                    deferral.Complete();
                    return;
                }
                Pixeez.AuthResult token;
                async System.Threading.Tasks.Task 正常加载tokenAsync()
                {
                    token = await Auth.AuthorizeAsync(username, password, null);
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
                            token = await Auth.AuthorizeAsync(username, password, token.Authorize.RefreshToken);
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
                AppDataHelper.SetValue(AppDataHelper.RefreshTokenKey, Newtonsoft.Json.JsonConvert.SerializeObject(token));
                var ranks = await token.Tokens.GetRankingAllAsync("daily", 1, 20);
                //更新磁贴
                var updater = TileUpdateManager.CreateTileUpdaterForApplication();
                updater.EnableNotificationQueue(false);
                //updater.Clear();
                using (var res = await token.Tokens.SendRequestToGetImageAsync(MethodType.GET, ranks[0].Works[numtoday - 1].Work.ImageUrls.Medium))
                {
                    StorageFolder applicationdatafolder = ApplicationData.Current.TemporaryFolder;
                    using (var stream = await res.GetResponseStreamAsync())
                    {
                        var file = await applicationdatafolder.CreateFileAsync("tmp" + numtoday.ToString() + ".jpg", CreationCollisionOption.ReplaceExisting);
                        using (var filestream = await file.OpenStreamForWriteAsync())
                        {
                            await stream.CopyToAsync(filestream);
                        }
                    }
                }
                TileBindingContentAdaptive content = new TileBindingContentAdaptive
                {
                    BackgroundImage = new TileBackgroundImage
                    {
                        Source = ApplicationData.Current.TemporaryFolder.Path + "\\tmp" + numtoday.ToString() + ".jpg"
                    }
                };
                TileBindingContentAdaptive content_m = new TileBindingContentAdaptive
                {
                    BackgroundImage = new TileBackgroundImage
                    {
                        Source = ApplicationData.Current.TemporaryFolder.Path + "\\tmp" + numtoday.ToString() + ".jpg"
                    }
                };
                AdaptiveSubgroup subgroupa = new AdaptiveSubgroup();
                subgroupa.Children.Add(new AdaptiveText() { Text = "Pixiv #" + numtoday, HintWrap = true });
                subgroupa.HintWeight = 3;
                AdaptiveSubgroup subgroupb = new AdaptiveSubgroup();
                subgroupb.Children.Add(new AdaptiveText() { Text = "By: " + ranks[0].Works[numtoday - 1].Work.User.Name, HintStyle = AdaptiveTextStyle.CaptionSubtle, HintWrap = true, HintAlign = AdaptiveTextAlign.Right });
                subgroupb.HintWeight = 7;
                AdaptiveGroup maingroup = new AdaptiveGroup();
                maingroup.Children.Add(subgroupa);
                maingroup.Children.Add(subgroupb);
                AdaptiveGroup maingroup_m = new AdaptiveGroup();
                maingroup_m.Children.Add(subgroupa);
                content.Children.Add(maingroup);
                content.Children.Add(new AdaptiveText() { Text = "★" + ranks[0].Works[numtoday - 1].Work.Stats.Score.ToString(), HintStyle = AdaptiveTextStyle.CaptionSubtle, HintWrap = true, HintAlign = AdaptiveTextAlign.Right });
                content_m.Children.Add(maingroup_m);
                TileBinding binding = new TileBinding
                {
                    Branding = TileBranding.Name,
                    DisplayName = ranks[0].Works[numtoday - 1].Work.Title,
                    Content = content
                };
                TileBinding binding_m = new TileBinding
                {
                    Branding = TileBranding.Name,
                    DisplayName = ranks[0].Works[numtoday - 1].Work.Title,
                    Content = content_m
                };
                TileContent tile = new TileContent
                {
                    Visual = new TileVisual
                    {
                        TileMedium = binding_m,
                        TileWide = binding,
                        TileLarge = binding
                    }
                };
                var notification = new TileNotification(tile.GetXml());
                updater.Update(notification);
            }
            catch
            { }
            finally
            {
                deferral?.Complete();
            }
        }
    }
}
