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
using System.Linq;
using ASC.Collections;
using ASC.Common.Data;
using ASC.Common.Data.Sql;
using ASC.Common.Data.Sql.Expressions;
using ASC.Core.Tenants;
using ASC.Projects.Core.DataInterfaces;
using ASC.Projects.Core.Domain;

namespace ASC.Projects.Data.DAO
{
    class CachedMilestoneDao : MilestoneDao
    {
        private readonly HttpRequestDictionary<Milestone> projectCache = new HttpRequestDictionary<Milestone>("milestone");


        public CachedMilestoneDao(string dbId, int tenant)
            : base(dbId, tenant)
        {
        }

        public override Milestone GetById(int id)
        {
            return projectCache.Get(id.ToString(CultureInfo.InvariantCulture), () => GetBaseById(id));
        }

        private Milestone GetBaseById(int id)
        {
            return base.GetById(id);
        }

        public override Milestone Save(Milestone milestone)
        {
            if (milestone != null)
            {
                ResetCache(milestone.ID);
            }
            return base.Save(milestone);
        }

        public override void Delete(int id)
        {
            ResetCache(id);
            base.Delete(id);
        }

        private void ResetCache(int milestoneId)
        {
            projectCache.Reset(milestoneId.ToString(CultureInfo.InvariantCulture));
        }
    }

    class MilestoneDao : BaseDao, IMilestoneDao
    {
        public MilestoneDao(string dbId, int tenant)
            : base(dbId, tenant)
        {
        }

        public List<Milestone> GetAll()
        {
            using (var db = new DbManager(DatabaseId))
            {
                return db.ExecuteList(CreateQuery()).ConvertAll(ToMilestone);
            }
        }

        public List<Milestone> GetByProject(int projectId)
        {
            using (var db = new DbManager(DatabaseId))
            {
                return db.ExecuteList(CreateQuery().Where("t.project_id", projectId))
                    .ConvertAll(ToMilestone);
            }
        }

        public List<Milestone> GetByFilter(TaskFilter filter, bool isAdmin)
        {
            var query = CreateQuery();

            if (filter.Max > 0 && filter.Max < 150000)
            {
                query.SetFirstResult((int)filter.Offset);
                query.SetMaxResults((int)filter.Max * 2);
            }

            query.OrderBy("t.status", true);

            if (!string.IsNullOrEmpty(filter.SortBy))
            {
                var sortColumns = filter.SortColumns["Milestone"];
                sortColumns.Remove(filter.SortBy);

                query.OrderBy("t." + (filter.SortBy == "create_on" ? "id" : filter.SortBy), filter.SortOrder);

                foreach (var sort in sortColumns.Keys)
                {
                    query.OrderBy("t." + (sort == "create_on" ? "id" : sort), sortColumns[sort]);
                }
            }

            query = CreateQueryFilter(query, filter, isAdmin);

            using (var db = new DbManager(DatabaseId))
            {
                return db.ExecuteList(query).ConvertAll(ToMilestone);
            }
        }

        public int GetByFilterCount(TaskFilter filter, bool isAdmin)
        {
            var query = new SqlQuery(MilestonesTable + " t")
                .InnerJoin(ProjectsTable + " p", Exp.EqColumns("t.project_id", "p.id") & Exp.EqColumns("t.tenant_id", "p.tenant_id"))
                .Select("t.id")
                .GroupBy("t.id")
                .Where("t.tenant_id", Tenant);

            query = CreateQueryFilter(query, filter, isAdmin);

            var queryCount = new SqlQuery().SelectCount().From(query, "t1");

            using (var db = new DbManager(DatabaseId))
            {
                return db.ExecuteScalar<int>(queryCount);
            }
        }

        public List<Milestone> GetByStatus(int projectId, MilestoneStatus milestoneStatus)
        {
            using (var db = new DbManager(DatabaseId))
            {
                return db.ExecuteList(CreateQuery().Where("t.project_id", projectId).Where("t.status", milestoneStatus))
                    .ConvertAll(ToMilestone);
            }
        }

