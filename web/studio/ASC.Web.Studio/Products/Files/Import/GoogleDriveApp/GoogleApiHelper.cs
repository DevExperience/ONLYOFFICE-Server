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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Web;
using System.Web.Configuration;
using ASC.Core;
using ASC.Core.Tenants;
using ASC.Core.Users;
using ASC.FederatedLogin;
using ASC.FederatedLogin.Helpers;
using ASC.Files.Core;
using ASC.Security.Cryptography;
using ASC.Thrdparty;
using ASC.Thrdparty.Configuration;
using ASC.Web.Core;
using ASC.Web.Core.Files;
using ASC.Web.Core.Users;
using ASC.Web.Files.Classes;
using ASC.Web.Files.Core;
using ASC.Web.Files.HttpHandlers;
using ASC.Web.Files.Resources;
using ASC.Web.Files.Services.DocumentService;
using ASC.Web.Studio.Core;
using ASC.Web.Studio.Core.Users;
using ASC.Web.Studio.Utility;
using Newtonsoft.Json.Linq;
using File = ASC.Files.Core.File;
using MimeMapping = ASC.Common.Web.MimeMapping;
using SecurityContext = ASC.Core.SecurityContext;

namespace ASC.Web.Files.GoogleDriveApp
{
    public class GoogleApiHelper
    {
        private const string GoogleUrlToken = "https://accounts.google.com/o/oauth2/token";
        private const string GoogleUrlUserInfo = "https://www.googleapis.com/oauth2/v1/userinfo?access_token={access_token}";
        private const string GoogleUrlFile = "https://www.googleapis.com/drive/v2/files/{fileId}";
        private const string GoogleUrlUpload = "https://www.googleapis.com/upload/drive/v2/files";

        private static readonly Dictionary<string, FileType> GoogleMimeTypes = new Dictionary<string, FileType>
            {
                { "application/vnd.google-apps.document", FileType.Document },
                { "application/vnd.google-apps.spreadsheet", FileType.Spreadsheet },
                { "application/vnd.google-apps.presentation", FileType.Presentation }
            };

        private static string ClientId
        {
            get { return KeyStorage.Get("googleDriveAppClientId"); }
        }

        private static string SecretKey
        {
            get { return KeyStorage.Get("googleDriveAppSecretKey"); }
        }

        private static string RedirectUrl
        {
            get { return KeyStorage.Get("googleDriveAppRedirectUrl"); }
        }


