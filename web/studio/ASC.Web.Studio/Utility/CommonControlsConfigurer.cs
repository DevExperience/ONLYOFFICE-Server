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
using System.IO;
using System.Linq;
using System.Web;
using ASC.Data.Storage;
using ASC.Web.Core.Utility.Skins;
using ASC.Web.Studio.Core;
using ASC.Web.Studio.UserControls.Common.Comments;
using ASC.Web.Studio.Utility.BBCodeParser;
using AjaxPro;
using System.Web.SessionState;

namespace ASC.Web.Studio.Utility
{
    [AjaxNamespace("CommonControlsConfigurer")]
    public class CommonControlsConfigurer
    {
        #region FCK Editor

        public static string FCKEditorBasePath
        {
            get { return VirtualPathUtility.ToAbsolute("~/products/community/modules/wiki/wikiuc/fckeditor/"); }
        }

        [Serializable]
        private class FCKTempUploadsInfo
        {
            public string StoreDomain { get; set; }
            public string FolderID { get; set; }
            public List<string> Files { get; set; }
            public bool IsEdit { get; set; }
            public int TenantID { get; set; }

            public FCKTempUploadsInfo()
            {
                Files = new List<string>();
            }
        }

        private static void FCKRemoveTempUploads(string domain, string folderID, bool isEdit)
        {
            var session = HttpContext.Current.Session;
            var uploads = session["tempFCKUploads"] as List<FCKTempUploadsInfo>;
            if (uploads != null)
            {
                uploads.RemoveAll(u => String.Equals(u.FolderID, folderID, StringComparison.InvariantCultureIgnoreCase)
                                       && String.Equals(u.StoreDomain, domain, StringComparison.InvariantCultureIgnoreCase)
                                       && u.IsEdit == isEdit);
            }

            session["fck_folder_" + domain] = null;
        }

        internal static void FCKClearTempStore(HttpSessionState session)
        {
            if (session == null)
                return;

            var uploads = session["tempFCKUploads"] as List<FCKTempUploadsInfo>;
            if (uploads == null) return;

            IDataStore store = null;
            foreach (var u in uploads.Where(u => u.Files.Count > 0))
            {
                if (store == null)
                    store = StorageFactory.GetStorage(u.TenantID.ToString(), "fckuploaders");

                if (u.IsEdit)
                {
                    foreach (var fileName in u.Files)
                        store.DeleteFiles(u.StoreDomain, u.FolderID, fileName.ToLower(), false);
                }
                else
                {
                    store.DeleteFiles(u.StoreDomain, u.FolderID, "*", false);
                }
            }
        }

        private static string GetCurrentFolderID(string domain)
        {
            var session = HttpContext.Current.Session;
            var folder = session["fck_folder_" + domain] as string;
            if (String.IsNullOrEmpty(folder))
            {
                folder = Guid.NewGuid().ToString();
                session["fck_folder_" + domain] = folder;
            }
            return folder;
        }

        internal static string FCKAddTempUploads(string domain, string filename, string itemID)
        {
            var isEdit = !String.IsNullOrEmpty(itemID);
            string folderID;
            if (isEdit)
            {
                folderID = FCKUploadsDBManager.GetFolderID(domain, itemID);
                if (String.IsNullOrEmpty(folderID))
                    folderID = GetCurrentFolderID(domain);
            }
            else
                folderID = GetCurrentFolderID(domain);

            var session = HttpContext.Current.Session;
            var uploads = session["tempFCKUploads"] as List<FCKTempUploadsInfo>;
            if (uploads == null)
            {
                uploads = new List<FCKTempUploadsInfo>();
                session["tempFCKUploads"] = uploads;
            }

            var tempInfo = uploads.Find(u => String.Equals(u.FolderID, folderID, StringComparison.InvariantCultureIgnoreCase)
                                             && String.Equals(u.StoreDomain, domain, StringComparison.InvariantCultureIgnoreCase)
                                             && u.IsEdit == isEdit);

            if (tempInfo == null)
            {
                tempInfo = new FCKTempUploadsInfo
                    {
                        FolderID = folderID,
                        StoreDomain = domain,
                        IsEdit = isEdit,
                        TenantID = TenantProvider.CurrentTenantID
                    };
                uploads.Add(tempInfo);
            }
            tempInfo.Files.Add(filename);
            return folderID;
        }


