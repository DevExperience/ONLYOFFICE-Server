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
using System.Web;
using ASC.Common.Data;
using ASC.Common.Data.Sql;
using ASC.Common.Data.Sql.Expressions;
using ASC.Projects.Core.Domain;
using ASC.Projects.Core.Domain.Entities.Feed;
using ASC.Projects.Engine;
using ASC.Web.Core.Security;
using ASC.Web.Studio.Utility;
using System.Linq;

namespace ASC.Feed.Aggregator.Modules.Projects
{
    internal class TasksModule : FeedModule
    {
        private const string item = "task";


        protected override string Table
        {
            get { return "projects_tasks"; }
        }

        protected override string LastUpdatedColumn
        {
            get { return "create_on"; }
        }

        protected override string TenantColumn
        {
            get { return "tenant_id"; }
        }

        protected override string DbId
        {
            get { return Constants.ProjectsDbId; }
        }


        public override string Name
        {
            get { return Constants.TasksModule; }
        }

        public override string Product
        {
            get { return ModulesHelper.ProjectsProductName; }
        }

        public override Guid ProductID
        {
            get { return ModulesHelper.ProjectsProductID; }
        }

        public override IEnumerable<int> GetTenantsWithFeeds(DateTime fromTime)
        {
            var q1 = new SqlQuery("projects_tasks")
                .Select("tenant_id")
                .Where(Exp.Gt("create_on", fromTime))
                .GroupBy(1)
                .Having(Exp.Gt("count(*)", 0));

            var q2 = new SqlQuery("projects_comments")
                .Select("tenant_id")
                .Where("substring_index(target_uniq_id, '_', 1) = 'Task'")
                .Where(Exp.Gt("create_on", fromTime))
                .GroupBy(1)
                .Having(Exp.Gt("count(*)", 0));

            using (var db = new DbManager(DbId))
            {
                return db.ExecuteList(q1)
                         .ConvertAll(r => Convert.ToInt32(r[0]))
                         .Union(db.ExecuteList(q2).ConvertAll(r => Convert.ToInt32(r[0])));
            }
        }

        public override bool VisibleFor(Feed feed, object data, Guid userId)
        {
            return base.VisibleFor(feed, data, userId) && ProjectSecurity.CanGoToFeed((Task)data, userId);
        }

        public override IEnumerable<Tuple<Feed, object>> GetFeeds(FeedFilter filter)
        {
            var query =
                new SqlQuery("projects_tasks t")
                    .Select(TaskColumns().Select(t => "t." + t).ToArray())
                    .LeftOuterJoin("projects_tasks_responsible r", Exp.EqColumns("r.task_id", "t.id") & Exp.Eq("r.tenant_id", filter.Tenant))
                    .Select("group_concat(distinct r.responsible_id)")
                    .InnerJoin("projects_projects p", Exp.EqColumns("p.id", "t.project_id") & Exp.Eq("p.tenant_id", filter.Tenant))
                    .Select(ProjectColumns().Select(p => "p." + p).ToArray())
                    .LeftOuterJoin("projects_comments c", Exp.EqColumns("c.target_uniq_id", "concat('Task_', convert(t.id, char))") & Exp.Eq("c.tenant_id", filter.Tenant) & Exp.Eq("c.inactive", 0))
                    .Select(CommentColumns().Select(c => "c." + c).ToArray())
                    .Where("t.tenant_id", filter.Tenant)
                    .Where(Exp.Between("t.create_on", filter.Time.From, filter.Time.To) | Exp.Between("c.create_on", filter.Time.From, filter.Time.To))
                    .GroupBy("c.id, t.id");

            using (var db = new DbManager(DbId))
            {
                var comments = db.ExecuteList(query)
                    .ConvertAll(ToComment);
                var groupedTasks = comments.GroupBy(c => c.Task.ID);

                return groupedTasks
                    .Select(t => new Tuple<Task, IEnumerable<ProjectComment>>(t.First().Task, t))
                    .Select(ToFeed);
            }
        }


        private static IEnumerable<string> TaskColumns()
        {
            return new[]
                {
                    "id",
                    "title",
                    "description",
                    "priority",
                    "status",
                    "status_changed",
                    "milestone_id",
                    "sort_order",
                    "deadline",
                    "start_date",
                    "create_by",
                    "create_on",
                    "last_modified_by",
                    "last_modified_on" // 13
                };
        }

        private static IEnumerable<string> ProjectColumns()
        {
            return new[]
                {
                    "id", //15
                    "title",
                    "description",
                    "status",
                    "status_changed",
                    "responsible_id",
                    "private",
                    "create_by",
                    "create_on",
                    "last_modified_by",
                    "last_modified_on" // 25
                };
        }

        private static IEnumerable<string> CommentColumns()
        {
            return new[]
                {
                    "id", // 26
                    "content",
                    "create_by",
                    "create_on",
                    "parent_id",
                    "target_uniq_id" // 31
                };
        }

