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
using System.Web;
using System.Web.UI;
using ASC.Projects.Engine;
using ASC.Web.Core.Utility.Skins;
using ASC.Web.Projects.Classes;
using ASC.Web.Projects.Configuration;
using ASC.Web.Projects.Resources;
using ASC.Web.Studio.Controls.Common;

namespace ASC.Web.Projects.Masters
{
    public partial class BasicTemplate : MasterPage
    {
        #region Properties

        private string currentPage;

        public string CurrentPage
        {
            get
            {
                if (string.IsNullOrEmpty(currentPage))
                {
                    var absolutePathWithoutQuery = Request.Url.AbsolutePath.Substring(0, Request.Url.AbsolutePath.IndexOf(".aspx", StringComparison.Ordinal));
                    currentPage = absolutePathWithoutQuery.Substring(absolutePathWithoutQuery.LastIndexOf('/') + 1);
                }
                return currentPage;
            }
        }

        public bool DisabledSidePanel
        {
            get { return Master.DisabledSidePanel; }
            set { Master.DisabledSidePanel = value; }
        }

        public bool DisabledHelpTour
        {
            get { return Master.DisabledHelpTour; }
            set { Master.DisabledHelpTour = value; }
        }

        public bool DisabledPrjNavPanel { get; set; }

        public bool DisabledEmptyScreens { get; set; }

        #endregion

        #region Events

        protected void Page_Load(object sender, EventArgs e)
        {
            InitControls();

            WriteClientScripts();

            Page.EnableViewState = false;
        }

        #endregion

        #region Methods

        protected void InitControls()
        {
            if (!Master.DisabledSidePanel)
            {
                projectsNavigationPanel.Controls.Add(LoadControl(PathProvider.GetFileStaticRelativePath("Common/NavigationSidePanel.ascx")));
            }

            if (!DisabledPrjNavPanel && RequestContext.IsInConcreteProject)
            {
                _projectNavigatePanel.Controls.Add(LoadControl(PathProvider.GetFileStaticRelativePath("Projects/ProjectNavigatePanel.ascx")));
            }

            _commonPopupHolder.Controls.Add(LoadControl(PathProvider.GetFileStaticRelativePath("Common/CommonPopupContainer.ascx")));

            if (!(DisabledEmptyScreens))
                InitEmptyScreens();
        }

        private void InitEmptyScreens()
        {
            emptyScreenPlaceHolders.Controls.Add(RenderEmptyScreenForFilter(MessageResource.FilterNoDiscussions, MessageResource.DescrEmptyListMilFilter, "discEmptyScreenForFilter"));
            emptyScreenPlaceHolders.Controls.Add(RenderEmptyScreenForFilter(TaskResource.NoTasks, TaskResource.DescrEmptyListTaskFilter, "tasksEmptyScreenForFilter"));
            emptyScreenPlaceHolders.Controls.Add(RenderEmptyScreenForFilter(MilestoneResource.FilterNoMilestones, MilestoneResource.DescrEmptyListMilFilter, "mileEmptyScreenForFilter"));
            emptyScreenPlaceHolders.Controls.Add(RenderEmptyScreenForFilter(ProjectsCommonResource.Filter_NoProjects, ProjectResource.DescrEmptyListProjFilter, "prjEmptyScreenForFilter"));
            emptyScreenPlaceHolders.Controls.Add(RenderEmptyScreenForFilter(TimeTrackingResource.NoTimersFilter, TimeTrackingResource.DescrEmptyListTimersFilter, "timeEmptyScreenForFilter"));

            emptyScreenPlaceHolders.Controls.Add(new EmptyScreenControl
            {
                ImgSrc = WebImageSupplier.GetAbsoluteWebPath("empty_screen_tasks.png", ProductEntryPoint.ID),
                Header = TaskResource.NoTasksCreated,
                Describe = String.Format(TaskResource.TasksHelpTheManage, TaskResource.DescrEmptyListTaskFilter),
                ID = "emptyListTask",
                ButtonHTML = RequestContext.CanCreateTask(true) ? String.Format("<span class='link dotline addFirstElement'>{0}</span>", TaskResource.AddFirstTask) : string.Empty
            });

            emptyScreenPlaceHolders.Controls.Add(new EmptyScreenControl
            {
                ImgSrc = WebImageSupplier.GetAbsoluteWebPath("empty_screen_discussions.png", ProductEntryPoint.ID),
                Header = MessageResource.DiscussionNotFound_Header,
                Describe = MessageResource.DiscussionNotFound_Describe,
                ID = "emptyListDiscussion",
                ButtonHTML = RequestContext.CanCreateDiscussion(true) ?
                            (RequestContext.IsInConcreteProject
                                ? String.Format("<a href='messages.aspx?prjID={0}&action=add' class='link dotline addFirstElement'>{1}</a>", RequestContext.GetCurrentProjectId(), MessageResource.StartFirstDiscussion)
                                : String.Format("<a href='messages.aspx?action=add' class='link dotline addFirstElement'>{0}</a>", MessageResource.StartFirstDiscussion))
                            : string.Empty
            });

            emptyScreenPlaceHolders.Controls.Add(new EmptyScreenControl
            {
                ImgSrc = WebImageSupplier.GetAbsoluteWebPath("empty_screen_milestones.png", ProductEntryPoint.ID),
                Header = MilestoneResource.MilestoneNotFound_Header,
                Describe = String.Format(MilestoneResource.MilestonesMarkMajorTimestamps),
                ID = "emptyListMilestone",
                ButtonHTML = RequestContext.CanCreateMilestone(true) ? String.Format("<a class='link dotline addFirstElement'>{0}</a>", MilestoneResource.PlanFirstMilestone) : string.Empty
            });

            emptyScreenPlaceHolders.Controls.Add(new EmptyScreenControl
            {
                Header = ProjectResource.EmptyListProjHeader,
                ImgSrc = WebImageSupplier.GetAbsoluteWebPath("projects_logo.png", ProductEntryPoint.ID),
                Describe = ProjectResource.EmptyListProjDescribe,
                ID = "emptyListProjects",
                ButtonHTML = ProjectSecurity.CanCreateProject() ? string.Format("<a href='projects.aspx?action=add' class='projectsEmpty link dotline addFirstElement'>{0}<a>", ProjectResource.CreateFirstProject) : string.Empty
            });

            emptyScreenPlaceHolders.Controls.Add(new EmptyScreenControl
            {
                ImgSrc = WebImageSupplier.GetAbsoluteWebPath("empty_screen_time_tracking.png", ProductEntryPoint.ID),
                Header = TimeTrackingResource.NoTtimers,
                Describe = String.Format(TimeTrackingResource.NoTimersNote),
                ID = "emptyListTimers",
                ButtonHTML = String.Format("<span class='link dotline addFirstElement {1}'>{0}</span>", TimeTrackingResource.StartTimer, RequestContext.CanCreateTime(true) ? string.Empty : "display-none")
            });

            emptyScreenPlaceHolders.Controls.Add(new EmptyScreenControl
            {
                Header = ProjectTemplatesResource.EmptyListTemplateHeader,
                ImgSrc = WebImageSupplier.GetAbsoluteWebPath("project-templates_logo.png", ProductEntryPoint.ID),
                Describe = ProjectTemplatesResource.EmptyListTemplateDescr,
                ID = "emptyListTemplates",
                ButtonHTML = string.Format("<a href='projectTemplates.aspx?action=add' class='projectsEmpty link dotline addFirstElement'>{0}<a>", ProjectTemplatesResource.EmptyListTemplateButton)
            });
        }