        internal static void RequestCode(HttpContext context)
        {
            var state = context.Request["state"];
            Global.Logger.Debug("GoogleDriveApp: state - " + state);
            if (string.IsNullOrEmpty(state))
            {
                Global.Logger.Info("GoogleDriveApp: empty state");
                throw new Exception("Empty state");
            }

            var token = GetToken(context.Request["code"]);
            if (token == null)
            {
                Global.Logger.Info("GoogleDriveApp: token is null");
                throw new SecurityException("Access token is null");
            }

            var stateJson = JObject.Parse(state);

            if (SecurityContext.IsAuthenticated)
            {
                Global.Logger.Debug("GoogleDriveApp: is authenticated");

                if (!CurrentUser(stateJson.Value<string>("userId")))
                {
                    Global.Logger.Debug("GoogleDriveApp: logout");
                    CookiesManager.ClearCookies(CookiesType.AuthKey);
                    SecurityContext.Logout();
                }
            }

            if (!SecurityContext.IsAuthenticated)
            {
                var userInfo = GetUserInfo(token);

                if (userInfo == null)
                {
                    Global.Logger.Error("GoogleDriveApp: UserInfo is null");
                    throw new Exception("Profile is null");
                }

                var cookiesKey = SecurityContext.AuthenticateMe(userInfo.ID);
                CookiesManager.SetCookies(CookiesType.AuthKey, cookiesKey);
            }

            Token.SaveToken(token);

            var action = stateJson.Value<string>("action");
            switch (action)
            {
                case "create":
                    var folderId = stateJson.Value<string>("folderId");

                    context.Response.Redirect(App.Location + "?" + FilesLinkUtility.FolderId + "=" + folderId, true);
                    return;
                case "open":
                    var idsArray = stateJson.Value<JArray>("ids") ?? stateJson.Value<JArray>("exportIds");
                    if (idsArray == null)
                    {
                        Global.Logger.Error("GoogleDriveApp: ids is empty");
                        throw new Exception("File id is null");
                    }
                    var fileId = idsArray.ToObject<List<string>>().FirstOrDefault();

                    var driveFile = GetDriveFile(fileId, token);
                    if (driveFile == null)
                    {
                        Global.Logger.Error("GoogleDriveApp: file is null");
                        throw new Exception("File not found");
                    }

                    var jsonFile = JObject.Parse(driveFile);
                    var ext = GetCorrectExt(jsonFile);
                    var mimeType = (jsonFile.Value<string>("mimeType") ?? "").ToLower();
                    if (FileUtility.ExtsMustConvert.Contains(ext)
                        || GoogleMimeTypes.Keys.Contains(mimeType))
                    {
                        Global.Logger.Debug("GoogleDriveApp: file must be converted");
                        if (FilesSettings.ConvertNotify)
                        {
                            context.Response.Redirect(App.Location + "?" + FilesLinkUtility.FileId + "=" + fileId, true);
                            return;
                        }

                        fileId = CreateConvertedFile(driveFile, token);
                    }

                    context.Response.Redirect(FilesLinkUtility.GetFileWebEditorUrl(fileId) + "&" + FilesLinkUtility.Action + "=app", true);
                    return;
            }
            Global.Logger.Error("GoogleDriveApp: Action not identified");
            throw new Exception("Action not identified");
        }

        internal static void StreamFile(HttpContext context)
        {
            try
            {
                var fileId = context.Request[FilesLinkUtility.FileId];
                var auth = context.Request[FilesLinkUtility.AuthKey];
                var userId = context.Request[CommonLinkUtility.ParamName_UserUserID];

                Global.Logger.Debug("GoogleDriveApp: get file stream " + fileId);

                int validateTimespan;
                int.TryParse(WebConfigurationManager.AppSettings["files.stream-url-minute"], out validateTimespan);
                if (validateTimespan <= 0) validateTimespan = 5;

                var validateResult = EmailValidationKeyProvider.ValidateEmailKey(fileId + userId, auth, TimeSpan.FromMinutes(validateTimespan));
                if (validateResult != EmailValidationKeyProvider.ValidationResult.Ok)
                {
                    var exc = new HttpException((int)HttpStatusCode.Forbidden, FilesCommonResource.ErrorMassage_SecurityException);

                    Global.Logger.Error(string.Format("GoogleDriveApp: {0} {1}: {2}", FilesLinkUtility.AuthKey, validateResult, context.Request.Url), exc);

                    throw exc;
                }

                var token = Token.GetToken(userId);
                var driveFile = GetDriveFile(fileId, token);

                var jsonFile = JObject.Parse(driveFile);

                var downloadUrl = jsonFile.Value<string>("downloadUrl");
                var contentLength = jsonFile.Value<string>("fileSize");

                if (string.IsNullOrEmpty(downloadUrl))
                {
                    Global.Logger.Error("GoogleDriveApp: downloadUrl is null");
                    throw new Exception("downloadUrl is null");
                }

                Global.Logger.Debug("GoogleDriveApp: get file stream  downloadUrl - " + downloadUrl);

                var request = WebRequest.Create(downloadUrl);
                request.Method = "GET";
                request.Headers.Add("Authorization", "Bearer " + token.AccessToken);

                using (var response = request.GetResponse())
                using (var stream = new ResponseStream(response))
                {
                    stream.StreamCopyTo(context.Response.OutputStream);

                    Global.Logger.Debug("GoogleDriveApp: get file stream  contentLength - " + contentLength);
                    context.Response.AddHeader("Content-Length", contentLength);
                }
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.Write(ex.Message);
                Global.Logger.Error("GoogleDriveApp: Error for: " + context.Request.Url, ex);
            }
            try
            {
                context.Response.Flush();
                context.Response.End();
            }
            catch (HttpException)
            {
            }
        }

