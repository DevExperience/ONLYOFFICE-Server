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
using ASC.Api.Attributes;
using ASC.Api.CRM.Wrappers;
using ASC.Api.Employee;
using ASC.Api.Exceptions;
using ASC.CRM.Core.Entities;

namespace ASC.Api.CRM
{
    public partial class CRMApi
    {
        /// <summary>
        ///   Creates a new task template container with the type and title specified in the request
        /// </summary>
        /// <param name="entityType">Type</param>
        /// <param name="title">Title</param>
        /// <short>Create task template container</short> 
        /// <category>Task Templates</category>
        /// <returns>
        ///     Task template container
        /// </returns>
        ///<exception cref="ArgumentException"></exception>
        [Create(@"{entityType:(contact|person|company|opportunity|case)}/tasktemplatecontainer")]
        public TaskTemplateContainerWrapper CreateTaskTemplateContainer(string entityType, string title)
        {
            if (string.IsNullOrEmpty(title)) throw new ArgumentException();

            var taskTemplateContainer = new TaskTemplateContainer
                {
                    EntityType = ToEntityType(entityType),
                    Title = title
                };

            taskTemplateContainer.ID = DaoFactory.GetTaskTemplateContainerDao().SaveOrUpdate(taskTemplateContainer);
            return ToTaskTemplateContainerWrapper(taskTemplateContainer);
        }

        /// <summary>
        ///    Returns the complete list of all the task template containers available on the portal
        /// </summary>
        /// <param name="entityType">Type</param>
        /// <short>Get task template container list</short> 
        /// <category>Task Templates</category>
        /// <returns>
        ///     Task template container list
        /// </returns>
        [Read(@"{entityType:(contact|person|company|opportunity|case)}/tasktemplatecontainer")]
        public IEnumerable<TaskTemplateContainerWrapper> GetTaskTemplateContainers(string entityType)
        {
            return ToTaskListTemplateContainerWrapper(DaoFactory.GetTaskTemplateContainerDao().GetItems(ToEntityType(entityType)));
        }

        /// <summary>
        ///   Deletes the task template container with the ID specified in the request
        /// </summary>
        /// <param name="containerid">Task template container ID</param>
        /// <short>Delete task template container</short> 
        /// <category>Task Templates</category>
        /// <returns>
        ///    Deleted task template container
        /// </returns>
        ///<exception cref="ArgumentException"></exception>
        ///<exception cref="ItemNotFoundException"></exception>
        [Delete(@"tasktemplatecontainer/{containerid:[0-9]+}")]
        public TaskTemplateContainerWrapper DeleteTaskTemplateContainer(int containerid)
        {
            if (containerid <= 0) throw new ArgumentException();

            var result = ToTaskTemplateContainerWrapper(DaoFactory.GetTaskTemplateContainerDao().GetByID(containerid));
            if (result == null) throw new ItemNotFoundException();

            DaoFactory.GetTaskTemplateContainerDao().Delete(containerid);

            return result;
        }

        /// <summary>
        ///   Updates the task template container with the ID specified in the request
        /// </summary>
        /// <param name="containerid">Task template container ID</param>
        /// <param name="title">Title</param>
        /// <short>Update task template container</short> 
        /// <category>Task Templates</category>
        /// <returns>
        ///     Task template container
        /// </returns>
        ///<exception cref="ArgumentException"></exception>
        ///<exception cref="ItemNotFoundException"></exception>
        [Update(@"tasktemplatecontainer/{containerid:[0-9]+}")]
        public TaskTemplateContainerWrapper UpdateTaskTemplateContainer(int containerid, string title)
        {
            if (containerid <= 0 || string.IsNullOrEmpty(title)) throw new ArgumentException();

            var result = DaoFactory.GetTaskTemplateContainerDao().GetByID(containerid);
            if (result == null) throw new ItemNotFoundException();

            result.Title = title;

            DaoFactory.GetTaskTemplateContainerDao().SaveOrUpdate(result);

            return ToTaskTemplateContainerWrapper(result);
        }