        public List<Milestone> GetUpcomingMilestones(int offset, int max, params int[] projects)
        {
            var query = CreateQuery()
                .SetFirstResult(offset)
                .Where("p.status", ProjectStatus.Open)
                .Where(Exp.Ge("t.deadline", TenantUtil.DateTimeNow().Date))
                .Where("t.status", MilestoneStatus.Open)
                .SetMaxResults(max)
                .OrderBy("t.deadline", true);
            if (projects != null && 0 < projects.Length)
            {
                query.Where(Exp.In("p.id", projects.Take(0 < max ? max : projects.Length).ToArray()));
            }

            using (var db = new DbManager(DatabaseId))
            {
                return db.ExecuteList(query).ConvertAll(ToMilestone);
            }
        }

        public List<Milestone> GetLateMilestones(int offset, int max, params int[] projects)
        {
            var query = CreateQuery()
                .SetFirstResult(offset)
                .Where("p.status", ProjectStatus.Open)
                .Where(!Exp.Eq("t.status", MilestoneStatus.Closed))
                .Where(Exp.Le("t.deadline", TenantUtil.DateTimeNow().Date.AddDays(-1)))
                .SetMaxResults(max)
                .OrderBy("t.deadline", true);
            if (projects != null && 0 < projects.Length)
            {
                query.Where(Exp.In("p.id", projects.Take(0 < max ? max : projects.Length).ToArray()));
            }

            using (var db = new DbManager(DatabaseId))
            {
                return db.ExecuteList(query).ConvertAll(ToMilestone);
            }
        }

        public List<Milestone> GetByDeadLine(DateTime deadline)
        {
            using (var db = new DbManager(DatabaseId))
            {
                return db.ExecuteList(CreateQuery().Where("t.deadline", deadline.Date).OrderBy("t.deadline", true))
                    .ConvertAll(ToMilestone);
            }
        }

        public virtual Milestone GetById(int id)
        {
            using (var db = new DbManager(DatabaseId))
            {
                return db.ExecuteList(CreateQuery().Where("t.id", id))
                    .ConvertAll(ToMilestone)
                    .SingleOrDefault();
            }
        }

        public List<Milestone> GetById(int[] id)
        {
            using (var db = new DbManager(DatabaseId))
            {
                return db.ExecuteList(CreateQuery().Where(Exp.In("t.id", id)))
                    .ConvertAll(ToMilestone);
            }
        }

        public bool IsExists(int id)
        {
            using (var db = new DbManager(DatabaseId))
            {
                var count = db.ExecuteScalar<long>(Query(MilestonesTable).SelectCount().Where("id", id));
                return 0 < count;
            }
        }

        public List<object[]> GetInfoForReminder(DateTime deadline)
        {
            var q = new SqlQuery(MilestonesTable)
                .Select("tenant_id", "id", "deadline")
                .Where(Exp.Between("deadline", deadline.Date.AddDays(-1), deadline.Date.AddDays(1)))
                .Where("status", MilestoneStatus.Open)
                .Where("is_notify", 1);

            using (var db = new DbManager(DatabaseId))
            {
                return db.ExecuteList(q)
                    .ConvertAll(r => new object[] {Convert.ToInt32(r[0]), Convert.ToInt32(r[1]), Convert.ToDateTime(r[2])});
            }
        }
        
        public virtual Milestone Save(Milestone milestone)
        {
            using (var db = new DbManager(DatabaseId))
            {
                if (milestone.DeadLine.Kind != DateTimeKind.Local)
                    milestone.DeadLine = TenantUtil.DateTimeFromUtc(milestone.DeadLine);

                var insert = Insert(MilestonesTable)
                    .InColumnValue("id", milestone.ID)
                    .InColumnValue("project_id", milestone.Project != null ? milestone.Project.ID : 0)
                    .InColumnValue("title", milestone.Title)
                    .InColumnValue("create_by", milestone.CreateBy.ToString())
                    .InColumnValue("create_on", TenantUtil.DateTimeToUtc(milestone.CreateOn))
                    .InColumnValue("last_modified_by", milestone.LastModifiedBy.ToString())
                    .InColumnValue("last_modified_on", TenantUtil.DateTimeToUtc(milestone.LastModifiedOn))
                    .InColumnValue("deadline", milestone.DeadLine)
                    .InColumnValue("status", milestone.Status)
                    .InColumnValue("is_notify", milestone.IsNotify)
                    .InColumnValue("is_key", milestone.IsKey)
                    .InColumnValue("description", milestone.Description)
                    .InColumnValue("status_changed", milestone.StatusChangedOn)
                    .InColumnValue("responsible_id", milestone.Responsible.ToString())
                    .Identity(1, 0, true);
                milestone.ID = db.ExecuteScalar<int>(insert);
                return milestone;
            }
        }