        internal static void ConfirmConvertFile(HttpContext context)
        {
            var fileId = context.Request[FilesLinkUtility.FileId];
            Global.Logger.Debug("GoogleDriveApp: ConfirmConvertFile - " + fileId);

            var token = Token.GetToken();

            var driveFile = GetDriveFile(fileId, token);
            if (driveFile == null)
            {
                Global.Logger.Error("GoogleDriveApp: file is null");
                throw new Exception("File not found");
            }

            fileId = CreateConvertedFile(driveFile, token);

            context.Response.Redirect(FilesLinkUtility.GetFileWebEditorUrl(fileId) + "&" + FilesLinkUtility.Action + "=app", true);
        }

        internal static void CreateFile(HttpContext context)
        {
            var folderId = context.Request[FilesLinkUtility.FolderId];
            var fileName = context.Request[FilesLinkUtility.FileTitle];
            Global.Logger.Debug("GoogleDriveApp: CreateFile folderId - " + folderId + " fileName - " + fileName);

            var token = Token.GetToken();

            var culture = CoreContext.UserManager.GetUsers(SecurityContext.CurrentAccount.ID).GetCulture();
            var storeTemp = Global.GetStoreTemplate();

            var path = FileConstant.NewDocPath + culture.TwoLetterISOLanguageName + "/";
            if (!storeTemp.IsDirectory(path))
            {
                path = FileConstant.NewDocPath + "default/";
            }
            var ext = FileUtility.InternalExtension[FileUtility.GetFileTypeByFileName(fileName)];
            path += "new" + ext;
            fileName = FileUtility.ReplaceFileExtension(fileName, ext);

            string driveFile;
            using (var content = storeTemp.IronReadStream("", path, 10))
            {
                driveFile = CreateFile(content, fileName, folderId, token);
            }
            if (driveFile == null)
            {
                Global.Logger.Error("GoogleDriveApp: file is null");
                throw new Exception("File not created");
            }

            var jsonFile = JObject.Parse(driveFile);
            var fileId = jsonFile.Value<string>("id");

            context.Response.Redirect(FilesLinkUtility.GetFileWebEditorUrl(fileId) + "&" + FilesLinkUtility.Action + "=app" + "&new=true", true);
        }

        internal static Token RefreshToken(string refreshToken)
        {
            Global.Logger.Debug("GoogleDriveApp: refresh token");

            var query = String.Format("grant_type=refresh_token&client_id={0}&client_secret={1}&redirect_uri={2}&refresh_token={3}",
                                      ClientId, SecretKey, RedirectUrl, refreshToken);

            var json = PerformRequest(GoogleUrlToken, "application/x-www-form-urlencoded", "POST", query);
            return Token.FromJson(json);
        }

        internal static File GetFile(string fileId, out bool editable)
        {
            Global.Logger.Debug("GoogleDriveApp: get file " + fileId);
            var token = Token.GetToken();
            var driveFile = GetDriveFile(fileId, token);
            editable = false;

            if (driveFile == null) return null;

            var jsonFile = JObject.Parse(driveFile);

            var file = new File
                {
                    ID = jsonFile.Value<string>("id"),
                    Title = Global.ReplaceInvalidCharsAndTruncate(GetCorrectTitle(jsonFile)),
                    CreateOn = TenantUtil.DateTimeFromUtc(jsonFile.Value<DateTime>("createdDate")),
                    ModifiedOn = TenantUtil.DateTimeFromUtc(jsonFile.Value<DateTime>("modifiedDate")),
                    ContentLength = Convert.ToInt64(jsonFile.Value<string>("fileSize")),
                    ModifiedByString = jsonFile.Value<string>("lastModifyingUserName"),
                    ProviderKey = "Google"
                };

            var owners = jsonFile.Value<JArray>("ownerNames");
            if (owners != null)
            {
                file.CreateByString = owners.ToObject<List<string>>().FirstOrDefault();
            }

            editable = jsonFile.Value<bool>("editable");
            return file;
        }