        /// <summary>
        ///   Returns the detailed information on the task template container with the ID specified in the request
        /// </summary>
        /// <param name="containerid">Task template container ID</param>
        /// <short>Get task template container by ID</short> 
        /// <category>Task Templates</category>
        /// <returns>
        ///     Task template container
        /// </returns>
        ///<exception cref="ArgumentException"></exception>
        ///<exception cref="ItemNotFoundException"></exception>
        [Read(@"tasktemplatecontainer/{containerid:[0-9]+}")]
        public TaskTemplateContainerWrapper GetTaskTemplateContainerByID(int containerid)
        {
            if (containerid <= 0) throw new ArgumentException();

            var item = DaoFactory.GetTaskTemplateContainerDao().GetByID(containerid);
            if (item == null) throw new ItemNotFoundException();

            return ToTaskTemplateContainerWrapper(item);
        }

        /// <summary>
        ///   Returns the list of all tasks in the container with the ID specified in the request
        /// </summary>
        /// <param name="containerid">Task template container ID</param>
        /// <short>Get task template list by contaier ID</short> 
        /// <category>Task Templates</category>
        /// <returns>
        ///     Task template list
        /// </returns>
        ///<exception cref="ArgumentException"></exception>
        ///<exception cref="ItemNotFoundException"></exception>
        [Read(@"tasktemplatecontainer/{containerid:[0-9]+}/tasktemplate")]
        public IEnumerable<TaskTemplateWrapper> GetTaskTemplates(int containerid)
        {
            if (containerid <= 0) throw new ArgumentException();

            var container = DaoFactory.GetTaskTemplateContainerDao().GetByID(containerid);
            if (container == null) throw new ItemNotFoundException();

            return DaoFactory.GetTaskTemplateDao().GetList(containerid).ConvertAll(ToTaskTemplateWrapper);
        }

        /// <summary>
        ///   Creates a new task template with the parameters specified in the request in the container with the selected ID
        /// </summary>
        /// <param name="containerid">Task template container ID</param>
        /// <param name="title">Title</param>
        /// <param name="description">Description</param>
        /// <param name="responsibleid">Responsible ID</param>
        /// <param name="categoryid">Category ID</param>
        /// <param name="isNotify">Responsible notification: notify or not</param>
        /// <param name="offsetTicks">Ticks offset</param>
        /// <param name="deadLineIsFixed"></param>
        /// <short>Create task template</short> 
        /// <category>Task Templates</category>
        /// <returns>Task template</returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ItemNotFoundException"></exception>
        [Create(@"tasktemplatecontainer/{containerid:[0-9]+}/tasktemplate")]
        public TaskTemplateWrapper CreateTaskTemplate(
            int containerid,
            string title,
            string description,
            Guid responsibleid,
            int categoryid,
            bool isNotify,
            long offsetTicks,
            bool deadLineIsFixed
            )
        {
            if (containerid <= 0 || string.IsNullOrEmpty(title) || categoryid <= 0) throw new ArgumentException();

            var container = DaoFactory.GetTaskTemplateContainerDao().GetByID(containerid);
            if (container == null) throw new ItemNotFoundException();

            var item = new TaskTemplate
                {
                    CategoryID = categoryid,
                    ContainerID = containerid,
                    DeadLineIsFixed = deadLineIsFixed,
                    Description = description,
                    isNotify = isNotify,
                    ResponsibleID = responsibleid,
                    Title = title,
                    Offset = TimeSpan.FromTicks(offsetTicks)
                };

            item.ID = DaoFactory.GetTaskTemplateDao().SaveOrUpdate(item);

            return ToTaskTemplateWrapper(item);
        }

        /// <summary>
        ///   Updates the selected task template with the parameters specified in the request in the container with the selected ID
        /// </summary>
        /// <param name="id">Task template ID</param>
        /// <param name="containerid">Task template container ID</param>
        /// <param name="title">Title</param>
        /// <param name="description">Description</param>
        /// <param name="responsibleid">Responsible ID</param>
        /// <param name="categoryid">Category ID</param>
        /// <param name="isNotify">Responsible notification: notify or not</param>
        /// <param name="offsetTicks">Ticks offset</param>
        /// <param name="deadLineIsFixed"></param>
        /// <short>Update task template</short> 
        /// <category>Task Templates</category>
        /// <returns>Task template</returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ItemNotFoundException"></exception>
        [Update(@"tasktemplatecontainer/{containerid:[0-9]+}/tasktemplate")]
        public TaskTemplateWrapper UpdateTaskTemplate(
            int id,
            int containerid,
            string title,
            string description,
            Guid responsibleid,
            int categoryid,
            bool isNotify,
            long offsetTicks,
            bool deadLineIsFixed
            )
        {
            if (containerid <= 0 || string.IsNullOrEmpty(title) || categoryid <= 0) throw new ArgumentException();

            var updatingItem = DaoFactory.GetTaskTemplateDao().GetByID(id);
            if (updatingItem == null) throw new ItemNotFoundException();

            var container = DaoFactory.GetTaskTemplateContainerDao().GetByID(containerid);
            if (container == null) throw new ItemNotFoundException();

            var item = new TaskTemplate
                {
                    CategoryID = categoryid,
                    ContainerID = containerid,
                    DeadLineIsFixed = deadLineIsFixed,
                    Description = description,
                    isNotify = isNotify,
                    ResponsibleID = responsibleid,
                    Title = title,
                    ID = id,
                    Offset = TimeSpan.FromTicks(offsetTicks)
                };

            item.ID = DaoFactory.GetTaskTemplateDao().SaveOrUpdate(item);

            return ToTaskTemplateWrapper(item);
        }

