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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Contacts;
using Windows.Storage.Streams;

namespace PixivUWP.Data
{
    //有关储存容器的部分引用自:http://www.cnblogs.com/tonge/p/4760217.html
    internal static class AppDataHelper
    {
        public const string RefreshTokenKey = "RefreshToken";
        static readonly byte[] HashSalt = new byte[] { 0x03, 0x0a, 0x08, 0x05, 0x0c, 0x0c };

        //获取应用内联系人列表
        private static async Task<ContactList> getContactListAsync()
        {
            var store = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite);
            if (store == null)
            {
#if DEBUG
                Debug.WriteLine("无法获取ContactStore");
                if (Debugger.IsAttached) Debugger.Break();
#endif
                return null;
            }
            var contactLists = await store.FindContactListsAsync();
            if (contactLists.Count == 0)
            {
#if DEBUG
                Debug.WriteLine("ContactLists无数据，创建新List");
#endif
                return await store.CreateContactListAsync("PixivUWP");
            }
            else return contactLists[0];
        }

        //获取应用内联系人注释列表
        private static async Task<ContactAnnotationList> getContactAnnotationListAsync()
        {
            var annotationStore = await ContactManager.RequestAnnotationStoreAsync(ContactAnnotationStoreAccessType.AppAnnotationsReadWrite);
            if (annotationStore == null)
            {
#if DEBUG
                Debug.WriteLine("无法获取ContactAnnotationStore");
                if (Debugger.IsAttached) Debugger.Break();
#endif
                return null;
            }
            var annotationLists = await annotationStore.FindAnnotationListsAsync();
            if (annotationLists.Count == 0)
            {
#if DEBUG
                Debug.WriteLine("ContactAnnotationLists无数据，创建新List");
#endif
                return await annotationStore.CreateAnnotationListAsync();
            }
            else return annotationLists[0];
        }

        //检查联系人是否在列表里
        public static async Task<bool> checkContactAsync(Contact contact)
        {
            var contactList = await getContactListAsync();
            try
            {
                if ((await contactList.GetContactFromRemoteIdAsync(contact.RemoteId)) == null)
                    return false;
                else
                    return true;
            }
            catch
            {
                return false;
            }
        }

        //添加联系人进列表
        public static async Task<bool> addContactAsync(Contact contact)
        {
            if (await checkContactAsync(contact)) return false;
            var contactList = await getContactListAsync();
            await contactList.SaveContactAsync(contact);
            await contactList.SaveAsync();
            ContactAnnotation contactAnnotation = new ContactAnnotation();
            contactAnnotation.ContactId = contact.Id;
            //contactAnnotation.ContactListId = contact.ContactListId;
            contactAnnotation.RemoteId = contact.RemoteId;
            contactAnnotation.ProviderProperties.Add("ContactPanelAppID", "18416PixeezPlusProject.PixivUWP_fsr1r9g7nfjfw!App");
            contactAnnotation.SupportedOperations = ContactAnnotationOperations.ContactProfile;
            var contactAnnotationList = await getContactAnnotationListAsync();
            var b = await contactAnnotationList.TrySaveAnnotationAsync(contactAnnotation);
            return true;
        }

        //从表中移除联系人
        public static async Task<bool> deleteContactAsync(Contact contact)
        {
            if (!(await checkContactAsync(contact))) return false;
            var contactList = await getContactListAsync();
            var realcontact = await contactList.GetContactFromRemoteIdAsync(contact.RemoteId);
            await contactList.DeleteContactAsync(realcontact);
            await contactList.SaveAsync();
            var contactAnnotationList = await getContactAnnotationListAsync();
            //await contactAnnotationList.DeleteAnnotationAsync(await contactAnnotationList.GetAnnotationAsync(realcontact.Id));
            return true;
        }

        //固定联系人
        public static async Task PinContact(Contact contact)
        {
            //前面应放置API版本检查代码，仅能实装于16299
            if (await checkContactAsync(contact)) return;
            await addContactAsync(contact);
            PinnedContactManager contactManager = PinnedContactManager.GetDefault();
            await contactManager.RequestPinContactAsync(contact, PinnedContactSurface.Taskbar);
        }

        public static async Task UnpinContact(Contact contact)
        {
            //前面应放置API版本检查代码，仅能实装于16299
            if (!(await checkContactAsync(contact))) return;
            PinnedContactManager contactManager = PinnedContactManager.GetDefault();
            var contactList = await getContactListAsync();
            await contactManager.RequestUnpinContactAsync(await contactList.GetContactFromRemoteIdAsync(contact.RemoteId), PinnedContactSurface.Taskbar);
            await deleteContactAsync(contact);
        }

        public static string GetDeviceId()
        {
            var easId = new Windows.Security.ExchangeActiveSyncProvisioning.EasClientDeviceInformation().Id;
            var engine = Windows.Security.Cryptography.Core.HashAlgorithmProvider.OpenAlgorithm(Windows.Security.Cryptography.Core.HashAlgorithmNames.Md5);
            byte[] by = easId.ToByteArray();
            using (System.IO.MemoryStream memoryStream = new System.IO.MemoryStream(HashSalt.Length+by.Length))
            {
                memoryStream.Write(by,0,by.Length);
                memoryStream.Write(HashSalt, 0, HashSalt.Length);
                return Windows.Security.Cryptography.CryptographicBuffer.EncodeToHexString(engine.HashData(System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.GetWindowsRuntimeBuffer(memoryStream)));
            }
        }
        #region 字段
        /// <summary>
        /// 获取应用的设置容器
        /// </summary>
        private static Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

        /// <summary>
        /// 获取独立存储文件
        /// </summary>
        private static Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
        #endregion

        #region Set应用设置(简单设置，容器中的设置)
        /// <summary>
        /// 简单设置
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void SetValue(string key, object value)
            => localSettings.Values[key] = value;

        /// <summary>
        /// 创建设置容器
        /// </summary>
        /// <param name="containerName"></param>
        /// <returns></returns>
        private static Windows.Storage.ApplicationDataContainer CreateContainer(string containerName)
            => localSettings.CreateContainer(containerName, Windows.Storage.ApplicationDataCreateDisposition.Always);

        /// <summary>
        /// 讲设置保存到设置容器
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void SetContainerValue(string containerName, string key, string value)
        {
            if (!localSettings.Containers.ContainsKey(containerName))
                CreateContainer(containerName);
            localSettings.Containers[containerName].Values[key] = value;
        }
        #endregion

        #region Get应用设置(简单设置，容器中的设置)
        /// <summary>
        /// 获取应用设置
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static object GetValue(string key)
            => localSettings.Values[key];

        /// <summary>
        /// 从设置容器中获取应用设置
        /// </summary>
        /// <returns></returns>
        public static object GetValueByContainer(string containerName, string key)
        {
            bool hasContainer = localSettings.Containers.ContainsKey(containerName);

            if (hasContainer)
            {
                return localSettings.Containers[containerName].Values.ContainsKey(key);
            }
            return null;
        }
        #endregion

        public static bool ContainKey(string key) => localSettings.Values.ContainsKey(key);

        #region Remove已完成的设置
        /// <summary>
        /// 删除简单设置或复合设置
        /// </summary>
        /// <param name="key"></param>
        public static void Remove(string key)
            => localSettings.Values.Remove(key);

        /// <summary>
        /// 删除设置容器
        /// </summary>
        public static void RemoveContainer(string containerName)
            => localSettings.DeleteContainer(containerName);

        #endregion
    }
}