        internal static string GetFileStreamUrl(File file)
        {
            if (file == null) return string.Empty;

            Global.Logger.Debug("GoogleDriveApp: get file stream url " + file.ID);

            var uriBuilder = new UriBuilder(CommonLinkUtility.GetFullAbsolutePath(ThirdPartyAppHandler.HandlerPath));
            if (uriBuilder.Uri.IsLoopback)
            {
                uriBuilder.Host = Dns.GetHostName();
            }
            var query = uriBuilder.Query;
            query += FilesLinkUtility.Action + "=stream&";
            query += FilesLinkUtility.FileId + "=" + HttpUtility.UrlEncode(file.ID.ToString()) + "&";
            query += CommonLinkUtility.ParamName_UserUserID + "=" + HttpUtility.UrlEncode(SecurityContext.CurrentAccount.ID.ToString()) + "&";
            query += FilesLinkUtility.AuthKey + "=" + EmailValidationKeyProvider.GetEmailKey(file.ID.ToString() + SecurityContext.CurrentAccount.ID);

            return uriBuilder.Uri + "?" + query;
        }

        internal static void SaveFile(string fileId, string downloadUrl)
        {
            Global.Logger.Debug("GoogleDriveApp: save file stream " + fileId + " from - " + downloadUrl);

            var token = Token.GetToken();

            var driveFile = GetDriveFile(fileId, token);
            if (driveFile == null)
            {
                Global.Logger.Error("GoogleDriveApp: file is null");
                throw new Exception("File not found");
            }

            var jsonFile = JObject.Parse(driveFile);
            var curExt = GetCorrectExt(jsonFile);
            var newExt = FileUtility.GetFileExtension(downloadUrl);
            if (curExt != newExt)
            {
                try
                {
                    Global.Logger.Debug("GoogleDriveApp: GetConvertedUri from " + newExt + " to " + curExt + " - " + downloadUrl);

                    var key = DocumentServiceConnector.GenerateRevisionId(downloadUrl);
                    DocumentServiceConnector.GetConvertedUri(downloadUrl, newExt, curExt, key, false, out downloadUrl);
                }
                catch (Exception e)
                {
                    Global.Logger.Error("GoogleDriveApp: Error convert", e);
                }
            }

            var downloadRequest = WebRequest.Create(downloadUrl);

            using (var downloadResponse = downloadRequest.GetResponse())
            using (var downloadStream = new ResponseStream(downloadResponse))
            {
                var request = (HttpWebRequest)WebRequest.Create(GoogleUrlUpload + "/{fileId}?uploadType=media".Replace("{fileId}", fileId));
                request.Method = "PUT";
                request.Headers.Add("Authorization", "Bearer " + token.AccessToken);
                request.ContentType = downloadResponse.ContentType;
                request.ContentLength = downloadResponse.ContentLength;

                const int bufferSize = 2048;
                var buffer = new byte[bufferSize];
                int readed;
                while ((readed = downloadStream.Read(buffer, 0, bufferSize)) > 0)
                {
                    request.GetRequestStream().Write(buffer, 0, readed);
                }

                try
                {
                    using (var response = request.GetResponse())
                    using (var stream = response.GetResponseStream())
                    {
                        var result = stream != null ? new StreamReader(stream).ReadToEnd() : null;

                        Global.Logger.Debug("GoogleDriveApp: save file stream response - " + result);
                    }
                }
                catch (WebException e)
                {
                    Global.Logger.Error("GoogleDriveApp: Error save file stream", e);
                    request.Abort();
                    var httpResponse = (HttpWebResponse)e.Response;
                    if (httpResponse.StatusCode == HttpStatusCode.Forbidden)
                    {
                        throw new SecurityException(FilesCommonResource.ErrorMassage_SecurityException, e);
                    }
                    throw;
                }
            }
        }