        public static void FCKEditingComplete(string domain, string itemID, string editedHtml, bool isEdit)
        {
            if (editedHtml == null) throw new ArgumentNullException("editedHtml");

            var isExistsFolder = false;
            string folderID;
            if (isEdit)
            {
                folderID = FCKUploadsDBManager.GetFolderID(domain, itemID);
                if (String.IsNullOrEmpty(folderID))
                    folderID = GetCurrentFolderID(domain);
                else
                    isExistsFolder = true;
            }
            else
                folderID = GetCurrentFolderID(domain);

            var store = StorageFactory.GetStorage(TenantProvider.CurrentTenantID.ToString(), "fckuploaders");
            var existsFileList = new List<Uri>(store.ListFiles(domain, folderID + "/", "*", false)).ConvertAll(uri => uri.ToString());

            var usingUploadFiles = new List<string>();
            var deleteFiles = new List<string>();

            foreach (var file in new List<string>(existsFileList))
            {
                if (editedHtml.IndexOf(file, StringComparison.InvariantCultureIgnoreCase) >= 0 ||
                    HttpUtility.UrlDecode(editedHtml).IndexOf(file, StringComparison.InvariantCultureIgnoreCase) >= 0 || HttpUtility.HtmlDecode(editedHtml).IndexOf(file, StringComparison.InvariantCultureIgnoreCase) >= 0)
                    usingUploadFiles.Add(file);
                else
                    deleteFiles.Add(file);

                //HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes(string.Format("//*[contains(@src,'{0}')]", file.ToLowerInvariant()));
                //if (nodes != null)
                //    usingUploadFiles.Add(file);
                //else
                //    deleteFiles.Add(file);
            }

            foreach (var df in deleteFiles)
            {
                var fileName = Path.GetFileName(df);
                store.Delete(domain, folderID + "/" + fileName);
            }

            if (usingUploadFiles.Count > 0 && !isExistsFolder)
            {
                FCKUploadsDBManager.SetUploadRelations(domain, folderID, itemID);
            }
            else if (usingUploadFiles.Count == 0)
            {
                FCKUploadsDBManager.RemoveUploadRelation(domain, folderID, itemID);
            }

            FCKRemoveTempUploads(domain, folderID, isEdit);
        }

        public static void FCKEditingCancel(string domain)
        {
            FCKEditingCancel(domain, null);
        }

