/*
(c) Copyright Ascensio System SIA 2010-2014

This program is a free software product.
You can redistribute it and/or modify it under the terms 
of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of 
any third-party rights.

This program is distributed WITHOUT ANY WARRANTY; without even the implied warranty 
of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see 
the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html

You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.

The  interactive user interfaces in modified source and object code versions of the Program must 
display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
 
Pursuant to Section 7(b) of the License you must retain the original Product logo when 
distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under 
trademark law for use of our trademarks.
 
All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode
*/

using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Web;
using ASC.Files.Core;
using ASC.Web.Files.Services.WCFService.FileOperations;
using File = ASC.Files.Core.File;

namespace ASC.Web.Files.Services.WCFService
{
    [ServiceContract]
    public interface IFileStorageService
    {
        #region Folder Manager

        [OperationContract]
        Folder GetFolder(String folderId);

        [OperationContract]
        [WebGet(UriTemplate = UriTemplates.XMLGetSubFolders)]
        ItemList<Folder> GetFolders(String parentId);

        [OperationContract]
        [WebGet(UriTemplate = UriTemplates.XMLGetPath, ResponseFormat = WebMessageFormat.Json)]
        ItemList<object> GetPath(String folderId);

        [OperationContract]
        [WebGet(UriTemplate = UriTemplates.XMLCreateFolder)]
        Folder CreateNewFolder(String title, String parentId);

        [OperationContract]
        [WebGet(UriTemplate = UriTemplates.XMLRenameFolder)]
        Folder FolderRename(String folderId, String title);

        [OperationContract]
        [WebInvoke(UriTemplate = UriTemplates.XMLPostFolderItems, Method = "POST")]
        DataWrapper GetFolderItems(String parentId, String from, String count, String filter, OrderBy orderBy, String subjectID, String searchText);

        [OperationContract]
        [WebInvoke(UriTemplate = UriTemplates.JSONPostCheckMoveFiles, Method = "POST", ResponseFormat = WebMessageFormat.Json)]
        ItemDictionary<String, String> MoveOrCopyFilesCheck(ItemList<String> items, String destFolderId);

        [OperationContract]
        [WebInvoke(UriTemplate = UriTemplates.JSONPostMoveItems, Method = "POST", ResponseFormat = WebMessageFormat.Json)]
        ItemList<FileOperationResult> MoveOrCopyItems(ItemList<String> items, String destFolderId, String overwriteFiles, String isCopyOperation);

        [OperationContract]
        [WebInvoke(UriTemplate = UriTemplates.JSONPostDeleteItems, Method = "POST", ResponseFormat = WebMessageFormat.Json)]
        ItemList<FileOperationResult> DeleteItems(ItemList<String> items);

        ItemList<FileOperationResult> DeleteItems(ItemList<String> items, bool ignoreException);

        #endregion

        #region File Manager

        [OperationContract]
        [WebGet(UriTemplate = UriTemplates.XMLLastFileVersion)]
        File GetFile(String fileId, int version);

        [OperationContract]
        [WebGet(UriTemplate = UriTemplates.XMLCreateNewFile)]
        File CreateNewFile(String parentId, String fileTitle);

        [OperationContract]
        [WebGet(UriTemplate = UriTemplates.XMLRenameFile)]
        File FileRename(String fileId, String title);

        [OperationContract]
        [WebGet(UriTemplate = UriTemplates.JSONUpdateToVersion, ResponseFormat = WebMessageFormat.Json)]
        KeyValuePair<File, ItemList<File>> UpdateToVersion(String fileId, int version);

        [OperationContract]
        [WebGet(UriTemplate = UriTemplates.JSONCompleteVersion, ResponseFormat = WebMessageFormat.Json)]
        KeyValuePair<File, ItemList<File>> CompleteVersion(String fileId, int version, bool continueVersion);

        [OperationContract]
        [WebGet(UriTemplate = UriTemplates.JSONUpdateComment, ResponseFormat = WebMessageFormat.Json)]
        String UpdateComment(String fileId, int version, String comment);

        [OperationContract]
        [WebGet(UriTemplate = UriTemplates.XMLGetFileHistory)]
        ItemList<File> GetFileHistory(String fileId);

        [OperationContract]
        [WebInvoke(UriTemplate = UriTemplates.JSONPostGetSiblingsFile, Method = "POST", ResponseFormat = WebMessageFormat.Json)]
        KeyValuePair<String, ItemDictionary<String, String>> GetSiblingsFile(String fileId, String filter, OrderBy orderBy, String subjectID, String searchText);

        [OperationContract]
        [WebGet(UriTemplate = UriTemplates.JSONGetTrackEditFile, ResponseFormat = WebMessageFormat.Json)]
        KeyValuePair<bool, String> TrackEditFile(String fileId, Guid tabId, String docKeyForTrack, String shareLinkKey, bool isFinish, bool fixedVersion);

        [OperationContract]
        [WebInvoke(UriTemplate = UriTemplates.JSONPostCheckEditing, Method = "POST", ResponseFormat = WebMessageFormat.Json)]
        ItemDictionary<String, String> CheckEditing(ItemList<String> filesId);

        [OperationContract]
        [WebGet(UriTemplate = UriTemplates.JSONGetCanEdit, ResponseFormat = WebMessageFormat.Json)]
        Guid CanEdit(String fileId, String shareLinkKey);

        [OperationContract]
        [WebGet(UriTemplate = UriTemplates.JSONGetSaveEditing, ResponseFormat = WebMessageFormat.Json)]
        void SaveEditing(String fileId, int version, Guid tabId, string downloadUri, bool asNew, String shareLinkKey);