        private static Token GetToken(string code)
        {
            var data = string.Format("code={0}&client_id={1}&client_secret={2}&redirect_uri={3}&grant_type=authorization_code",
                                     HttpUtility.UrlEncode(code),
                                     HttpUtility.UrlEncode(ClientId),
                                     HttpUtility.UrlEncode(SecretKey),
                                     RedirectUrl);

            var resultResponse = PerformRequest(GoogleUrlToken, "application/x-www-form-urlencoded", "POST", data);
            Global.Logger.Debug("GoogleDriveApp: token response - " + resultResponse);

            return Token.FromJson(resultResponse);
        }

        private static bool CurrentUser(string googleId)
        {
            var accounts = new AccountLinker("webstudio")
                .GetLinkedObjectsByHashId(HashHelper.MD5(string.Format("{0}/{1}", ProviderConstants.OpenId, googleId)));
            return
                accounts.Select(x =>
                                    {
                                        try
                                        {
                                            return new Guid(x);
                                        }
                                        catch
                                        {
                                            return Guid.Empty;
                                        }
                                    })
                        .Any(account => account == SecurityContext.CurrentAccount.ID);

        }

        private static UserInfo GetUserInfo(Token token)
        {
            if (token == null)
            {
                Global.Logger.Info("GoogleDriveApp: token is null");
                throw new SecurityException("Access token is null");
            }

            var resultResponse = PerformRequest(GoogleUrlUserInfo.Replace("{access_token}", token.AccessToken));
            Global.Logger.Debug("GoogleDriveApp: userinfo response - " + resultResponse);

            var googleUserInfo = JObject.Parse(resultResponse);
            if (googleUserInfo == null)
            {
                Global.Logger.Error("Error in userinfo request");
                return null;
            }

            var email = googleUserInfo.Value<string>("email");
            var userInfo = CoreContext.UserManager.GetUserByEmail(email);
            if (!Equals(userInfo, Constants.LostUser))
            {
                return userInfo;
            }


            userInfo = new UserInfo
                {
                    Status = EmployeeStatus.Active,
                    FirstName = googleUserInfo.Value<string>("given_name"),
                    LastName = googleUserInfo.Value<string>("family_name"),
                    Email = email,
                    Title = string.Empty,
                    Location = string.Empty,
                    WorkFromDate = TenantUtil.DateTimeNow(),
                };

            var cultureName = googleUserInfo.Value<string>("locale") ?? CultureInfo.CurrentUICulture.Name;
            var cultureInfo = SetupInfo.EnabledCultures.Find(c => String.Equals(c.TwoLetterISOLanguageName, cultureName, StringComparison.InvariantCultureIgnoreCase));
            if (cultureInfo != null)
            {
                userInfo.CultureName = cultureInfo.Name;
            }

            if (string.IsNullOrEmpty(userInfo.FirstName))
            {
                userInfo.FirstName = FilesCommonResource.UnknownFirstName;
            }
            if (string.IsNullOrEmpty(userInfo.LastName))
            {
                userInfo.LastName = FilesCommonResource.UnknownLastName;
            }

            var pwd = UserManagerWrapper.GeneratePassword();

            UserInfo newUserInfo;
            try
            {
                SecurityContext.AuthenticateMe(ASC.Core.Configuration.Constants.CoreSystem);
                newUserInfo = UserManagerWrapper.AddUser(userInfo, pwd);
            }
            finally
            {
                SecurityContext.Logout();
            }

            var linker = new AccountLinker("webstudio");
            linker.AddLink(newUserInfo.ID.ToString(), googleUserInfo.Value<string>("id") ?? "", ProviderConstants.OpenId);

            UserHelpTourHelper.IsNewUser = true;
            PersonalSettings.IsNewUser = true;

            Global.Logger.Debug("GoogleDriveApp: new user " + newUserInfo.ID);
            return newUserInfo;
        }