        /// <summary>
        ///   Deletes the task template with the ID specified in the request
        /// </summary>
        /// <param name="id">Task template ID</param>
        /// <short>Delete task template</short> 
        /// <category>Task Templates</category>
        /// <returns>Task template</returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ItemNotFoundException"></exception>
        [Delete(@"tasktemplatecontainer/tasktemplate/{id:[0-9]+}")]
        public TaskTemplateWrapper DeleteTaskTemplate(int id)
        {
            if (id <= 0) throw new ArgumentException();

            var taskTemplate = DaoFactory.GetTaskTemplateDao().GetByID(id);
            if (taskTemplate == null) throw new ItemNotFoundException();

            var result = ToTaskTemplateWrapper(taskTemplate);

            DaoFactory.GetTaskTemplateDao().Delete(id);

            return result;
        }

        /// <summary>
        ///   Return the task template with the ID specified in the request
        /// </summary>
        /// <param name="id">Task template ID</param>
        /// <short>Get task template by ID</short> 
        /// <category>Task Templates</category>
        /// <returns>Task template</returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ItemNotFoundException"></exception>
        [Read(@"tasktemplatecontainer/tasktemplate/{id:[0-9]+}")]
        public TaskTemplateWrapper GetTaskTemplateByID(int id)
        {
            if (id <= 0) throw new ArgumentException();

            var taskTemplate = DaoFactory.GetTaskTemplateDao().GetByID(id);
            if (taskTemplate == null) throw new ItemNotFoundException();

            return ToTaskTemplateWrapper(taskTemplate);
        }

        protected TaskTemplateWrapper ToTaskTemplateWrapper(TaskTemplate taskTemplate)
        {
            return new TaskTemplateWrapper
                {
                    Category = GetTaskCategoryByID(taskTemplate.CategoryID),
                    ContainerID = taskTemplate.ContainerID,
                    DeadLineIsFixed = taskTemplate.DeadLineIsFixed,
                    Description = taskTemplate.Description,
                    ID = taskTemplate.ID,
                    isNotify = taskTemplate.isNotify,
                    Title = taskTemplate.Title,
                    OffsetTicks = taskTemplate.Offset.Ticks,
                    Responsible = EmployeeWraper.Get(taskTemplate.ResponsibleID)
                };
        }

        protected IEnumerable<TaskTemplateContainerWrapper> ToTaskListTemplateContainerWrapper(IEnumerable<TaskTemplateContainer> items)
        {
            var result = new List<TaskTemplateContainerWrapper>();

            var taskTemplateDictionary = DaoFactory.GetTaskTemplateDao().GetAll()
                                                   .GroupBy(item => item.ContainerID)
                                                   .ToDictionary(x => x.Key, y => y.Select(ToTaskTemplateWrapper));

            foreach (var item in items)
            {
                var taskTemplateContainer = new TaskTemplateContainerWrapper
                    {
                        Title = item.Title,
                        EntityType = item.EntityType.ToString(),
                        ID = item.ID
                    };

                if (taskTemplateDictionary.ContainsKey(taskTemplateContainer.ID))
                {
                    taskTemplateContainer.Items = taskTemplateDictionary[taskTemplateContainer.ID];
                }

                result.Add(taskTemplateContainer);
            }

            return result;
        }

        protected TaskTemplateContainerWrapper ToTaskTemplateContainerWrapper(TaskTemplateContainer item)
        {
            return ToTaskListTemplateContainerWrapper(new List<TaskTemplateContainer>
                {
                    item
                }).FirstOrDefault();
        }
    }
}