        [OperationContract]
        [WebGet(UriTemplate = UriTemplates.JSONGetStartEdit, ResponseFormat = WebMessageFormat.Json)]
        void StartEdit(String fileId, String docKeyForTrack, bool asNew, String shareLinkKey);

        [OperationContract]
        [WebInvoke(UriTemplate = UriTemplates.JSONPostCheckConversion, Method = "POST", ResponseFormat = WebMessageFormat.Json)]
        ItemList<FileOperationResult> CheckConversion(ItemList<ItemList<String>> filesIdVersion);

        [OperationContract]
        [WebGet(UriTemplate = UriTemplates.XMLGetLockFile)]
        File LockFile(String fileId, bool lockFile);

        #endregion

        #region Utils

        [OperationContract]
        [WebInvoke(UriTemplate = UriTemplates.JSONPostBulkDownload, Method = "POST", ResponseFormat = WebMessageFormat.Json)]
        ItemList<FileOperationResult> BulkDownload(ItemDictionary<String, String> items);

        [OperationContract]
        [WebGet(UriTemplate = UriTemplates.JSONGetTasksStatuses, ResponseFormat = WebMessageFormat.Json)]
        ItemList<FileOperationResult> GetTasksStatuses();

        [OperationContract]
        [WebGet(UriTemplate = UriTemplates.JSONEmptyTrash, ResponseFormat = WebMessageFormat.Json)]
        ItemList<FileOperationResult> EmptyTrash();

        [OperationContract]
        [WebGet(UriTemplate = UriTemplates.JSONTerminateTasks, ResponseFormat = WebMessageFormat.Json)]
        ItemList<FileOperationResult> TerminateTasks(bool import);

        [OperationContract]
        [WebGet(UriTemplate = UriTemplates.JSONGetShortenLink, ResponseFormat = WebMessageFormat.Json)]
        String GetShortenLink(String fileId);

        [OperationContract]
        [WebInvoke(UriTemplate = UriTemplates.XMLPostLinkToEmail, Method = "POST")]
        void SendLinkToEmail(String fileId, ItemDictionary<String, ItemList<String>> messageAddresses);

        [OperationContract]
        [WebGet(UriTemplate = UriTemplates.JSONGetStoreOriginal, ResponseFormat = WebMessageFormat.Json)]
        bool StoreOriginal(bool store);

        [OperationContract]
        [WebGet(UriTemplate = UriTemplates.JSONGetUpdateIfExist, ResponseFormat = WebMessageFormat.Json)]
        bool UpdateIfExist(bool update);

        [OperationContract]
        [WebGet(UriTemplate = UriTemplates.JSONGetHelpCenter, ResponseFormat = WebMessageFormat.Json)]
        String GetHelpCenter();

        #endregion

        #region Import

        [OperationContract]
        [WebInvoke(UriTemplate = UriTemplates.XMLPostGetImportDocs, Method = "POST")]
        ItemList<DataToImport> GetImportDocs(String source, AuthData authData);

        [OperationContract]
        [WebInvoke(UriTemplate = UriTemplates.JSONPostExecImportDocs, Method = "POST", ResponseFormat = WebMessageFormat.Json)]
        ItemList<FileOperationResult> ExecImportDocs(String login, String password, String token, String source, String parentId, bool ignoreCoincidenceFiles, List<DataToImport> dataToImport);

        #endregion

        #region Ace Manager

        [OperationContract]
        [WebInvoke(UriTemplate = UriTemplates.JSONPostSharedInfo, ResponseFormat = WebMessageFormat.Json)]
        ItemList<AceWrapper> GetSharedInfo(ItemList<String> objectId);

        [OperationContract]
        [WebGet(UriTemplate = UriTemplates.JSONGetSharedInfoShort, ResponseFormat = WebMessageFormat.Json)]
        ItemList<AceShortWrapper> GetSharedInfoShort(String objectId);

        [OperationContract]
        [WebInvoke(UriTemplate = UriTemplates.JSONPostSetAceObject, Method = "POST", ResponseFormat = WebMessageFormat.Json)]
        ItemList<String> SetAceObject(AceCollection aceCollection, bool notify);

        [OperationContract]
        [WebInvoke(UriTemplate = UriTemplates.JSONPostRemoveAce, Method = "POST", ResponseFormat = WebMessageFormat.Json)]
        void RemoveAce(ItemList<String> items);

        [OperationContract]
        [WebInvoke(UriTemplate = UriTemplates.JSONPostMarkAsRead, Method = "POST", ResponseFormat = WebMessageFormat.Json)]
        ItemList<FileOperationResult> MarkAsRead(ItemList<String> items);

        [OperationContract]
        [WebGet(UriTemplate = UriTemplates.JSONGetNewItems)]
        ItemList<FileEntry> GetNewItems(String folderId);

        #endregion

        #region ThirdParty

        ItemList<ThirdPartyParams> GetThirdParty();

        [OperationContract]
        [WebGet(UriTemplate = UriTemplates.JSONGetThirdParty, ResponseFormat = WebMessageFormat.Json)]
        ItemList<Folder> GetThirdPartyFolder();

        [OperationContract]
        [WebInvoke(UriTemplate = UriTemplates.JSONPostSaveThirdParty, Method = "POST", ResponseFormat = WebMessageFormat.Json)]
        Folder SaveThirdParty(ThirdPartyParams thirdPartyParams);

        [OperationContract]
        [WebGet(UriTemplate = UriTemplates.JSONDeleteThirdParty, ResponseFormat = WebMessageFormat.Json)]
        object DeleteThirdParty(String providerId);

        [OperationContract]
        [WebGet(UriTemplate = UriTemplates.JSONGetChangeAccessToThirdparty, ResponseFormat = WebMessageFormat.Json)]
        bool ChangeAccessToThirdparty(bool enableThirdpartySettings);

        #endregion
    }
}