        private static string GetDriveFile(string googleFileId, Token token)
        {
            if (token == null)
            {
                Global.Logger.Info("GoogleDriveApp: token is null");
                throw new SecurityException("Access token is null");
            }

            var resultResponse = PerformRequest(GoogleUrlFile.Replace("{fileId}", googleFileId), headers: new Dictionary<string, string> { { "Authorization", "Bearer " + token.AccessToken } });
            Global.Logger.Debug("GoogleDriveApp: file response - " + resultResponse);
            return resultResponse;
        }

        private static string CreateFile(string contentUrl, string fileName, string folderId, Token token)
        {
            if (string.IsNullOrEmpty(contentUrl))
            {
                Global.Logger.Error("GoogleDriveApp: downloadUrl is null");
                throw new Exception("downloadUrl is null");
            }
            Global.Logger.Debug("GoogleDriveApp: create from - " + contentUrl);

            var request = WebRequest.Create(contentUrl);

            using (var response = request.GetResponse())
            using (var content = new ResponseStream(response))
            {
                return CreateFile(content, fileName, folderId, token);
            }
        }

        private static string CreateFile(Stream content, string fileName, string folderId, Token token)
        {
            Global.Logger.Debug("GoogleDriveApp: create file");

            var request = (HttpWebRequest)WebRequest.Create(GoogleUrlUpload + "?uploadType=multipart");

            using (var tmpStream = new MemoryStream())
            {
                var boundary = DateTime.UtcNow.Ticks.ToString("x");

                var folderdata = string.IsNullOrEmpty(folderId) ? "" : string.Format(",\"parents\":[{{\"id\":\"{0}\"}}]", folderId);
                var metadata = string.Format("{{\"title\":\"{0}\"{1}}}", fileName, folderdata);
                var metadataPart = string.Format("\r\n--{0}\r\nContent-Type: application/json; charset=UTF-8\r\n\r\n{1}", boundary, metadata);
                Global.Logger.Debug("GoogleDriveApp: create file metadataPart - " + metadataPart);

                var bytes = Encoding.UTF8.GetBytes(metadataPart);
                tmpStream.Write(bytes, 0, bytes.Length);

                var mediaPartStart = string.Format("\r\n--{0}\r\nContent-Type: {1}\r\n\r\n", boundary, MimeMapping.GetMimeMapping(fileName));
                Global.Logger.Debug("GoogleDriveApp: create file mediaPartStart - " + mediaPartStart);

                bytes = Encoding.UTF8.GetBytes(mediaPartStart);
                tmpStream.Write(bytes, 0, bytes.Length);

                content.CopyTo(tmpStream);

                var mediaPartEnd = string.Format("\r\n--{0}--\r\n", boundary);
                Global.Logger.Debug("GoogleDriveApp: create file mediaPartEnd - " + mediaPartEnd);

                bytes = Encoding.UTF8.GetBytes(mediaPartEnd);
                tmpStream.Write(bytes, 0, bytes.Length);

                request.Method = "POST";
                request.Headers.Add("Authorization", "Bearer " + token.AccessToken);
                request.ContentType = "multipart/related; boundary=" + boundary;
                request.ContentLength = tmpStream.Length;
                Global.Logger.Debug("GoogleDriveApp: create file totalSize - " + tmpStream.Length);

                const int bufferSize = 2048;
                var buffer = new byte[bufferSize];
                int readed;
                tmpStream.Seek(0, SeekOrigin.Begin);
                while ((readed = tmpStream.Read(buffer, 0, bufferSize)) > 0)
                {
                    request.GetRequestStream().Write(buffer, 0, readed);
                }
            }

            try
            {
                using (var response = request.GetResponse())
                using (var stream = response.GetResponseStream())
                {
                    var result = stream != null ? new StreamReader(stream).ReadToEnd() : null;

                    Global.Logger.Debug("GoogleDriveApp: create file response - " + result);
                    return result;
                }
            }
            catch (WebException e)
            {
                Global.Logger.Error("GoogleDriveApp: Error create file", e);
                request.Abort();

                var httpResponse = (HttpWebResponse)e.Response;
                if (httpResponse.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new SecurityException(FilesCommonResource.ErrorMassage_SecurityException, e);
                }
            }
            return null;
        }