        public static void FCKEditingCancel(string domain, string itemID)
        {
            string folderID;
            var isEdit = !String.IsNullOrEmpty(itemID);

            var store = StorageFactory.GetStorage(TenantProvider.CurrentTenantID.ToString(), "fckuploaders");
            if (isEdit)
            {
                folderID = FCKUploadsDBManager.GetFolderID(domain, itemID);

                var session = HttpContext.Current.Session;
                var uploads = session["tempFCKUploads"] as List<FCKTempUploadsInfo>;
                if (uploads != null)
                {
                    var tempUploads = uploads.Find(u => String.Equals(u.FolderID, folderID, StringComparison.InvariantCultureIgnoreCase)
                                                        && String.Equals(u.StoreDomain, domain, StringComparison.InvariantCultureIgnoreCase)
                                                        && u.IsEdit == isEdit);

                    if (tempUploads != null)
                    {
                        foreach (var file in tempUploads.Files)
                        {
                            try
                            {
                                store.DeleteFiles(domain, folderID + "/", file, false);
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }
            else
            {
                folderID = GetCurrentFolderID(domain);
                try
                {
                    store.DeleteFiles(domain, folderID + "/", "*", false);
                }
                catch
                {
                }
            }

            FCKRemoveTempUploads(domain, folderID, isEdit);
        }

        public static void FCKUploadsRemoveForItem(string domain, string itemID)
        {
            var store = StorageFactory.GetStorage(TenantProvider.CurrentTenantID.ToString(), "fckuploaders");
            var folderID = FCKUploadsDBManager.GetFolderID(domain, itemID);
            if (String.IsNullOrEmpty(folderID)) return;

            FCKUploadsDBManager.RemoveUploadRelation(domain, folderID, itemID);
            try
            {
                store.DeleteFiles(domain, folderID + "/", "*", false);
            }
            catch
            {
            }
        }

        #endregion

        #region comments

        public static void CommentsConfigure(CommentsList commentList)
        {
            commentList.PID = CommonLinkUtility.GetProductID().ToString();
            commentList.InactiveMessage = Resources.Resource.CommentRemovedMessage;
            commentList.UserProfileUrlResolver = user => CommonLinkUtility.GetUserProfile(user);
            commentList.AddCommentLink = Resources.Resource.AddNewCommentButton;
            commentList.CancelCommentLink = Resources.Resource.CancelCommentButton;

            commentList.SaveButton = Resources.Resource.PublishButton;
            commentList.BehaviorID = "_commentObj";

            commentList.PreviewButton = Resources.Resource.PreviewButton;
            commentList.HidePrevuewButton = Resources.Resource.CloseButton;
            commentList.CancelButton = Resources.Resource.CancelButton;
            commentList.InactiveMessage = Resources.Resource.CommentWasRemoved;

            commentList.RemoveCommentLink = Resources.Resource.DeleteButton;
            commentList.ResponseCommentLink = Resources.Resource.AnswerButton;
            commentList.EditCommentLink = Resources.Resource.EditButton;

            commentList.FCKBasePath = FCKEditorBasePath;

            commentList.CommentsTitle = Resources.Resource.Comments;

            commentList.ConfirmRemoveCommentMessage = Resources.Resource.ConfirmRemoveCommentMessage;

            commentList.AdditionalSubmitText = "<span class=\"text-medium-describe\" style=\"margin-left:5px;\">" + Resources.Resource.OrPress + "</span> <span class=\"text-base\">" + Resources.Resource.CtrlEnterKeys + "</span>";

            commentList.FCKEditorAreaCss = WebSkin.BaseCSSFileAbsoluteWebPath;

            commentList.OnEditedCommentJS = "FCKCommentsController.EditCommentHandler";
            commentList.OnCanceledCommentJS = "FCKCommentsController.CancelCommentHandler";
            commentList.OnRemovedCommentJS = "FCKCommentsController.RemoveCommentHandler";

            try
            {
                AjaxPro.Utility.RegisterTypeForAjax(typeof(CommonControlsConfigurer));
            }
            catch
            {
            }
        }

        [AjaxMethod(HttpSessionStateRequirement.ReadWrite)]
        public object EditCommentComplete(string commentID, string domain, string html, bool isEdit)
        {
            try
            {
                FCKEditingComplete(domain, commentID, html, isEdit);
                return 1;
            }

            catch
            {
                return 0;
            }
        }

        [AjaxMethod(HttpSessionStateRequirement.ReadWrite)]
        public object RemoveCommentComplete(string commentID, string domain)
        {
            try
            {
                FCKUploadsRemoveForItem(domain, commentID);
                return 1;
            }
            catch
            {
                return 0;
            }
        }

        [AjaxMethod(HttpSessionStateRequirement.ReadWrite)]
        public object CancelCommentComplete(string commentID, string domain, bool isEdit)
        {
            try
            {
                if (isEdit)
                    FCKEditingCancel(domain, commentID);
                else
                    FCKEditingCancel(domain);

                return 1;
            }
            catch
            {
                return 0;
            }
        }

        #endregion

        #region Smiles

        public static List<Smile> Smiles
        {
            get
            {
                var smiles = new List<Smile>
                    {
                        new Smile(":-)", WebImageSupplier.GetAbsoluteWebPath("smile1.gif"), Resources.Resource.SmlSmile, ":-)"),
                        new Smile(";-)", WebImageSupplier.GetAbsoluteWebPath("smile2.gif"), Resources.Resource.SmlEye, ";-)"),
                        new Smile(":-\\", WebImageSupplier.GetAbsoluteWebPath("smile3.gif"), Resources.Resource.SmlSmirk, ":-\\\\"),
                        new Smile(":-D", WebImageSupplier.GetAbsoluteWebPath("smile4.gif"), Resources.Resource.SmlHollywood, ":-D"),
                        new Smile(":-(", WebImageSupplier.GetAbsoluteWebPath("smile5.gif"), Resources.Resource.SmlMourning, ":-("),
                        new Smile("8-)", WebImageSupplier.GetAbsoluteWebPath("smile6.gif"), Resources.Resource.SmlSpectacles, "8-)"),
                        new Smile("*DANCE*", WebImageSupplier.GetAbsoluteWebPath("smile7.gif"), Resources.Resource.SmlDance, "*DANCE*"),
                        new Smile("[:-}", WebImageSupplier.GetAbsoluteWebPath("smile8.gif"), Resources.Resource.SmlMusic, "[:-}"),
                        new Smile("*CRAZY*", WebImageSupplier.GetAbsoluteWebPath("smile9.gif"), Resources.Resource.SmlCrazy, "*CRAZY*"),
                        new Smile("=-O", WebImageSupplier.GetAbsoluteWebPath("smile10.gif"), Resources.Resource.SmlAmazement, "=-O"),
                        new Smile(":-P", WebImageSupplier.GetAbsoluteWebPath("smile11.gif"), Resources.Resource.SmlTongue, ":-P"),
                        new Smile(":'(", WebImageSupplier.GetAbsoluteWebPath("smile12.gif"), Resources.Resource.SmlCry, ":\\'("),
                        new Smile(":-!", WebImageSupplier.GetAbsoluteWebPath("smile13.gif"), Resources.Resource.SmlGreen, ":-!"),
                        new Smile("*SUPER*", WebImageSupplier.GetAbsoluteWebPath("smile14.gif"), Resources.Resource.SmlSuper, "*SUPER*"),
                        new Smile("*SORRY*", WebImageSupplier.GetAbsoluteWebPath("smile15.gif"), Resources.Resource.SmlSorry, "*SORRY*"),
                        new Smile("*YAHOO*", WebImageSupplier.GetAbsoluteWebPath("smile16.gif"), Resources.Resource.SmlYAHOO, "*YAHOO*"),
                        new Smile("*OK*", WebImageSupplier.GetAbsoluteWebPath("smile17.gif"), Resources.Resource.SmlOK, "*OK*"),
                        new Smile("]-:)", WebImageSupplier.GetAbsoluteWebPath("smile18.gif"), Resources.Resource.SmlEvil, "]-:)"),
                        new Smile("*SOS*", WebImageSupplier.GetAbsoluteWebPath("smile19.gif"), Resources.Resource.SmlSOS, "*SOS*"),
                        new Smile("*DRINK*", WebImageSupplier.GetAbsoluteWebPath("smile20.gif"), Resources.Resource.SmlDrink, "*DRINK*"),
                        new Smile("@=", WebImageSupplier.GetAbsoluteWebPath("smile21.gif"), Resources.Resource.SmlBomb, "@=")
                    };

                //OK

                //Sos

                return smiles;
            }
        }

        #endregion

        #region BBCodeParser

        public static ParserConfiguration CoreConfig
        {
            get
            {
                var config = new ParserConfiguration(true);
                foreach (var sm in Smiles)
                    config.ExpressionReplacements.Add(new ExpressionReplacement(sm.Value, "<img title=\"" + sm.Title + "\" src=\"" + sm.Img + "\" alt=\"" + sm.Value + "\">"));

                config.ExpressionReplacements.Add(new ExpressionReplacement("\n", "<br/>"));

                var tc = new TagConfiguration("URL", "<a target=\"_blank\" href=\"{1}\">{0}</a>") { IsParseTextReqularExpressions = false };
                var tpo = new TagParamOption
                    {
                        PreValue = "http://",
                        ParamNumber = 1,
                        IsUseAnotherParamValue = true,
                        AnotherParamNumber = 0
                    };
                tc.ParamOptions.Add(tpo);
                config.TagConfigurations.Add(tc);

                config.RegExpTemplates.Add(RegularExpressionTemplate.HTMLReferenceExpression);

                config.TagConfigurations.Add(new TagConfiguration("SIZE1", "<font size=\"1\">{0}</font>"));
                config.TagConfigurations.Add(new TagConfiguration("SIZE2", "<font size=\"2\">{0}</font>"));
                config.TagConfigurations.Add(new TagConfiguration("SIZE3", "<font size=\"3\">{0}</font>"));
                config.TagConfigurations.Add(new TagConfiguration("SIZE4", "<font size=\"4\">{0}</font>"));
                config.TagConfigurations.Add(new TagConfiguration("SIZE5", "<font size=\"5\">{0}</font>"));
                config.TagConfigurations.Add(new TagConfiguration("RED", "<span style=\"color:red;\">{0}</span>"));
                config.TagConfigurations.Add(new TagConfiguration("GREEN", "<span style=\"color:green;\">{0}</span>"));
                config.TagConfigurations.Add(new TagConfiguration("BLUE", "<span style=\"color:blue;\">{0}</span>"));
                config.TagConfigurations.Add(new TagConfiguration("MAIL", "<a href=\"mailto:{1}\">{0}</a>"));
                config.TagConfigurations.Add(new TagConfiguration("QUOTE", "<table class='borderBase describe-text' style=\"border-width: 2px;\" cellpadding=\"0\" cellspacing=\"0\"><tr height=\"20\"><td style=\"padding-left:5px;\"><span style='font-weight:bolder;'>{1}:</span></td></tr><tr><td style=\"padding:5px;\">{0}</td></tr></table>"));
                config.TagConfigurations.Add(new TagConfiguration("IMG", "<img alt='' src=\"{0}\"/>") { IsParseTextReqularExpressions = false });
                config.TagConfigurations.Add(new TagConfiguration("FIXED", "<pre>{0}</pre>", false) { IsParseTextReplacement = false, IsParseTextReqularExpressions = false });
                config.TagConfigurations.Add(new TagConfiguration("B", "<b>{0}</b>"));
                config.TagConfigurations.Add(new TagConfiguration("I", "<i>{0}</i>"));
                config.TagConfigurations.Add(new TagConfiguration("U", "<u>{0}</u>"));
                config.TagConfigurations.Add(new TagConfiguration("STRIKE", "<strike>{0}</strike>"));
                config.TagConfigurations.Add(new TagConfiguration("REPLY", "<span class=\"baseHeader\">To: {0}</span><br/>"));

                config.TagConfigurations.Add(new TagConfiguration("CPP", "<pre><code class='cpp'>{0}</code></pre>", false) { IsParseTextReplacement = false, IsParseTextReqularExpressions = false });
                config.TagConfigurations.Add(new TagConfiguration("CS", "<pre><code class='cs'>{0}</code></pre>", false) { IsParseTextReplacement = false, IsParseTextReqularExpressions = false });
                config.TagConfigurations.Add(new TagConfiguration("CSS", "<pre><code class='css'>{0}</code></pre>", false));
                config.TagConfigurations.Add(new TagConfiguration("DELPHI", "<pre><code class='delphi'>{0}</code></pre>", false) { IsParseTextReplacement = false, IsParseTextReqularExpressions = false });
                config.TagConfigurations.Add(new TagConfiguration("DOS", "<pre><code class='dos'>{0}</code></pre>", false) { IsParseTextReplacement = false, IsParseTextReqularExpressions = false });
                config.TagConfigurations.Add(new TagConfiguration("HTMLXML", "<pre><code class='html-xml'>{0}</code></pre>", false) { IsParseTextReplacement = false, IsParseTextReqularExpressions = false });
                config.TagConfigurations.Add(new TagConfiguration("JAVA", "<pre><code class='java'>{0}</code></pre>", false) { IsParseTextReplacement = false, IsParseTextReqularExpressions = false });
                config.TagConfigurations.Add(new TagConfiguration("JAVASCRIPT", "<pre><code class='javascript'>{0}</code></pre>", false) { IsParseTextReplacement = false, IsParseTextReqularExpressions = false });
                config.TagConfigurations.Add(new TagConfiguration("SQL", "<pre><code class='sql'>{0}</code></pre>", false) { IsParseTextReplacement = false, IsParseTextReqularExpressions = false });
                config.TagConfigurations.Add(new TagConfiguration("VBSCRIPT", "<pre><code class='vbscript'>{0}</code></pre>", false) { IsParseTextReplacement = false, IsParseTextReqularExpressions = false });

                return config;
            }
        }

        public static ParserConfiguration TextConfig
        {
            get
            {
                var config = new ParserConfiguration(false);

                config.TagConfigurations.Add(new TagConfiguration("URL", "{0}"));
                config.TagConfigurations.Add(new TagConfiguration("SIZE1", "{0}"));
                config.TagConfigurations.Add(new TagConfiguration("SIZE2", "{0}"));
                config.TagConfigurations.Add(new TagConfiguration("SIZE3", "{0}"));
                config.TagConfigurations.Add(new TagConfiguration("SIZE4", "{0}"));
                config.TagConfigurations.Add(new TagConfiguration("SIZE5", "{0}"));
                config.TagConfigurations.Add(new TagConfiguration("RED", "{0}"));
                config.TagConfigurations.Add(new TagConfiguration("GREEN", "{0}"));
                config.TagConfigurations.Add(new TagConfiguration("BLUE", "{0}"));
                config.TagConfigurations.Add(new TagConfiguration("MAIL", "{0}"));
                config.TagConfigurations.Add(new TagConfiguration("QUOTE", "{0}"));
                config.TagConfigurations.Add(new TagConfiguration("IMG", "{0}", false) { IsParseTextReplacement = false, IsParseTextReqularExpressions = false });
                config.TagConfigurations.Add(new TagConfiguration("FIXED", "{0}", false) { IsParseTextReplacement = false, IsParseTextReqularExpressions = false });
                config.TagConfigurations.Add(new TagConfiguration("B", "{0}"));
                config.TagConfigurations.Add(new TagConfiguration("I", "{0}"));
                config.TagConfigurations.Add(new TagConfiguration("U", "{0}"));
                config.TagConfigurations.Add(new TagConfiguration("STRIKE", "{0}"));
                config.TagConfigurations.Add(new TagConfiguration("REPLY", "To: {0}\n"));

                config.TagConfigurations.Add(new TagConfiguration("CPP", "{0}", false) { IsParseTextReplacement = false, IsParseTextReqularExpressions = false });
                config.TagConfigurations.Add(new TagConfiguration("CS", "{0}", false) { IsParseTextReplacement = false, IsParseTextReqularExpressions = false });
                config.TagConfigurations.Add(new TagConfiguration("CSS", "{0}", false) { IsParseTextReplacement = false, IsParseTextReqularExpressions = false });
                config.TagConfigurations.Add(new TagConfiguration("DELPHI", "{0}", false) { IsParseTextReplacement = false, IsParseTextReqularExpressions = false });
                config.TagConfigurations.Add(new TagConfiguration("DOS", "{0}", false) { IsParseTextReplacement = false, IsParseTextReqularExpressions = false });
                config.TagConfigurations.Add(new TagConfiguration("HTMLXML", "{0}", false) { IsParseTextReplacement = false, IsParseTextReqularExpressions = false });
                config.TagConfigurations.Add(new TagConfiguration("JAVA", "{0}", false) { IsParseTextReplacement = false, IsParseTextReqularExpressions = false });
                config.TagConfigurations.Add(new TagConfiguration("JAVASCRIPT", "{0}", false) { IsParseTextReplacement = false, IsParseTextReqularExpressions = false });
                config.TagConfigurations.Add(new TagConfiguration("SQL", "{0}", false) { IsParseTextReplacement = false, IsParseTextReqularExpressions = false });
                config.TagConfigurations.Add(new TagConfiguration("VBSCRIPT", "{0}", false) { IsParseTextReplacement = false, IsParseTextReqularExpressions = false });

                return config;
            }
        }

        public static ParserConfiguration SimpleTextConfig
        {
            get
            {
                var config = new ParserConfiguration(true);

                config.ExpressionReplacements.Add(new ExpressionReplacement("\n", "<br/>"));
                config.RegExpTemplates.Add(RegularExpressionTemplate.HTMLReferenceExpression);

                return config;
            }

        }

        #endregion
    }
}