        public virtual void Delete(int id)
        {
            using (var db = new DbManager(DatabaseId))
            {
                using (var tx = db.BeginTransaction())
                {
                    db.ExecuteNonQuery(Delete(CommentsTable).Where("target_uniq_id", ProjectEntity.BuildUniqId<Milestone>(id)));
                    db.ExecuteNonQuery(Update(TasksTable).Set("milestone_id", 0).Where("milestone_id", id));
                    db.ExecuteNonQuery(Delete(MilestonesTable).Where("id", id));

                    tx.Commit();
                }
            }
        }

        public string GetLastModified()
        {
            using (var db = new DbManager(DatabaseId))
            {
                var query = Query(MilestonesTable).SelectMax("last_modified_on").SelectCount();
                var data = db.ExecuteList(query).FirstOrDefault();
                if (data == null)
                {
                    return "";
                }
                var lastModified = "";
                if (data[0] != null)
                {
                    lastModified += TenantUtil.DateTimeFromUtc(Convert.ToDateTime(data[0])).ToString(CultureInfo.InvariantCulture);
                }
                if (data[1] != null)
                {
                    lastModified += data[1];
                }

                return lastModified;
            }
        }

        private SqlQuery CreateQuery()
        {
            return new SqlQuery(MilestonesTable + " t")
                .InnerJoin(ProjectsTable + " p", Exp.EqColumns("t.tenant_id", "p.tenant_id") & Exp.EqColumns("t.project_id", "p.id"))
                .Select(ProjectDao.ProjectColumns.Select(c => "p." + c).ToArray())
                .Select("t.id", "t.title", "t.create_by", "t.create_on", "t.last_modified_by", "t.last_modified_on")
                .Select("t.deadline", "t.status", "t.is_notify", "t.is_key", "t.description", "t.responsible_id")
                .Select("(select sum(case pt1.status when 1 then 1 when 4 then 1 else 0 end) from projects_tasks pt1 where pt1.tenant_id = t.tenant_id and  pt1.milestone_id=t.id)")
                .Select("(select sum(case pt2.status when 2 then 1 else 0 end) from projects_tasks pt2 where pt2.tenant_id = t.tenant_id and  pt2.milestone_id=t.id)")
                .GroupBy("t.id")
                .Where("t.tenant_id", Tenant);
        }