        private static string ConvertFile(string downloadUrl, string fromExt, Token token)
        {
            Global.Logger.Debug("GoogleDriveApp: convert file");

            if (string.IsNullOrEmpty(downloadUrl))
            {
                Global.Logger.Error("GoogleDriveApp: downloadUrl is null");
                throw new Exception("downloadUrl is null");
            }

            var request = WebRequest.Create(downloadUrl);
            request.Method = "GET";
            request.Headers.Add("Authorization", "Bearer " + token.AccessToken);

            try
            {
                using (var response = request.GetResponse())
                using (var fileStream = new ResponseStream(response))
                {
                    Global.Logger.Debug("GoogleDriveApp: GetExternalUri - " + downloadUrl);

                    var key = DocumentServiceConnector.GenerateRevisionId(downloadUrl);
                    downloadUrl = DocumentServiceConnector.GetExternalUri(fileStream, response.ContentType, key);
                }
            }
            catch (WebException e)
            {
                Global.Logger.Error("GoogleDriveApp: Error GetExternalUri", e);
                request.Abort();
            }

            var toExt = FileUtility.GetInternalExtension(fromExt);
            try
            {
                Global.Logger.Debug("GoogleDriveApp: GetConvertedUri- " + downloadUrl);

                var key = DocumentServiceConnector.GenerateRevisionId(downloadUrl);
                DocumentServiceConnector.GetConvertedUri(downloadUrl, fromExt, toExt, key, false, out downloadUrl);
            }
            catch (Exception e)
            {
                Global.Logger.Error("GoogleDriveApp: Error GetConvertedUri", e);
            }

            return downloadUrl;
        }

        private static string CreateConvertedFile(string driveFile, Token token)
        {
            var jsonFile = JObject.Parse(driveFile);
            var fileName = GetCorrectTitle(jsonFile);
            fileName = FileUtility.ReplaceFileExtension(fileName, FileUtility.GetInternalExtension(fileName));

            var folderId = (string)jsonFile.SelectToken("parents[0].id");

            Global.Logger.Info("GoogleDriveApp: create copy - " + fileName);

            var mimeType = (jsonFile.Value<string>("mimeType") ?? "").ToLower();
            FileType fileType;
            if (GoogleMimeTypes.TryGetValue(mimeType, out fileType))
            {
                var links = jsonFile["exportLinks"];
                if (links == null)
                {
                    Global.Logger.Error("GoogleDriveApp: exportLinks is null");
                    throw new Exception("exportLinks is null");
                }

                var internalExt = FileUtility.InternalExtension[fileType];
                var requiredMimeType = MimeMapping.GetMimeMapping(internalExt);

                var exportLinks = links.ToObject<Dictionary<string, string>>();
                var downloadUrl = exportLinks[requiredMimeType] ?? "";

                if (string.IsNullOrEmpty(downloadUrl))
                {
                    Global.Logger.Error("GoogleDriveApp: exportLinks without requested mime - " + links);
                    throw new Exception("exportLinks without requested mime");
                }


                var request = WebRequest.Create(downloadUrl);
                request.Method = "GET";
                request.Headers.Add("Authorization", "Bearer " + token.AccessToken);

                try
                {
                    using (var response = request.GetResponse())
                    using (var fileStream = new ResponseStream(response))
                    {
                        Global.Logger.Debug("GoogleDriveApp: download exportLink - " + downloadUrl);

                        driveFile = CreateFile(fileStream, fileName, folderId, token);
                    }
                }
                catch (WebException e)
                {
                    Global.Logger.Error("GoogleDriveApp: Error download exportLink", e);
                    request.Abort();

                    var httpResponse = (HttpWebResponse)e.Response;
                    if (httpResponse.StatusCode == HttpStatusCode.Unauthorized && fileType == FileType.Spreadsheet)
                    {
                        throw new SecurityException(FilesCommonResource.AppDriveSpreadsheetException, e);
                    }
                }
            }
            else
            {
                var downloadUrl = jsonFile.Value<string>("downloadUrl");

                var ext = GetCorrectExt(jsonFile);
                var convertedUrl = ConvertFile(downloadUrl, ext, token);

                driveFile = CreateFile(convertedUrl, fileName, folderId, token);
            }

            jsonFile = JObject.Parse(driveFile);
            return jsonFile.Value<string>("id");
        }


