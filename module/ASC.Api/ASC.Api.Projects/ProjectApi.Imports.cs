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
using System.Linq;
using System.Security;
using ASC.Api.Attributes;
using ASC.Api.Collections;
using ASC.Api.Employee;
using ASC.Api.Projects.Wrappers;
using ASC.Core;
using ASC.MessagingSystem;
using ASC.Web.Projects.Configuration;
using ASC.Web.Projects.Resources;
using SecurityContext = ASC.Core.SecurityContext;

namespace ASC.Api.Projects
{
    public partial class ProjectApi
    {
        ///<summary>
        ///Adds the company URL for importing to the queue
        ///</summary>
        ///<short>
        ///Add importing URL to queue
        ///</short>
        /// <category>Import</category>        
        ///<param name="url">The company URL </param>
        ///<param name="userName">User Name </param>
        ///<param name="password">Password </param>
        ///<param name="importClosed">Import closed</param>
        ///<param name="disableNotifications">Disable notifications</param>
        ///<param name="importUsersAsCollaborators">Flag for add users as guests</param>
        ///<param name="projects" optional="true">Projects for importing</param>
        ///<returns>Import status</returns>
        [Create(@"import")]
        public ImportStatus Add(string url, string userName, string password, bool importClosed, bool disableNotifications, bool importUsersAsCollaborators, IEnumerable<int> projects)
        {
            if (!CoreContext.UserManager.IsUserInGroup(SecurityContext.CurrentAccount.ID, Core.Users.Constants.GroupAdmin.ID))
            {
                throw new SecurityException();
            }

            //Validate all data
            if (string.IsNullOrEmpty(url)) throw new ArgumentException(ImportResource.EmptyURL);

            if (string.IsNullOrEmpty(userName)) throw new ArgumentException(ImportResource.EmptyEmail);

            if (string.IsNullOrEmpty(password)) throw new ArgumentException(ImportResource.EmptyPassword);

            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute)) throw new ArgumentException(ImportResource.MalformedUrl);

            ImportQueue.Add(url, userName, password, importClosed, disableNotifications, importUsersAsCollaborators, projects);

            MessageService.Send(_context, MessageAction.ProjectsImportedFromBasecamp, url);

            return GetStatus();
        }

        ///<summary>
        ///Returns the list of the projects to be imported
        ///</summary>
        ///<short>
        ///Get projects for import
        ///</short>
        /// <category>Import</category>
        ///<returns>List of projects</returns>
        ///<param name="url">The company URL </param>
        ///<param name="userName">User Name </param>
        ///<param name="password">Password </param>
        [Create(@"import/projects")]
        public IEnumerable<ObjectWrapperBase> GetProjectsForImport(string url, string userName, string password)
        {
            return ImportQueue.GetProjects(url, userName, password).Select(x => new ObjectWrapperBase {Id = x.ID, Title = x.Title, Status = (int)x.Status, Responsible = new EmployeeWraperFull()}).ToSmartList();
        }

        ///<summary>
        ///Returns the number of users that can be added to the import. This number can be negative, if the number of imported users exceeds the quota.
        ///</summary>
        ///<short>
        ///Returns the number of users that can be added to the import
        ///</short>
        /// <category>Import</category>
        ///<returns>Number of users</returns>
        ///<param name="url">The company URL </param>
        ///<param name="userName">User Name </param>
        ///<param name="password">Password </param>
        ///<visible>false</visible>
        [Create(@"import/quota")]
        public int CheckUsersQuota(string url, string userName, string password)
        {
            return ImportQueue.CheckUsersQuota(url, userName, password);
        }

        ///<summary>
        ///Returns the project importing status
        ///</summary>
        ///<short>
        ///Get import status
        ///</short>
        /// <category>Import</category>
        ///<returns>Importing Status</returns>
        [Read(@"import")]
        public ImportStatus GetStatus()
        {
            if (!CoreContext.UserManager.IsUserInGroup(SecurityContext.CurrentAccount.ID, Core.Users.Constants.GroupAdmin.ID))
            {
                throw new SecurityException();
            }

            return ImportQueue.GetStatus();
        }
    }
}