        protected void WriteClientScripts()
        {
            WriteProjectResources();

            if (Page is GanttChart)
            {
                Page.RegisterBodyScripts(LoadControl(VirtualPathUtility.ToAbsolute("~/products/projects/masters/GanttBodyScripts.ascx")));
                return;
            }

            Page.RegisterStyleControl(LoadControl(VirtualPathUtility.ToAbsolute("~/products/projects/masters/Styles.ascx")));
            Page.RegisterBodyScripts(LoadControl(VirtualPathUtility.ToAbsolute("~/products/projects/masters/CommonBodyScripts.ascx")));
        }

        public void RegisterCRMResources()
        {
            Page.RegisterStyleControl(ResolveUrl(VirtualPathUtility.ToAbsolute("~/products/crm/app_themes/default/css/common.less")));
            Page.RegisterStyleControl(ResolveUrl(VirtualPathUtility.ToAbsolute("~/products/crm/app_themes/default/css/contacts.less")));

            Page.RegisterBodyScripts(VirtualPathUtility.ToAbsolute("~/js/third-party/jquery/jquery.watermarkinput.js"));
            Page.RegisterBodyScripts(VirtualPathUtility.ToAbsolute("~/products/crm/js/contacts.js"));
            Page.RegisterBodyScripts(VirtualPathUtility.ToAbsolute("~/products/crm/js/common.js"));
        }

        public void WriteProjectResources()
        {
            Page.RegisterClientLocalizationScript(typeof(ClientScripts.ClientLocalizationResources));
            Page.RegisterClientLocalizationScript(typeof(ClientScripts.ClientTemplateResources));

            Page.RegisterClientScript(typeof(ClientScripts.ClientUserResources));
            Page.RegisterClientScript(typeof(ClientScripts.ClientCurrentUserResources));

            if (RequestContext.IsInConcreteProject)
                Page.RegisterClientScript(typeof(ClientScripts.ClientProjectResources));
        }

        private static EmptyScreenControl RenderEmptyScreenForFilter(string headerText, string description, string id = "emptyScreenForFilter")
        {
            return new EmptyScreenControl
                {
                    ImgSrc = WebImageSupplier.GetAbsoluteWebPath("empty_screen_filter.png"),
                    Header = headerText,
                    Describe = description,
                    ID = id,
                    ButtonHTML = String.Format("<a class='clearFilterButton link dotline'>{0}</a>", ProjectsFilterResource.ClearFilter)
                };
        }

        #endregion
    }
}