        private SqlQuery CreateQueryFilter(SqlQuery query, TaskFilter filter, bool isAdmin)
        {
            if (filter.MilestoneStatuses.Count != 0)
            {
                query.Where("t.status", filter.MilestoneStatuses.First());
            }

            if (filter.ProjectIds.Count != 0)
            {
                query.Where(Exp.In("t.project_id", filter.ProjectIds));
            }
            else
            {
                if (filter.MyProjects)
                {
                    query.InnerJoin(ParticipantTable + " ppp", Exp.EqColumns("p.id", "ppp.project_id") & Exp.Eq("ppp.removed", false) & Exp.EqColumns("ppp.tenant", "t.tenant_id"));
                    query.Where("ppp.participant_id", CurrentUserID);
                }
            }

            if (filter.UserId != Guid.Empty)
            {
                query.Where(Exp.Eq("t.responsible_id", filter.UserId));
            }

            if (filter.TagId != 0)
            {
                query.InnerJoin(ProjectTagTable + " ptag", Exp.EqColumns("ptag.project_id", "t.project_id"));
                query.Where("ptag.tag_id", filter.TagId);
            }

            if (filter.ParticipantId.HasValue)
            {
                var existSubtask = new SqlQuery(SubtasksTable + " pst").Select("pst.task_id").Where(Exp.EqColumns("t.tenant_id", "pst.tenant_id") & Exp.EqColumns("pt.id", "pst.task_id") & Exp.Eq("pst.status", TaskStatus.Open));
                var existResponsible = new SqlQuery(TasksResponsibleTable + " ptr1").Select("ptr1.task_id").Where(Exp.EqColumns("t.tenant_id", "ptr1.tenant_id") & Exp.EqColumns("pt.id", "ptr1.task_id"));

                existSubtask.Where(Exp.Eq("pst.responsible_id", filter.ParticipantId.ToString()));
                existResponsible.Where(Exp.Eq("ptr1.responsible_id", filter.ParticipantId.ToString()));

                query.LeftOuterJoin(TasksTable + " as pt", Exp.EqColumns("pt.milestone_id", "t.id") & Exp.EqColumns("pt.tenant_id", "t.tenant_id"));
                query.Where(Exp.Exists(existSubtask) | Exp.Exists(existResponsible));
            }

            if (!filter.FromDate.Equals(DateTime.MinValue) && !filter.FromDate.Equals(DateTime.MaxValue))
            {
                query.Where(Exp.Ge("t.deadline", TenantUtil.DateTimeFromUtc(filter.FromDate)));
            }

            if (!filter.ToDate.Equals(DateTime.MinValue) && !filter.ToDate.Equals(DateTime.MaxValue))
            {
                query.Where(Exp.Le("t.deadline", TenantUtil.DateTimeFromUtc(filter.ToDate)));
            }

            if (!string.IsNullOrEmpty(filter.SearchText))
            {
                query.Where(Exp.Like("t.title", filter.SearchText, SqlLike.AnyWhere));
            }

            if (!isAdmin)
            {
                if (!filter.MyProjects && !filter.MyMilestones)
                {
                    query.LeftOuterJoin(ParticipantTable + " ppp", Exp.Eq("ppp.participant_id", CurrentUserID) & Exp.EqColumns("ppp.project_id", "t.project_id") & Exp.EqColumns("ppp.tenant", "t.tenant_id"));
                }

                var isInTeam = Exp.Sql("ppp.security IS NOT NULL") & Exp.Eq("ppp.removed", false);
                var canReadMilestones = !Exp.Eq("security & " + (int)ProjectTeamSecurity.Milestone, (int)ProjectTeamSecurity.Milestone);
                var responsible = Exp.Eq("t.responsible_id", CurrentUserID);

                query.Where(Exp.Eq("p.private", false) | isInTeam & (responsible | canReadMilestones));
            }

            return query;
        }

        private static Milestone ToMilestone(object[] r)
        {
            var offset = ProjectDao.ProjectColumns.Length;
            return new Milestone
                       {
                           Project = r[0] != null ? ProjectDao.ToProject(r) : null,
                           ID = Convert.ToInt32(r[0 + offset]),
                           Title = (string) r[1 + offset],
                           CreateBy = ToGuid(r[2 + offset]),
                           CreateOn = TenantUtil.DateTimeFromUtc(Convert.ToDateTime(r[3 + offset])),
                           LastModifiedBy = ToGuid(r[4 + offset]),
                           LastModifiedOn = TenantUtil.DateTimeFromUtc(Convert.ToDateTime(r[5 + offset])),
                           DeadLine = DateTime.SpecifyKind(Convert.ToDateTime(r[6 + offset]), DateTimeKind.Local),
                           Status = (MilestoneStatus) Convert.ToInt32(r[7 + offset]),
                           IsNotify = Convert.ToBoolean(r[8 + offset]),
                           IsKey = Convert.ToBoolean(r[9 + offset]),
                           Description = (string) r[10 + offset],
                           Responsible = ToGuid(r[11 + offset]),
                           ActiveTaskCount = Convert.ToInt32(r[12 + ProjectDao.ProjectColumns.Length]),
                           ClosedTaskCount = Convert.ToInt32(r[13 + ProjectDao.ProjectColumns.Length])
                       };
        }
        

        internal List<Milestone> GetMilestones(Exp where)
        {
            using (var db = new DbManager(DatabaseId))
            {
                return db.ExecuteList(CreateQuery().Where(where)).ConvertAll(ToMilestone);
            }
        }
    }
}