        private static string GetCorrectTitle(JObject jsonFile)
        {
            var title = (jsonFile.Value<string>("title") ?? "").ToLower();
            var extTitle = FileUtility.GetFileExtension(title);
            var correctExt = GetCorrectExt(jsonFile);

            if (extTitle != correctExt)
            {
                title = title + correctExt;
            }
            return title;
        }

        private static string GetCorrectExt(JObject jsonFile)
        {
            string ext;
            var mimeType = (jsonFile.Value<string>("mimeType") ?? "").ToLower();

            FileType fileType;
            if (GoogleMimeTypes.TryGetValue(mimeType, out fileType))
            {
                ext = FileUtility.InternalExtension[fileType];
            }
            else
            {
                var title = (jsonFile.Value<string>("title") ?? "").ToLower();
                ext = FileUtility.GetFileExtension(title);

                if (MimeMapping.GetMimeMapping(ext) != mimeType)
                {
                    var originalFilename = (jsonFile.Value<string>("originalFilename") ?? "").ToLower();
                    ext = FileUtility.GetFileExtension(originalFilename);

                    if (MimeMapping.GetMimeMapping(ext) != mimeType)
                    {
                        ext = MimeMapping.GetExtention(mimeType);

                        Global.Logger.Debug("GoogleDriveApp: Try GetCorrectExt - " + ext + " for - " + mimeType);
                    }
                }
            }
            return ext;
        }

        private static String PerformRequest(String uri, String contentType = "", String method = "", String queryString = "", Dictionary<string, string> headers = null)
        {
            if (String.IsNullOrEmpty(uri))
            {
                Global.Logger.Error("GoogleDriveApp: request with empty url");
                return null;
            }

            if (String.IsNullOrEmpty(method))
                method = "GET";

            byte[] bytes = null;
            if (!String.IsNullOrEmpty(queryString))
                bytes = Encoding.UTF8.GetBytes(queryString);

            var request = WebRequest.Create(uri);
            request.Method = method;
            request.Timeout = 5000;
            if (headers != null)
            {
                foreach (var key in headers.Keys)
                {
                    request.Headers.Add(key, headers[key]);
                }
            }

            if (request.Method != "GET" && !String.IsNullOrEmpty(contentType) && bytes != null)
            {
                request.ContentType = contentType;
                request.ContentLength = bytes.Length;
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(bytes, 0, bytes.Length);
                }
            }

            try
            {
                var response = request.GetResponse();
                using (var stream = response.GetResponseStream())
                {
                    return stream != null ? new StreamReader(stream).ReadToEnd() : null;
                }
            }
            catch (WebException)
            {
                request.Abort();
            }

            return null;
        }
    }
}