        private static ProjectComment ToComment(object[] r)
        {
            var p = new ProjectComment
                {
                    Task = new Task
                        {
                            ID = Convert.ToInt32(r[0]),
                            Title = Convert.ToString(r[1]),
                            Description = Convert.ToString(r[2]),
                            Priority = (TaskPriority)Convert.ToInt32(r[3]),
                            Status = (TaskStatus)Convert.ToInt32(r[4]),
                            StatusChangedOn = Convert.ToDateTime(r[5]),
                            Milestone = Convert.ToInt32(r[6]),
                            SortOrder = Convert.ToInt32(r[7]),
                            Deadline = Convert.ToDateTime(r[8]),
                            StartDate = Convert.ToDateTime(r[9]),
                            CreateBy = new Guid(Convert.ToString(r[10])),
                            CreateOn = Convert.ToDateTime(r[11]),
                            LastModifiedBy = new Guid(Convert.ToString(r[12])),
                            LastModifiedOn = Convert.ToDateTime(r[13]),
                            Responsibles =
                                r[14] != null
                                    ? new HashSet<Guid>(Convert.ToString(r[14]).Split(',').Select(x => new Guid(x)))
                                    : new HashSet<Guid>(),
                            Project = new Project
                                {
                                    ID = Convert.ToInt32(r[15]),
                                    Title = Convert.ToString(r[16]),
                                    Description = Convert.ToString(r[17]),
                                    Status = (ProjectStatus)Convert.ToInt32(18),
                                    StatusChangedOn = Convert.ToDateTime(r[19]),
                                    Responsible = new Guid(Convert.ToString(r[20])),
                                    Private = Convert.ToBoolean(r[21]),
                                    CreateBy = new Guid(Convert.ToString(r[22])),
                                    CreateOn = Convert.ToDateTime(r[23]),
                                    LastModifiedBy = new Guid(Convert.ToString(r[24])),
                                    LastModifiedOn = Convert.ToDateTime(r[25]),
                                }
                        }
                };

            if (r[26] != null)
            {
                p.Comment = new Comment
                    {
                        ID = new Guid(Convert.ToString(r[26])),
                        Content = Convert.ToString(r[27]),
                        CreateBy = new Guid(Convert.ToString(r[28])),
                        CreateOn = Convert.ToDateTime(r[29]),
                        Parent = new Guid(Convert.ToString(r[30])),
                        TargetUniqID = Convert.ToString(r[31])
                    };
            }
            return p;
        }

        private Tuple<Feed, object> ToFeed(Tuple<Task, IEnumerable<ProjectComment>> t)
        {
            var task = t.Item1;

            var itemUrl = "/products/projects/tasks.aspx?prjID=" + task.Project.ID + "&id=" + task.ID;
            var projectUrl = "/products/projects/tasks.aspx?prjID=" + task.Project.ID;
            var commentApiUrl = "/api/2.0/project/task/" + task.ID + "/comment.json";
            
            var responsiblesString = Helper.GetUsersString(task.Responsibles);

            var comments = t.Item2.Where(c => c.Comment != null).OrderBy(c => c.Comment.CreateOn).ToList();
            var feedDate = comments.Any() ? comments.First().Comment.CreateOn : task.CreateOn;
            var feedAutohor = comments.Any() ? comments.Last().Comment.CreateBy : task.CreateBy;

            var feed = new Feed(task.CreateBy, feedDate, true)
                {
                    Item = item,
                    ItemId = task.ID.ToString(CultureInfo.InvariantCulture),
                    ItemUrl = CommonLinkUtility.ToAbsolute(itemUrl),
                    LastModifiedBy = feedAutohor,
                    Product = Product,
                    Module = Name,
                    Action = comments.Any() ? FeedAction.Commented : FeedAction.Created,
                    Title = task.Title,
                    Description = Helper.GetHtmlDescription(HttpUtility.HtmlEncode(task.Description)),
                    ExtraLocation = task.Project.Title,
                    ExtraLocationUrl = CommonLinkUtility.ToAbsolute(projectUrl),
                    AdditionalInfo = responsiblesString,
                    HasPreview = false,
                    CanComment = true,
                    CommentApiUrl = CommonLinkUtility.ToAbsolute(commentApiUrl),
                    Comments = comments.Select(ToFeedComment),
                    GroupId = string.Format("{0}_{1}", item, task.ID)
                };
            feed.Keywords = string.Format("{0} {1} {2}",
                                          task.Title,
                                          task.Description,
                                          string.Join(" ", feed.Comments.Select(x => x.Description)));

            return new Tuple<Feed, object>(feed, task);
        }

        private static FeedComment ToFeedComment(ProjectComment comment)
        {
            return new FeedComment(comment.Comment.CreateBy)
                {
                    Id = comment.Comment.ID.ToString(),
                    Description = HtmlSanitizer.Sanitize(comment.Comment.Content),
                    Date = comment.Comment.CreateOn
                };
        }
    }
}