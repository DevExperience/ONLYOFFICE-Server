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

/*
    Copyright (c) Ascensio System SIA 2013. All rights reserved.
    http://www.teamlab.com
*/
if (typeof ASC === "undefined") {
    ASC = {};
}

if (typeof ASC.CRM === "undefined") {
    ASC.CRM = function() { return {} };
}


ASC.CRM.myTaskContactFilter = {
    filterId: 'tasksAdvansedFilter',
    idFilterByContact: 'contactID',

    type: 'custom-contact',
    hiddenContainerId: 'hiddenBlockForTaskContactSelector',
    headerContainerId: 'taskContactSelectorForFilter',
    isInProc: false,

    onSelectContact: function (event, item) {
        jq("#" + ASC.CRM.myTaskContactFilter.headerContainerId).find(".inner-text .value").text(item.title);

        var $filter = jq('#' + ASC.CRM.myTaskContactFilter.filterId);
        $filter.advansedFilter(ASC.CRM.myTaskContactFilter.idFilterByContact, { id: item.id, displayName: item.title, value: jq.toJSON([item.id, "contact"]) });
        $filter.advansedFilter('resize');
    },

    createFilterByContact: function (filter) {
        var o = document.createElement('div');
        o.innerHTML = [
      '<div class="default-value">',
        '<span class="title">',
          filter.title,
        '</span>',
        '<span class="selector-wrapper">',
          '<span class="contact-selector"></span>',
        '</span>',
        '<span class="btn-delete"></span>',
      '</div>'
        ].join('');
        return o;
    },

    customizeFilterByContact: function ($container, $filteritem, filter) {
        if (jq('#' + ASC.CRM.myTaskContactFilter.headerContainerId).parent().is("#" + ASC.CRM.myTaskContactFilter.hiddenContainerId)) {
            jq("#" + ASC.CRM.myTaskContactFilter.headerContainerId)
                .off("showList")
                .on("showList", function (event, item) {
                    ASC.CRM.myTaskContactFilter.onSelectContact(event, item);
                });
            jq('#' + ASC.CRM.myTaskContactFilter.headerContainerId).next().andSelf().appendTo($filteritem.find('span.contact-selector:first'));
        }
    },

    destroyFilterByContact: function ($container, $filteritem, filter) {
        if (!jq('#' + ASC.CRM.myTaskContactFilter.headerContainerId).parent().is("#" + ASC.CRM.myTaskContactFilter.hiddenContainerId)) {
            jq("#" + ASC.CRM.myTaskContactFilter.headerContainerId).off("showList");
            jq('#' + ASC.CRM.myTaskContactFilter.headerContainerId).find(".inner-text .value").text(ASC.CRM.Resources.CRMCommonResource.Select);
            jq('#' + ASC.CRM.myTaskContactFilter.headerContainerId).next().andSelf().appendTo(jq('#' + ASC.CRM.myTaskContactFilter.hiddenContainerId));
            jq('#' + ASC.CRM.myTaskContactFilter.headerContainerId).contactadvancedSelector("reset");
        }
    },

    processFilter: function ($container, $filteritem, filtervalue, params) {
        if (params && params.id && isFinite(params.id)) {
            jq("#" + ASC.CRM.myTaskContactFilter.headerContainerId).find(".inner-text .value").text(params.displayName);
            jq("#" + ASC.CRM.myTaskContactFilter.headerContainerId).contactadvancedSelector("select", [params.id]);
        }
    }
};


/*******************************************************************************
ListTaskView.ascx
*******************************************************************************/
ASC.CRM.ListTaskView = new function() {

    //Teamlab.bind(Teamlab.events.getException, _onGetException);

    function _onGetException(params, errors) {
        console.log('tasks.js ', errors);
        LoadingBanner.hideLoading();
    };

    var _setCookie = function (page, countOnPage) {
        if (ASC.CRM.ListTaskView.cookieKey && ASC.CRM.ListTaskView.cookieKey != "") {
            var cookie = {
                page: page,
                countOnPage: countOnPage
            };
            jq.cookies.set(ASC.CRM.ListTaskView.cookieKey, cookie, { path: location.pathname });
        }
    };

    var _renderNoTasksEmptyScreen = function () {
        jq("#taskList").hide();
        jq("#taskFilterContainer").hide();
        ASC.CRM.Common.hideExportButtons();
        jq("#emptyContentForTasksFilter:not(.display-none)").addClass("display-none");
        jq("#tasksEmptyScreen.display-none").removeClass("display-none");

        LoadingBanner.hideLoading();
        return false;
    };

    var _renderNoTasksForQueryEmptyScreen = function () {
        jq("#taskList").hide();
        ASC.CRM.Common.hideExportButtons();
        jq("#taskFilterContainer").show();
        ASC.CRM.ListTaskView.resizeFilter();
        jq("#emptyContentForTasksFilter.display-none").removeClass("display-none");

        LoadingBanner.hideLoading();
        return false;
    };


    var _initPageNavigatorControl = function (isTab, countOfRows, currentPageNumber) {
        window.taskPageNavigator = new ASC.Controls.PageNavigator.init("taskPageNavigator", "#divForTaskPager", countOfRows, ASC.CRM.Data.VisiblePageCount, currentPageNumber,
                                                                        ASC.CRM.Resources.CRMJSResource.Previous, ASC.CRM.Resources.CRMJSResource.Next);

        window.taskPageNavigator.changePageCallback = function (page) {
            _setCookie(page, window.taskPageNavigator.EntryCountOnPage);

            var startIndex = window.taskPageNavigator.EntryCountOnPage * (page - 1);
            if (isTab == true) {
                _getTasksForEntity(startIndex)
            } else {
                _renderContent(startIndex);
            }
        };
    };

    var _renderTaskPageNavigator = function (startIndex) {
        var tmpTotal;
        if (startIndex >= ASC.CRM.ListTaskView.Total) {
            tmpTotal = startIndex + 1;
        } else {
            tmpTotal = ASC.CRM.ListTaskView.Total;
        }
        window.taskPageNavigator.drawPageNavigator((startIndex / ASC.CRM.ListTaskView.CountOfRows).toFixed(0) * 1 + 1, tmpTotal);
    };

    var _changeFilter = function () {
        var defaultStartIndex = 0;
        if (ASC.CRM.ListTaskView.defaultCurrentPageNumber != 0) {
            _setCookie(ASC.CRM.ListTaskView.defaultCurrentPageNumber, window.taskPageNavigator.EntryCountOnPage);
            defaultStartIndex = (ASC.CRM.ListTaskView.defaultCurrentPageNumber - 1) * window.taskPageNavigator.EntryCountOnPage;
            ASC.CRM.ListTaskView.defaultCurrentPageNumber = 0;
        } else {
            _setCookie(0, window.taskPageNavigator.EntryCountOnPage);
        }

        _renderContent(defaultStartIndex);
    };

    var _renderContent = function (startIndex) {
        LoadingBanner.displayLoading();

        _getTasks(startIndex);
    };

    var _getTasksForEntity = function (startIndex) {
        LoadingBanner.displayLoading();
        var filterSettings = {
            entityid : ASC.CRM.ListTaskView.ContactID != 0 ? ASC.CRM.ListTaskView.ContactID : ASC.CRM.ListTaskView.EntityID,
            entitytype : ASC.CRM.ListTaskView.EntityType,
            sortBy : 'deadLine',
            sortOrder : 'descending',
            count : ASC.CRM.ListTaskView.CountOfRows,
            startIndex : startIndex
        };

        Teamlab.getCrmTasks({
            __startIndex: startIndex
        }, { filter: filterSettings, success: ASC.CRM.ListTaskView.CallbackMethods.get_tasks_for_entity });
    };

    var _getTasks = function(startIndex) {
        var filterSettings = ASC.CRM.ListTaskView.getFilterSettings();

        filterSettings.Count = ASC.CRM.ListTaskView.CountOfRows;
        filterSettings.startIndex = startIndex;

        Teamlab.getCrmTasks({
            __startIndex: startIndex
        }, { filter: filterSettings, success: ASC.CRM.ListTaskView.CallbackMethods.get_tasks_by_filter });
    };

    var _deleteTaskItem = function(taskID) {
        Teamlab.removeCrmTask({}, taskID,
            {
                success: ASC.CRM.ListTaskView.CallbackMethods.delete_task,
                before: function() {
                    jq("#task_" + taskID + " .check").hide();
                    jq("#task_" + taskID + " .ajax_edit_task").show();
                    jq("#taskMenu_" + taskID).hide();
                    PopupKeyUpActionProvider.EnableEsc = true;
                    jq.unblockUI();
                }
            });
    };

    var _changeTaskItemStatus = function(task_id, isClosed) {

        Teamlab.updateCrmTask({}, task_id, { id: task_id, isClosed: isClosed },
            {
                success: ASC.CRM.TaskActionView.CallbackMethods.edit_task,
                error: function (params, error) {
                    var taskErrorBox = jq("#addTaskPanel .error-popup");
                    taskErrorBox.text(error[0]);
                    taskErrorBox.removeClass("display-none").show();
                },
                before: function() {
                    jq('#taskStatusListContainer').hide();
                    jq("#task_" + task_id + " .check").hide();
                    jq("#task_" + task_id + " .ajax_edit_task").show();
                    jq("#taskMenu_" + task_id).hide();
                },
                after: function() {
                    jq("#task_" + task_id + " .ajax_edit_task").hide();
                    jq("#task_" + task_id + " .check").show();
                    jq("#taskMenu_" + task_id).show();
                }
            });

        if (isClosed) {
            var task = ASC.CRM.ListTaskView.TaskList[ASC.CRM.ListTaskView.findIndexOfTaskByID(task_id)],
                dataEvent = {
                    content    : jq.format(ASC.CRM.Resources.CRMJSResource.TaskIsOver, task.title),
                    categoryId : ASC.CRM.Data.HistoryCategorySystem.TaskClosed,
                    created    : Teamlab.serializeTimestamp(new Date())
                };

            if (task.contact != null) {
                dataEvent.contactId = task.contact.id;
            }
            if (task.entity != null) {
                dataEvent.entityId = task.entity.entityId;
                dataEvent.entityType = task.entity.entityType;
            }

            if (task.contact != null || task.entity != null) {

                var callbackMethod;
                if (ASC.CRM.ListTaskView.ContactID == 0 && ASC.CRM.ListTaskView.EntityID == 0) {
                    callbackMethod = new function(params, response) {
                    };
                } else {
                    callbackMethod = ASC.CRM.HistoryView.CallbackMethods.add_event;
                }

                Teamlab.addCrmHistoryEvent({}, dataEvent, callbackMethod);
            }
        }
    };

    var _initTaskStatusesMenu = function() {
        var $taskStatusListContainer = jq('#taskStatusListContainer');
        if ($taskStatusListContainer.length === 1) {
            jq.dropdownToggle({
                dropdownID: 'taskStatusListContainer',
                switcherSelector: '#taskTable .changeStatusCombobox.canEdit',
                addTop: 1,
                addLeft: 8,
                showFunction: function(switcherObj, dropdownItem) {
                    jq('#taskTable .changeStatusCombobox.selected').removeClass('selected');

                    if (dropdownItem.is(":hidden")) {
                        switcherObj.addClass('selected');
                        if ($taskStatusListContainer.attr('taskid') != switcherObj.attr('taskid')) {
                            $taskStatusListContainer.attr('taskid', switcherObj.attr('taskid'));
                        }
                    }
                },
                hideFunction: function() {
                    jq('#taskTable .changeStatusCombobox.selected').removeClass('selected');
                }
            });

            jq('#taskStatusListContainer li').bind({
                click: function() {
                    if (jq(this).is('.selected')) {
                        $taskStatusListContainer.hide();
                        jq('#taskTable .changeStatusCombobox.selected').removeClass('selected');
                        return;
                    }
                    var taskid = $taskStatusListContainer.attr('taskid'),
                        status = jq(this).attr('class');
                    if (status == jq('#task_' + taskid + ' .changeStatusCombobox span').attr('class')) {
                        $taskStatusListContainer.hide();
                        jq('#taskTable .changeStatusCombobox.selected').removeClass('selected');
                        return;
                    }
                    _changeTaskItemStatus(taskid, status == "closed");
                }
            });
        }
    };

    var _initChangeFilterByClickOnResponsible = function() {
        jq("#taskList").on("click", ".divForUser:not(.removedUser) span", function () {
            var resp_id = jq(this).attr("resp_id"),
                filters = [];
            filters.push({ type: "person", id: "responsibleID", isset: true, params: { id: resp_id} });
            jq("#tasksAdvansedFilter").advansedFilter({ filters: filters });
        });
    };

    var _initEmptyScreen = function () {

        //init emptyScreen for all list
        var buttonHTML = ["<a class='link dotline plus'>",
                        ASC.CRM.Resources.CRMTaskResource.CreateFirstTask,
                        "</a>"].join('');
        
        if (jq.browser.mobile != true) {
            buttonHTML += ["<br/><a class='crm-importLink link' href='tasks.aspx?action=import'>",
                        ASC.CRM.Resources.CRMTaskResource.ImportTasks,
                        "</a>"].join('');
        }
        
        jq.tmpl("emptyScrTmpl",
            {
                ID: "tasksEmptyScreen",
                ImgSrc: ASC.CRM.Data.EmptyScrImgs["empty_screen_tasks"],
                Header: ASC.CRM.Resources.CRMTaskResource.EmptyContentTasksHeader,
                Describe: jq.format(ASC.CRM.Resources.CRMTaskResource.EmptyContentTasksDescribe,
                    "<span class='hintCategories baseLinkAction'>", "</span>"),
                ButtonHTML: buttonHTML,
                CssClass: "display-none"
            }).insertAfter("#taskFilterContainer");

        //init emptyScreen for filter
        jq.tmpl("emptyScrTmpl",
            {
                ID: "emptyContentForTasksFilter",
                ImgSrc: ASC.CRM.Data.EmptyScrImgs["empty_screen_filter"],
                Header: ASC.CRM.Resources.CRMTaskResource.EmptyContentTasksFilterHeader,
                Describe: ASC.CRM.Resources.CRMTaskResource.EmptyContentTasksFilterDescribe,
                ButtonHTML: ["<a class='clearFilterButton link dotline' href='javascript:void(0);' onclick='ASC.CRM.ListTaskView.advansedFilter.advansedFilter(null);'>",
                    ASC.CRM.Resources.CRMCommonResource.ClearFilter,
                    "</a>"].join(''),
                CssClass: "display-none"
            }).insertAfter("#taskFilterContainer");
    };
    
    var _initFilter = function () {
        if (!jq("#tasksAdvansedFilter").advansedFilter) return;

        var tmpDate = new Date(),

            today = new Date(tmpDate.getFullYear(), tmpDate.getMonth(), tmpDate.getDate(), 0, 0, 0, 0),
            yesterday = new Date(new Date(today).setDate(tmpDate.getDate() - 1)),
            tomorrow = new Date(new Date(today).setDate(tmpDate.getDate() + 1)),

            todayString = Teamlab.serializeTimestamp(today),
            yesterdayString = Teamlab.serializeTimestamp(yesterday),
            tomorrowString = Teamlab.serializeTimestamp(tomorrow);

        ASC.CRM.ListTaskView.advansedFilter = jq("#tasksAdvansedFilter")
            .advansedFilter({
                anykey      : false,
                hint        : ASC.CRM.Resources.CRMCommonResource.AdvansedFilterInfoText.format(
                            '<b>',
                            '</b>',
                            '<br/><br/><a href="' + ASC.Resources.Master.FilterHelpCenterLink + '" target="_blank">',
                            '</a>'),
                maxfilters  : 3,
                colcount    : 2,
                maxlength   : "100",
                store       : true,
                inhash      : true,
                filters     : [
                            {
                                type        : "person",
                                id          : "my",
                                apiparamname: "responsibleID",
                                title       : ASC.CRM.Resources.CRMTaskResource.MyTasksFilter,
                                filtertitle : ASC.CRM.Resources.CRMCommonResource.FilterByResponsible,
                                group       : ASC.CRM.Resources.CRMCommonResource.FilterByResponsible,
                                groupby     : "responsible",
                                bydefault   : { id: Teamlab.profile.id, value: Teamlab.profile.id }
                            },
                            {
                                type        : "person",
                                id          : "responsibleID",
                                apiparamname: "responsibleID",
                                title       : ASC.CRM.Resources.CRMTaskResource.CustomResponsibleFilter,
                                filtertitle : ASC.CRM.Resources.CRMCommonResource.FilterByResponsible,
                                group       : ASC.CRM.Resources.CRMCommonResource.FilterByResponsible,
                                groupby     : "responsible"
                            },

                            {
                                type        : "combobox",
                                id          : "overdue",
                                apiparamname: jq.toJSON(["fromDate", "toDate"]),
                                title       : ASC.CRM.Resources.CRMTaskResource.OverdueTasksFilter,
                                filtertitle : ASC.CRM.Resources.CRMTaskResource.ByDueDate,
                                group       : ASC.CRM.Resources.CRMTaskResource.ByDueDate,
                                groupby     : "deadline",
                                options     :
                                        [
                                        { value: jq.toJSON(["", yesterdayString]), classname: '', title: ASC.CRM.Resources.CRMTaskResource.OverdueTasksFilter, def: true },
                                        { value: jq.toJSON([todayString, todayString]), classname: '', title: ASC.CRM.Resources.CRMTaskResource.TodayTasksFilter },
                                        { value: jq.toJSON([tomorrowString, ""]), classname: '', title: ASC.CRM.Resources.CRMTaskResource.TheNextTasksFilter }
                                        ]
                            },
                            {
                                type        : "combobox",
                                id          : "today",
                                apiparamname: jq.toJSON(["fromDate", "toDate"]),
                                title       : ASC.CRM.Resources.CRMTaskResource.TodayTasksFilter,
                                filtertitle : ASC.CRM.Resources.CRMTaskResource.ByDueDate,
                                group       : ASC.CRM.Resources.CRMTaskResource.ByDueDate,
                                groupby     : "deadline",
                                options     :
                                        [
                                        { value: jq.toJSON(["", yesterdayString]), classname: '', title: ASC.CRM.Resources.CRMTaskResource.OverdueTasksFilter },
                                        { value: jq.toJSON([todayString, todayString]), classname: '', title: ASC.CRM.Resources.CRMTaskResource.TodayTasksFilter, def: true },
                                        { value: jq.toJSON([tomorrowString, ""]), classname: '', title: ASC.CRM.Resources.CRMTaskResource.TheNextTasksFilter }
                                        ]
                            },
                            {
                                type        : "combobox",
                                id          : "theNext",
                                apiparamname: jq.toJSON(["fromDate", "toDate"]),
                                title       : ASC.CRM.Resources.CRMTaskResource.TheNextTasksFilter,
                                filtertitle : ASC.CRM.Resources.CRMTaskResource.ByDueDate,
                                group       : ASC.CRM.Resources.CRMTaskResource.ByDueDate,
                                groupby     : "deadline",
                                options     :
                                        [
                                        { value: jq.toJSON(["", yesterdayString]), classname: '', title: ASC.CRM.Resources.CRMTaskResource.OverdueTasksFilter },
                                        { value: jq.toJSON([todayString, todayString]), classname: '', title: ASC.CRM.Resources.CRMTaskResource.TodayTasksFilter },
                                        { value: jq.toJSON([tomorrowString, ""]), classname: '', title: ASC.CRM.Resources.CRMTaskResource.TheNextTasksFilter, def: true }
                                        ]
                            },
                            {
                                type        : "daterange",
                                id          : "fromToDate",
                                title       : ASC.CRM.Resources.CRMTaskResource.CustomDateFilter,
                                filtertitle : ASC.CRM.Resources.CRMTaskResource.ByDueDate,
                                group       : ASC.CRM.Resources.CRMTaskResource.ByDueDate,
                                groupby     : "deadline"
                            },
                            {
                                type        : "combobox",
                                id          : "openTask",
                                apiparamname: "isClosed",
                                title       : ASC.CRM.Resources.CRMTaskResource.OnlyOpenTasks,
                                filtertitle : ASC.CRM.Resources.CRMTaskResource.TasksByStatus,
                                group       : ASC.CRM.Resources.CRMTaskResource.TasksByStatus,
                                groupby     : "taskStatus",
                                options     :
                                        [
                                        { value: false, classname: '', title: ASC.CRM.Resources.CRMTaskResource.OnlyOpenTasks, def: true },
                                        { value: true, classname: '', title: ASC.CRM.Resources.CRMTaskResource.OnlyClosedTasks }
                                        ]
                            },
                            {
                                type        : "combobox",
                                id          : "closedTask",
                                apiparamname: "isClosed",
                                title       : ASC.CRM.Resources.CRMTaskResource.OnlyClosedTasks,
                                filtertitle : ASC.CRM.Resources.CRMTaskResource.TasksByStatus,
                                group       : ASC.CRM.Resources.CRMTaskResource.TasksByStatus,
                                groupby     : "taskStatus",
                                options     :
                                        [
                                        { value: false, classname: '', title: ASC.CRM.Resources.CRMTaskResource.OnlyOpenTasks },
                                        { value: true, classname: '', title: ASC.CRM.Resources.CRMTaskResource.OnlyClosedTasks, def: true }
                                        ]
                            },
                            {
                                type        : "combobox",
                                id          : "categoryID",
                                apiparamname: "categoryID",
                                title       : ASC.CRM.Resources.CRMCommonResource.ByCategory,
                                group       : ASC.CRM.Resources.CRMCommonResource.Other,
                                options     : ASC.CRM.Data.taskCategories,
                                defaulttitle: ASC.CRM.Resources.CRMCommonResource.Choose
                            },
                            {
                                type        : ASC.CRM.myTaskContactFilter.type,
                                id          : ASC.CRM.myTaskContactFilter.idFilterByContact,
                                apiparamname: jq.toJSON(["entityid", "entityType"]),
                                title       : ASC.CRM.Resources.CRMContactResource.Contact,
                                group       : ASC.CRM.Resources.CRMCommonResource.Other,
                                hashmask    : '',
                                create      : ASC.CRM.myTaskContactFilter.createFilterByContact,
                                customize   : ASC.CRM.myTaskContactFilter.customizeFilterByContact,
                                destroy     : ASC.CRM.myTaskContactFilter.destroyFilterByContact,
                                process     : ASC.CRM.myTaskContactFilter.processFilter
                            }
                ],

                sorters: [
                            { id: "title", title: ASC.CRM.Resources.CRMCommonResource.Title, dsc: false, def: false },
                            { id: "category", title: ASC.CRM.Resources.CRMCommonResource.Category, dsc: false, def: false },
                            { id: "deadline", title: ASC.CRM.Resources.CRMTaskResource.DueDate, dsc: false, def: true },
                            { id: "contactmanager", title: ASC.CRM.Resources.CRMCommonResource.Responsible, dsc: false, def: false },
                            { id: "contact", title: ASC.CRM.Resources.CRMContactResource.Contact, dsc: false, def: false }
                ]
            })
            .bind("setfilter", ASC.CRM.ListTaskView.setFilter)
            .bind("resetfilter", ASC.CRM.ListTaskView.resetFilter);
    };

    return {
        CallbackMethods: {
            get_tasks_by_filter: function(params, tasks) {

                ASC.CRM.ListTaskView.TaskList = [];
                ASC.CRM.ListTaskView.Total = params.__total || 0;
                var startIndex = params.__startIndex || 0;


                for (var i = 0, n = tasks.length; i < n; i++) {
                    ASC.CRM.ListTaskView.taskItemFactory(tasks[i]);
                    ASC.CRM.ListTaskView.TaskList.push(tasks[i]);
                }

                if (ASC.CRM.ListTaskView.Total === 0 && typeof (ASC.CRM.ListTaskView.advansedFilter) != "undefined" &&
                            ASC.CRM.ListTaskView.advansedFilter.advansedFilter().length == 1) {
                    ASC.CRM.ListTaskView.noTasks = true;
                    ASC.CRM.ListTaskView.noTasksForQuery = true;
                } else {
                    ASC.CRM.ListTaskView.noTasks = false;
                    if (ASC.CRM.ListTaskView.Total === 0) {
                        ASC.CRM.ListTaskView.noTasksForQuery = true;
                    } else {
                        ASC.CRM.ListTaskView.noTasksForQuery = false;
                    }
                }

                if (ASC.CRM.ListTaskView.noTasks) {
                    _renderNoTasksEmptyScreen();
                    LoadingBanner.hideLoading();
                    return false;
                }

                if (ASC.CRM.ListTaskView.noTasksForQuery) {
                    _renderNoTasksForQueryEmptyScreen();

                    jq("#taskFilterContainer").show();
                    ASC.CRM.ListTaskView.resizeFilter();

                    LoadingBanner.hideLoading();
                    return false;
                }

                if (tasks.length == 0) {//it can happen when select page without elements after deleting
                    jq("tasksEmptyScreen:not(.display-none)").addClass("display-none");
                    jq("#emptyContentForTasksFilter:not(.display-none)").addClass("display-none");
                    jq("#taskTable tbody tr").remove();
                    jq("#tableForTasksNavigation").show();
                    ASC.CRM.Common.hideExportButtons();
                    LoadingBanner.hideLoading();
                    return false;
                }

                jq("#totalTasksOnPage").text(ASC.CRM.ListTaskView.Total);
                jq("#emptyContentForTasksFilter:not(.display-none)").addClass("display-none");
                jq("#tasksEmptyScreen:not(.display-none)").addClass("display-none");
                ASC.CRM.Common.showExportButtons();

                jq("#taskFilterContainer").show();
                ASC.CRM.ListTaskView.resizeFilter();

                jq("#taskTable tbody").replaceWith(jq.tmpl("taskListTmpl", { tasks: ASC.CRM.ListTaskView.TaskList }));
                jq("#taskList").show();

                ASC.CRM.Common.RegisterContactInfoCard();

                for (var i = 0, n = ASC.CRM.ListTaskView.TaskList.length; i < n; i++) {
                    ASC.CRM.Common.tooltip("#taskTitle_" + ASC.CRM.ListTaskView.TaskList[i].id, "tooltip");
                }

                _renderTaskPageNavigator(startIndex);

                 window.scrollTo(0, 0);
                LoadingBanner.hideLoading();
            },

            get_tasks_for_entity: function (params, tasks) {

                ASC.CRM.ListTaskView.TaskList = [];
                ASC.CRM.ListTaskView.Total = params.__total || 0;
                var startIndex = params.__startIndex || 0;

                for (var i = 0, n = tasks.length; i < n; i++) {
                    ASC.CRM.ListTaskView.taskItemFactory(tasks[i]);
                    ASC.CRM.ListTaskView.TaskList.push(tasks[i]);
                }

                ASC.CRM.ListTaskView.noTasks = ASC.CRM.ListTaskView.Total === 0;
  
                if (ASC.CRM.ListTaskView.noTasks) {
                    _renderNoTasksEmptyScreen();
                    LoadingBanner.hideLoading();
                    return false;
                }

                if (tasks.length == 0) {//it can happen when select page without elements after deleting
                    jq("tasksEmptyScreen:not(.display-none)").addClass("display-none");
                    jq("#taskTable tbody tr").remove();
                    jq("#tableForTasksNavigation").show();
                    ASC.CRM.Common.hideExportButtons();
                    LoadingBanner.hideLoading();
                    return false;
                }

                jq("#totalTasksOnPage").text(ASC.CRM.ListTaskView.Total);
                jq("#tasksEmptyScreen:not(.display-none)").addClass("display-none");
                ASC.CRM.Common.showExportButtons();

                jq("#taskList").show();
                jq("#taskTable tbody").replaceWith(jq.tmpl("taskListTmpl", { tasks: ASC.CRM.ListTaskView.TaskList }));
                ASC.CRM.Common.RegisterContactInfoCard();

                for (var i = 0, n = ASC.CRM.ListTaskView.TaskList.length; i < n; i++) {
                    ASC.CRM.Common.tooltip("#taskTitle_" + ASC.CRM.ListTaskView.TaskList[i].id, "tooltip");
                }

                _renderTaskPageNavigator(startIndex);

                window.scrollTo(0, 0);
                LoadingBanner.hideLoading();
            },

            delete_task: function(params, task) {
                jq("#task_" + task.id).animate({ opacity: "hide" }, 500);
                //ASC.CRM.Common.changeCountInTab("delete", "tasks");

                setTimeout(function() {
                    jq("#task_" + task.id).remove();
                    ASC.CRM.ListTaskView.Total -= 1;
                    jq("#totalTasksOnPage").text(ASC.CRM.ListTaskView.Total);

                    var index = ASC.CRM.ListTaskView.findIndexOfTaskByID(task.id);
                    if (index != -1) {
                        ASC.CRM.ListTaskView.TaskList.splice(index, 1);
                    }


                    if (ASC.CRM.ListTaskView.Total == 0
                            && (typeof (ASC.CRM.ListTaskView.advansedFilter) == "undefined"
                            || ASC.CRM.ListTaskView.advansedFilter.advansedFilter().length == 1)) {
                        ASC.CRM.ListTaskView.noTasks = true;
                        ASC.CRM.ListTaskView.noTasksForQuery = true;
                    } else {
                        ASC.CRM.ListTaskView.noTasks = false;
                        if (ASC.CRM.ListTaskView.Total === 0) {
                            ASC.CRM.ListTaskView.noTasksForQuery = true;
                        } else {
                            ASC.CRM.ListTaskView.noTasksForQuery = false;
                        }
                    }

                    PopupKeyUpActionProvider.EnableEsc = true;
                    if (ASC.CRM.ListTaskView.noTasks) {
                        _renderNoTasksEmptyScreen();
                        jq.unblockUI();
                        return;
                    }

                    if (ASC.CRM.ListTaskView.noTasksForQuery) {
                        _renderNoTasksForQueryEmptyScreen();
                        jq.unblockUI();
                        return;
                    }

                    if (jq("#taskTable tbody tr").length == 0) {
                        jq.unblockUI();

                        var startIndex = ASC.CRM.ListTaskView.CountOfRows * (window.taskPageNavigator.CurrentPageNumber - 1);
                        while (startIndex >= ASC.CRM.ListTaskView.Total && startIndex >= ASC.CRM.ListTaskView.CountOfRows) {
                            startIndex -= ASC.CRM.ListTaskView.CountOfRows;
                        }
                        _renderContent(startIndex);
                    } else {
                        jq.unblockUI();
                    }
                }, 500);
            }
        },

        init: function (exportErrorCookieKey, parentSelector) {
            if (jq(parentSelector).length == 0) return;
            ASC.CRM.Common.setDocumentTitle(ASC.CRM.Resources.CRMTaskResource.Tasks);
            jq(parentSelector).removeClass("display-none");

            jq.tmpl("tasksListBaseTmpl", {}).appendTo(parentSelector);

            jq('#privatePanelWrapper').appendTo("#permissionsDealsPanelInnerHtml");

            ASC.CRM.ListTaskView.ContactID = 0;
            ASC.CRM.ListTaskView.EntityType = "";
            ASC.CRM.ListTaskView.EntityID = 0;

            ASC.CRM.ListTaskView.cookieKey = ASC.CRM.Data.CookieKeyForPagination["tasks"];
            ASC.CRM.ListTaskView.TaskList = [];

            ASC.CRM.ListTaskView.isFilterVisible = false;
            ASC.CRM.ListTaskView.isTabActive = false;
            ASC.CRM.ListTaskView.Total = 0;
            ASC.CRM.ListTaskView.advansedFilter = null;

            var exportErrorText = jq.cookies.get(exportErrorCookieKey)
            if (exportErrorText != null && exportErrorText != "") {
                jq.cookies.del(exportErrorCookieKey);
                jq.tmpl("blockUIPanelTemplate", {
                    id: "exportToCsvError",
                    headerTest: ASC.CRM.Resources.CRMCommonResource.Alert,
                    questionText: "",
                    innerHtmlText: ['<div>', exportErrorText, '</div>'].join(''),
                    CancelBtn: ASC.CRM.Resources.CRMCommonResource.Close,
                    progressText: ""
                }).insertAfter("#taskFilterContainer");

                PopupKeyUpActionProvider.EnableEsc = false;
                StudioBlockUIManager.blockUI("#exportToCsvError", 500, 400, 0);
            }

            _initEmptyScreen();

            ASC.CRM.ListTaskView.actionMenuPositionCalculated = false;
            jq.tmpl("taskExtendedListTmpl", { IsTab: false }).insertAfter("#taskFilterContainer");

            var settings = {
                page: 1,
                countOnPage: jq("#tableForTaskNavigation select:first>option:first").val()
            },
                key = location.protocol + '//' + location.hostname + (location.port ? ':' + location.port : '') + location.pathname + location.search,
                currentAnchor = location.hash,
                cookieKey = encodeURIComponent(key);

            currentAnchor = currentAnchor && typeof currentAnchor === 'string' && currentAnchor.charAt(0) === '#'
                ? currentAnchor.substring(1)
                : currentAnchor;

            var cookieAnchor = jq.cookies.get(cookieKey);
            if (currentAnchor == "" || cookieAnchor == currentAnchor) {
                var tmp = ASC.CRM.Common.getPagingParamsFromCookie(ASC.CRM.ListTaskView.cookieKey);
                if (tmp != null) {
                    settings = tmp;
                }
            } else {
                _setCookie(settings.page, settings.countOnPage);
            }

            ASC.CRM.ListTaskView.CountOfRows = settings.countOnPage;
            ASC.CRM.ListTaskView.defaultCurrentPageNumber = settings.page;

            jq("#tableForTaskNavigation select:first").val(ASC.CRM.ListTaskView.CountOfRows)
                    .change(function (evt) {
                        ASC.CRM.ListTaskView.changeCountOfRows(this.value, false);
                    })
                    .tlCombobox();

            _initTaskStatusesMenu();

            jq("#menuCreateNewTask").on("click", function () { ASC.CRM.TaskActionView.showTaskPanel(0, "", 0, null, {}); });
            jq("#tasksEmptyScreen .emptyScrBttnPnl>a.link.plus").on("click", function () { ASC.CRM.TaskActionView.showTaskPanel(0, "", 0, null, {}); });

            _initChangeFilterByClickOnResponsible();
            _initPageNavigatorControl(false, ASC.CRM.ListTaskView.CountOfRows, ASC.CRM.ListTaskView.defaultCurrentPageNumber);

            ASC.CRM.Common.registerChangeHoverStateByParent("label.task_category", "#taskTable .with-entity-menu");

            jq("#" + ASC.CRM.myTaskContactFilter.headerContainerId).contactadvancedSelector(
            {
                showme: true,
                addtext: ASC.CRM.Resources.CRMContactResource.AddNewCompany,
                noresults: ASC.CRM.Resources.CRMCommonResource.NoMatches,
                noitems: ASC.CRM.Resources.CRMCommonResource.NoMatches,
                inPopup: true,
                onechosen: true
            });

            jq(window).bind("afterResetSelectedContact", function (event, obj, objName) {
                if (objName === "taskContactSelectorForFilter" && ASC.CRM.myTaskContactFilter.filterId) {
                    jq('#' + ASC.CRM.myTaskContactFilter.filterId).advansedFilter('resize');
                }
            });

            _initFilter();

            ASC.CRM.ListTaskView.advansedFilter.one("adv-ready", function () {
                var crmAdvansedFilterContainer = jq("#tasksAdvansedFilter .advansed-filter-list");
                crmAdvansedFilterContainer.find("li[data-id='my'] .inner-text").trackEvent(ga_Categories.tasks, ga_Actions.filterClick, 'me_manager');
                crmAdvansedFilterContainer.find("li[data-id='responsibleID'] .inner-text").trackEvent(ga_Categories.tasks, ga_Actions.filterClick, 'custom_manager');
                crmAdvansedFilterContainer.find("li[data-id='openTask'] .inner-text").trackEvent(ga_Categories.tasks, ga_Actions.filterClick, 'open_task');
                crmAdvansedFilterContainer.find("li[data-id='closedTask'] .inner-text").trackEvent(ga_Categories.tasks, ga_Actions.filterClick, 'closed_task');
                crmAdvansedFilterContainer.find("li[data-id='overdue'] .inner-text").trackEvent(ga_Categories.tasks, ga_Actions.filterClick, 'overdue');
                crmAdvansedFilterContainer.find("li[data-id='today'] .inner-text").trackEvent(ga_Categories.tasks, ga_Actions.filterClick, 'today');
                crmAdvansedFilterContainer.find("li[data-id='theNext'] .inner-text").trackEvent(ga_Categories.tasks, ga_Actions.filterClick, 'the_next');
                crmAdvansedFilterContainer.find("li[data-id='fromToDate'] .inner-text").trackEvent(ga_Categories.tasks, ga_Actions.filterClick, 'from_to_date');
                crmAdvansedFilterContainer.find("li[data-id='categoryID'] .inner-text").trackEvent(ga_Categories.tasks, ga_Actions.filterClick, 'by_category');
                crmAdvansedFilterContainer.find("li[data-id='contactID'] .inner-text").trackEvent(ga_Categories.tasks, ga_Actions.filterClick, 'contact');

                jq("#tasksAdvansedFilter .btn-toggle-sorter").trackEvent(ga_Categories.tasks, ga_Actions.filterClick, 'sort');
                jq("#tasksAdvansedFilter .advansed-filter-input").trackEvent(ga_Categories.tasks, ga_Actions.filterClick, "search_text", "enter");
            });
        },

        initTab: function(ContactID, EntityType, EntityID) {
            ASC.CRM.ListTaskView.ContactID = ContactID;
            ASC.CRM.ListTaskView.EntityType = EntityType;
            ASC.CRM.ListTaskView.EntityID = EntityID;

            ASC.CRM.ListTaskView.CountOfRows = ASC.CRM.Data.DefaultEntryCountOnPage;

            ASC.CRM.ListTaskView.TaskList = [];

            ASC.CRM.ListTaskView.isFilterVisible = false;
            ASC.CRM.ListTaskView.isTabActive = false;

           // ASC.CRM.ListTaskView.isFirstTime = true;
            ASC.CRM.ListTaskView.actionMenuPositionCalculated = false;

            jq.tmpl("emptyScrTmpl",
                {   ID: "tasksEmptyScreen",
                    ImgSrc: ASC.CRM.Data.EmptyScrImgs["empty_screen_tasks"],
                    Header: ASC.CRM.Resources.CRMTaskResource.EmptyContentTasksHeader,
                    Describe: jq.format(ASC.CRM.Resources.CRMTaskResource.EmptyContentTasksDescribe,
                                    "<span class='hintCategories baseLinkAction' >", "</span>"),
                    ButtonHTML: ["<a class='link dotline plus'>",
                            ASC.CRM.Resources.CRMTaskResource.CreateTask,
                            "</a>"
                            ].join(''),
                    CssClass: "display-none"
                }).insertAfter("#taskListTab");
            jq.tmpl("taskExtendedListTmpl", { IsTab: true }).appendTo("#taskListTab");
            jq("#tableForTaskNavigation select").val(ASC.CRM.ListTaskView.CountOfRows)
                    .change(function (evt) {
                        ASC.CRM.ListTaskView.changeCountOfRows(this.value, true);
                    })
                    .tlCombobox();

            _initTaskStatusesMenu();
            _initPageNavigatorControl(true, ASC.CRM.ListTaskView.CountOfRows, 0);

            ASC.CRM.Common.registerChangeHoverStateByParent("label.task_category", "#taskTable .with-entity-menu");
        },

        bindEmptyScrBtnEvent: function(params) {
            jq("#tasksEmptyScreen .emptyScrBttnPnl>a.link.plus").bind("click", function() {
                ASC.CRM.TaskActionView.showTaskPanel(0, ASC.CRM.ListTaskView.EntityType, ASC.CRM.ListTaskView.EntityID, window.contactForInitTaskActionPanel, params);
            });
        },

        setFilter: function (evt, $container, filter, params, selectedfilters) { _changeFilter(); },
        resetFilter: function (evt, $container, filter, selectedfilters) { _changeFilter(); },

        activate: function() {
            if (ASC.CRM.ListTaskView.isTabActive == false) {
                ASC.CRM.ListTaskView.isTabActive = true;
                _getTasksForEntity(0);
            }
        },

        resizeFilter: function() {
            var visible = jq("#taskFilterContainer").is(":hidden") == false;
            if (ASC.CRM.ListTaskView.isFilterVisible == false && visible) {
                ASC.CRM.ListTaskView.isFilterVisible = true;
                if (ASC.CRM.ListTaskView.advansedFilter) {
                    jq("#tasksAdvansedFilter").advansedFilter("resize");
                }
            }
        },

        getFilterSettings: function() {
            var settings = {};
            if (ASC.CRM.ListTaskView.advansedFilter.advansedFilter == null) {
                return settings;
            }

            var param = ASC.CRM.ListTaskView.advansedFilter.advansedFilter();

            jq(param).each(function(i, item) {
                switch (item.id) {
                    case "sorter":
                        settings.sortBy = item.params.id;
                        settings.sortOrder = item.params.dsc == true ? 'descending' : 'ascending';
                        break;
                    case "text":
                        settings.filterValue = item.params.value;
                        break;
                    case "fromToDate":
                        settings.fromDate = new Date(item.params.from);
                        settings.toDate = new Date(item.params.to);
                        break;
                    default:
                        if (item.hasOwnProperty("apiparamname") && item.params.hasOwnProperty("value") && item.params.value != null) {
                            try {
                                var apiparamnames = jq.parseJSON(item.apiparamname),
                                    apiparamvalues = jq.parseJSON(item.params.value);
                                if (apiparamnames.length != apiparamvalues.length) {
                                    settings[item.apiparamname] = item.params.value;
                                }
                                for (var i = 0, len = apiparamnames.length; i < len; i++) {
                                    if (typeof (apiparamvalues[i]) != "string" || apiparamvalues[i].trim().length != 0) {
                                        settings[apiparamnames[i]] = apiparamvalues[i];
                                    }
                                }
                            } catch (err) {
                                settings[item.apiparamname] = item.params.value;
                            }
                        }
                        break;
                }
            });
            return settings;
        },

        taskItemFactory: function(taskItem) {
            var nowDate = new Date(),
                todayDate = new Date(nowDate.getFullYear(), nowDate.getMonth(), nowDate.getDate(), 0, 0, 0, 0);

            if (taskItem.isClosed) {
                taskItem.classForTitle = "header-base-small gray-text";
                taskItem.classForTaskDeadline = "gray-text";
            } else {
                if (taskItem.deadLine.getHours() != 0 || taskItem.deadLine.getMinutes() != 0) {
                    if (taskItem.deadLine.getTime() < nowDate.getTime()) {
                        taskItem.classForTitle = "header-base-small red-text";
                        taskItem.classForTaskDeadline = "red-text";
                    } else {
                        taskItem.classForTitle = "header-base-small";
                        taskItem.classForTaskDeadline = "";
                    }
                } else {
                    if (taskItem.deadLine.getTime() < todayDate.getTime()) {
                        taskItem.classForTitle = "header-base-small red-text";
                        taskItem.classForTaskDeadline = "red-text";
                    } else {
                        taskItem.classForTitle = "header-base-small";
                        taskItem.classForTaskDeadline = "";
                    }
                }
            }

            if (taskItem.entity != null) {
                switch (taskItem.entity.entityType) {
                    case "opportunity":
                        taskItem.entityURL = "deals.aspx?id=" + taskItem.entity.entityId;
                        taskItem.entityType = ASC.CRM.Resources.CRMJSResource.Deal;
                        break;
                    case "case":
                        taskItem.entityURL = "cases.aspx?id=" + taskItem.entity.entityId;
                        taskItem.entityType = ASC.CRM.Resources.CRMJSResource.Case;
                        break;
                    default:
                        taskItem.entityURL = "";
                        taskItem.entityType = "";
                        break;
                }
            }
            taskItem.category.cssClass = taskItem.category.imagePath.split('/')[taskItem.category.imagePath.split('/').length - 1].split('.')[0];

            if (!ASC.CRM.Common.isUserActive(taskItem.responsible.id)) {
                taskItem.responsible.displayName = ASC.CRM.Data.ProfileRemoved;
                taskItem.responsible.activationStatus = 2;
            }

        },

        showConfirmationPanelForDelete: function(taskID) {
            jq("#confirmationDeleteOneTaskPanel .middle-button-container>.button.blue.middle").unbind("click").bind("click", function () {
                _deleteTaskItem(taskID);
            });
            PopupKeyUpActionProvider.EnableEsc = false;
            StudioBlockUIManager.blockUI("#confirmationDeleteOneTaskPanel", 500, 500, 0);
        },

        findIndexOfTaskByID: function(taskID) {
            var length = ASC.CRM.ListTaskView.TaskList.length;
            for (var i = 0; i < length; i++) {
                if (ASC.CRM.ListTaskView.TaskList[i].id == taskID) {
                    return i;
                }
            }
            return -1;
        },

        showActionMenu: function (taskID, contactID, contactDisplayName, entityType, entityID, isEmailCategory, email) {
            contactDisplayName = jq.base64.decode(contactDisplayName);
            if (ASC.CRM.ListTaskView.actionMenuPositionCalculated === false) {
                ASC.CRM.ListTaskView.actionMenuPositionCalculated = true;
                jq.dropdownToggle({
                    dropdownID: "taskActionMenu",
                    switcherSelector: "#taskTable .entity-menu",
                    addTop: -2,
                    addLeft: 10,
                    rightPos: true,
                    showFunction: function(switcherObj, dropdownItem) {
                        jq("#taskTable .entity-menu.active").removeClass("active");
                        if (dropdownItem.is(":hidden")) {
                            switcherObj.addClass("active");
                        }
                    },
                    hideFunction: function() {
                        jq("#taskTable .entity-menu.active").removeClass("active");
                    }
                });
            }
            jq("#editTaskLink").unbind("click").bind("click", function() {
                jq("#taskActionMenu").hide();
                jq("#taskTable .entity-menu.active").removeClass("active");
                ASC.CRM.TaskActionView.showTaskPanel(taskID, entityType, entityID, window.contactForInitTaskActionPanel, {});
            });
            jq("#deleteTaskLink").unbind("click").bind("click", function() {
                jq("#taskActionMenu").hide();
                jq("#taskTable .entity-menu.active").removeClass("active");
                ASC.CRM.ListTaskView.showConfirmationPanelForDelete(taskID);
            });

            if (isEmailCategory) {
                jq("#sendEmailLink").removeClass("display-none");
                
                var basePathMail = ASC.CRM.Common.getMailModuleBasePath(),
                    pathCreateEmail = "";

                if (email != '') {
                    pathCreateEmail = [
                        basePathMail,
                        "#composeto/email='",
                        contactDisplayName,
                        "' <",
                        email,
                        ">"
                    ].join('');
                } else {
                    pathCreateEmail = [
                        basePathMail,
                        "#composeto/"
                    ].join('');
                }
                jq("#sendEmailLink").attr("href", pathCreateEmail);
            } else {
                jq("#sendEmailLink").addClass("display-none");
            }
        },

        changeCountOfRows: function(newValue, isTab) {
            if (isNaN(newValue)) return;
            var newCountOfRows = newValue * 1;
            ASC.CRM.ListTaskView.CountOfRows = newCountOfRows;
            taskPageNavigator.EntryCountOnPage = newCountOfRows;
            if (isTab == true) {
                _getTasksForEntity(0);
            } else {
                _setCookie(1, newCountOfRows);
                _renderContent(0);
            }
        }
    };
};

/*******************************************************************************
TaskActionView.ascx
*******************************************************************************/
ASC.CRM.TaskActionView = new function() {

    var _changeSelectionDeadlineButtons = function(daysCount) {
        jq("#taskDeadlineContainer").find("a").each(function() {
            jq(this).css("border-bottom", "");
            jq(this).css("font-weight", "normal");
        });

        if (daysCount == 0 || daysCount == 3 || daysCount == 7) {
            jq("#deadline_" + daysCount).css("border-bottom", "none");
            jq("#deadline_" + daysCount).css("font-weight", "bold");
        }
    };

    var _initTaskContactSelector = function (contact) {
        window.taskContactSelector.ShowAddButton = false;
        window.taskContactSelector.clearSelector();

        if (typeof (contact) === "object" && contact != null && contact.length != 0) {
            if (!ASC.CRM.Common.isArray(contact)) {
                window.taskContactSelector.setContact(jq("#contactTitle_taskContactSelector_0"), contact.id, contact.displayName, contact.smallFotoUrl);
                window.taskContactSelector.showInfoContent(jq("#contactTitle_taskContactSelector_0"));
            } else {
                window.taskContactSelector.setContact(jq("#contactTitle_taskContactSelector_0"), contact[0].id, contact[0].displayName, contact[0].smallFotoUrl);
                window.taskContactSelector.showInfoContent(jq("#contactTitle_taskContactSelector_0"));
                for (var i = 1, n = contact.length; i < n; i++) {
                    window.taskContactSelector.AddNewSelectorWithSelectedContact(contact[i]);
                    window.taskContactSelector.SelectedContacts.push(contact[i].id);
                }
            }
        } else {
            window.taskContactSelector.changeContact('taskContactSelector_0');
            window.taskContactSelector.crossButtonEventClick('taskContactSelector_0');
        }
    };

    var _initTaskResponsibleSelector = function(contact, task) {

        if (typeof (task) === "object" && task != null) {
            _initTaskResponsibleByResponsible(task.responsible);
        } else if (typeof (contact) === "object" && contact != null) {
            if (contact.isPrivate) {
                _initTaskResponsibleByContact(contact);
            } else {
                _clearTaskResponsible();
            }
        } else {
            _clearTaskResponsible();
        }
    };


    var _initTaskResponsibleByContact = function(contact) {
        jq("#taskActionViewAdvUsrSrContainer").useradvancedSelector("reset");

        var allUsers = [],
            accessList = [],
            items = jq("#taskActionViewAdvUsrSrContainer").data("items") || [];

        for (var i = 0, n = items.length; i < n; i++) {
            allUsers.push(items[i].id);
        }

        for (var i = 0, n = contact.accessList.length; i < n; i++) {
            accessList.push(contact.accessList[i].id);
        }

        for (var i = 0, n = ASC.CRM.Data.crmAdminList.length; i < n; i++) {
            accessList.push(ASC.CRM.Data.crmAdminList[i].id);
        }

        for (var i = 0, n = allUsers.length; i < n; i++) {
            var hasAccess = false;
            for (var j = 0, m = accessList.length; j < m; j++) {
                if (allUsers[i] == accessList[j]) {
                    hasAccess = true;
                    break;
                }
            }
            if (!hasAccess) {
                jq("#taskActionViewAdvUsrSrContainer").useradvancedSelector("disable", allUsers[i]);
            }
        }
    };

    var _clearTaskResponsible = function () {
        jq("#taskActionViewAdvUsrSrContainer").useradvancedSelector("reset");
        jq("#taskActionViewAdvUsrSrContainer").useradvancedSelector("select", [Teamlab.profile.id]);
    };

    var _initTaskResponsibleByResponsible = function (responsible) {
        jq("#taskActionViewAdvUsrSrContainer").useradvancedSelector("reset");
        if (responsible.activationStatus != 2) {
            jq("#taskActionViewAdvUsrSrContainer").useradvancedSelector("select", [responsible.id]);
        } else {
            jq("#taskActionViewAdvUsrSrContainer .taskResponsibleLabel").text(responsible.displayName);
            jq("#taskActionViewAdvUsrSrContainer").attr("data-responsible-id", "");
        }
    };

    var _initTaskCategorySelector = function () {
        var selectedCategory = {};
        if (ASC.CRM.Data.taskActionViewCategories.length > 0) {
            selectedCategory = ASC.CRM.Data.taskActionViewCategories[0];
        } else {
            selectedCategory = { id: 0, title: "", imgSrc: "" };
        }
        window.taskCategorySelector = new ASC.CRM.CategorySelector("taskCategorySelector", selectedCategory);
        taskCategorySelector.renderControl(ASC.CRM.Data.taskActionViewCategories, selectedCategory, "#taskCategorySelectorContainer", 0, "");
    };

    var _initHoursAndMinutesSelects = function(){
        var html = "<option value='-1' id='optDeadlineHours_-1' selected='selected'>--</option>";
        for (var i = 0; i < 24; i++)
        {
            html += ["<option value='",
                    i,
                    "' id='optDeadlineHours_",
                    i,
                    "'>",
                    i < 10 ? "0" + i : i,
                    "</option>"]
                    .join('');
        }
        jq("#taskDeadlineHours").html(html);
            
        html = "<option value='-1' id='optDeadlineMinutes_-1' selected='selected'>--</option>";
        for (var i = 0; i < 60; i++)
        {
            html += ["<option value='",
                    i,
                    "' id='optDeadlineMinutes_",
                    i,
                    "'>",
                    i < 10 ? "0" + i : i,
                    "</option>"]
                    .join('');
        }
        jq("#taskDeadlineMinutes").html(html);
    };

    var _readDataTask = function (taskID, entityType, entityID, contactid) {
        var dataTask = {},
            isValid = true,
            deadLine = null;

        if (jq.trim(jq("#addTaskPanel input[id$=taskDeadline]").val()) != "") {
            deadLine = jq("#taskDeadline").datepicker('getDate');
            if (parseInt(jq("#taskDeadlineHours option:selected").val()) != -1) {
                deadLine.setHours(parseInt(jq("#taskDeadlineHours option:selected").val()));
            } else {
                deadLine.setHours(0);
            }

            if (parseInt(jq("#taskDeadlineMinutes option:selected").val()) != -1) {
                deadLine.setMinutes(parseInt(jq("#taskDeadlineMinutes option:selected").val()));
            } else {
                deadLine.setMinutes(0);
            }

            deadLine = Teamlab.serializeTimestamp(deadLine);
        }

        dataTask = {
            id: taskID,
            title: jq("#tbxTitle").val(),
            description: jq("#tbxDescribe").val(),
            deadline: deadLine,
            responsibleid: jq("#taskActionViewAdvUsrSrContainer").attr("data-responsible-id"),
            categoryid: window.taskCategorySelector.CategoryID,
            isnotify: jq("#notifyResponsible").is(":checked"),
            contactid: contactid,
            alertValue: jq("#taskAlertInterval").val()
        };

        if (entityID != 0) {
            dataTask.entityType = entityType;
            dataTask.entityid = entityID;
        }

        var invalidTaskTime = (parseInt(jq("#taskDeadlineHours option:selected").val()) == -1
                            && parseInt(jq("#taskDeadlineMinutes option:selected").val()) != -1)
                            || (parseInt(jq("#taskDeadlineHours option:selected").val()) != -1
                            && parseInt(jq("#taskDeadlineMinutes option:selected").val()) == -1);

        if (jq.trim(jq("#tbxTitle").val()) == "") {
            AddRequiredErrorText(jq("#tbxTitle"), ASC.CRM.Resources.CRMJSResource.EmptyTaskTitle);
            ShowRequiredError(jq("#tbxTitle"), true);
            isValid = false;
        } else {
            RemoveRequiredErrorClass(jq("#tbxTitle"));
        }

        if (dataTask.responsibleid == "") {
            AddRequiredErrorText(jq("#taskActionViewAdvUsrSrContainer"), ASC.CRM.Resources.CRMJSResource.EmptyTaskResponsible);
            ShowRequiredError(jq("#taskActionViewAdvUsrSrContainer"), true);
            isValid = false;
        } else {
            RemoveRequiredErrorClass(jq("#taskActionViewAdvUsrSrContainer"));
        }

        if (dataTask.deadline == null || invalidTaskTime) {
            AddRequiredErrorText(jq("#taskDeadline"), ASC.CRM.Resources.CRMJSResource.EmptyTaskDeadline);
            ShowRequiredError(jq("#taskDeadline"), true);

            if (invalidTaskTime) {
                jq("#taskDeadlineHours").addClass("requiredInputError");
                jq("#taskDeadlineMinutes").addClass("requiredInputError");
            } else {
                jq("#taskDeadlineHours").removeClass("requiredInputError");
                jq("#taskDeadlineMinutes").removeClass("requiredInputError");
            }

            isValid = false;
        } else {
            RemoveRequiredErrorClass(jq("#taskDeadline"));
            jq("#taskDeadlineHours").removeClass("requiredInputError");
            jq("#taskDeadlineMinutes").removeClass("requiredInputError");
        }

        if (isValid) {
            return dataTask;
        } else {
            return null
        }
    };

    return {
        CallbackMethods: {
            add_task: function (params, task) {
                var isTab = typeof (ASC.CRM.ListTaskView) == "undefined" || typeof (ASC.CRM.ListTaskView.advansedFilter) == "undefined";
                if (typeof (ASC.CRM.ListTaskView) != "undefined" && typeof (ASC.CRM.ListTaskView.TaskList) != "undefined") {
                    var newTask = task;
                    ASC.CRM.ListTaskView.taskItemFactory(newTask);
                    ASC.CRM.ListTaskView.TaskList.push(newTask);

                    if (jq("#taskTable tbody tr").length == 0) {
                        if (!isTab) {
                            jq("#emptyContentForTasksFilter:not(.display-none)").addClass("display-none");
                        }
                        jq("#tasksEmptyScreen:not(.display-none)").addClass("display-none");

                        if (!isTab) {
                            jq("#taskFilterContainer").show();
                            ASC.CRM.ListTaskView.resizeFilter();
                        }

                        jq("#taskList").show();
                    }

                    jq.tmpl("taskTmpl", newTask).prependTo("#taskTable tbody");
                    ASC.CRM.ListTaskView.Total += 1;
                    jq("#totalTasksOnPage").text(ASC.CRM.ListTaskView.Total);

                    ASC.CRM.Common.tooltip("#taskTitle_" + newTask.id, "tooltip");
                    ASC.CRM.Common.RegisterContactInfoCard();
                }
                PopupKeyUpActionProvider.EnableEsc = true;
                jq.unblockUI();
            },

            edit_task: function(params, task) {
                var newTask = task;

                ASC.CRM.ListTaskView.taskItemFactory(newTask);
                ASC.CRM.ListTaskView.TaskList.push(newTask);

                jq("#task_" + newTask.id).attr("id", "old_task").hide();
                jq.tmpl("taskTmpl", newTask).insertBefore(jq("#old_task"));
                jq("#old_task").remove();

                ASC.CRM.Common.tooltip("#taskTitle_" + newTask.id, "tooltip");

                PopupKeyUpActionProvider.EnableEsc = true;
                jq.unblockUI();

                ASC.CRM.Common.RegisterContactInfoCard();
            }
        },

        init: function (_showChangeButton) {

            jq.tmpl("blockUIPanelTemplate", {
                id: "addTaskPanel",
                headerTest: ASC.CRM.Resources.CRMTaskResource.AddNewTask,
                questionText: "",
                innerHtmlText:"<div class=\"addTaskPanelContainer\"></div>",
                progressText: ""
            }).appendTo("#studioPageContent .mainPageContent .containerBodyBlock:first");

            jq("#addTaskPanel .addTaskPanelContainer").replaceWith(jq.tmpl("taskActionViewTmpl", {}).appendTo("#addTaskPanel .addTaskPanelContainer"));

            _initHoursAndMinutesSelects();

            jq("#taskDeadline").mask(ASC.Resources.Master.DatePatternJQ);
            jq("#taskDeadline").datepickerWithButton({
                onSelect: function(date) {
                    var selectedDate = jq("#taskDeadline").datepicker("getDate"),
                        tmpDate = new Date(),
                        today = new Date(tmpDate.getFullYear(), tmpDate.getMonth(), tmpDate.getDate(), 0, 0, 0, 0),
                        daysCount = Math.floor((selectedDate.getTime() - today.getTime()) / (24 * 60 * 60 * 1000));
                    _changeSelectionDeadlineButtons(daysCount);
                    setTimeout(function() {
                        jq('<input type="text" />').insertAfter("#taskDeadline").focus().remove();
                    }, 100);
                }
            });

            _initTaskCategorySelector();

            //init task ContactSelector
            window["taskContactSelector"] = new ASC.CRM.ContactSelector.ContactSelector("taskContactSelector",
                       {
                           SelectorType: ASC.CRM.Data.ContactSelectorTypeEnum.All,
                           EntityType: 0,
                           EntityID: 0,
                           ShowOnlySelectorContent: false,
                           DescriptionText: ASC.CRM.Resources.CRMCommonResource.FindContactByName,
                           DeleteContactText: "",
                           AddContactText: "",
                           IsInPopup: true,
                           ShowChangeButton: _showChangeButton,
                           ShowAddButton: false,
                           ShowDeleteButton: false,
                           ShowContactImg: true,
                           ShowNewCompanyContent: true,
                           ShowNewContactContent: true,
                           presetSelectedContactsJson: '',
                           ExcludedArrayIDs: [],
                           HTMLParent: "#addTaskPanel .connectWithContactContainer"
                       });

        },

        showTaskPanel: function(taskID, entityType, entityID, contact, params) {
            var index = ASC.CRM.TaskActionView.initPanel(taskID, contact, params);
            PopupKeyUpActionProvider.EnableEsc = false;
            HideRequiredError();
            jq("#addTaskPanel .error-popup").addClass("display-none");
            jq("#createNewButton").hide();
            StudioBlockUIManager.blockUI("#addTaskPanel", 650, 670, 0);

            jq("#addTaskPanel input[id$=tbxTitle]").focus();
            jq("#taskActionPopupOK").unbind("click").bind("click", function () {

                if (window.taskContactSelector.SelectedContacts.length > 1) {
                    ASC.CRM.TaskActionView.saveTaskGroup(entityType, entityID, window.taskContactSelector.SelectedContacts, index, params);
                } else {
                    var contactid=0;
                    if (window.taskContactSelector.SelectedContacts.length == 1) {
                        contactid = window.taskContactSelector.SelectedContacts[0];
                    }
                    ASC.CRM.TaskActionView.saveTask(taskID, entityType, entityID, contactid, index, params);
                }
            });
        },

        keyPress: function(event) {
            var code;
            if (!e) var e = event;
            if (!e) var e = window.event;

            if (e.keyCode) code = e.keyCode;
            else if (e.which) code = e.which;

            if (code >= 48 && code <= 57) {
                jq("#taskDeadlineContainer").find("a").each(function() { jq(this).css("border-bottom", ""); });
            }
        },

        changeDeadline: function(object) {
            var daysCount = parseInt(jq.trim(jq(object).attr('id').split('_')[1])),
                tmp = new Date(),
                newDate = new Date(tmp.setDate(tmp.getDate() + daysCount));
            jq("#taskDeadline").datepicker('setDate', newDate);

            _changeSelectionDeadlineButtons(daysCount);
        },

        saveTask: function(taskID, entityType, entityID, contactid, index, params) {

            var dataTask = _readDataTask(taskID, entityType, entityID, contactid);
            if (dataTask == null) {
                return false;
            }

            if (taskID != 0) {
                ASC.CRM.ListTaskView.TaskList.splice(index, 1);

                Teamlab.updateCrmTask({}, taskID, dataTask,
                {
                    success: ASC.CRM.TaskActionView.CallbackMethods.edit_task,
                    error: function (params, error) {
                        var taskErrorBox = jq("#addTaskPanel .error-popup");
                        taskErrorBox.text(error[0]);
                        taskErrorBox.removeClass("display-none").show();
                    },
                    before: function (params) {
                        LoadingBanner.strLoading = ASC.CRM.Resources.CRMTaskResource.SavingTask;
                        LoadingBanner.showLoaderBtn("#addTaskPanel");
                    },
                    after: function (params) {
                        LoadingBanner.hideLoaderBtn("#addTaskPanel");
                    }
                });
            } else {
                var callbackFunc = ASC.CRM.TaskActionView.CallbackMethods.add_task;
                if (typeof (params) === "object" && params != null && params.hasOwnProperty("success") && typeof (params.success) === "function") {
                    callbackFunc = params.success;
                }

                Teamlab.addCrmTask({}, dataTask,
                    {
                        success: callbackFunc,
                        error: function(params, error) {
                            var taskErrorBox = jq("#addTaskPanel .error-popup");
                            taskErrorBox.text(error[0]);
                            taskErrorBox.removeClass("display-none").show();
                        },
                        before: function (params) {
                            LoadingBanner.strLoading = ASC.CRM.Resources.CRMTaskResource.SavingTask;
                            LoadingBanner.showLoaderBtn("#addTaskPanel");
                        },
                        after: function (params) {
                            LoadingBanner.hideLoaderBtn("#addTaskPanel");
                        }
                    });
            }
        },

        saveTaskGroup: function (entityType, entityID, contactids, index, params) {
            var dataTask = _readDataTask(0, entityType, entityID, contactids);
            if (dataTask == null) {
                return false;
            }

            var callbackFunc = function (params, tasks) { };
            if (typeof (params) === "object" && params.hasOwnProperty("success") && typeof (params.success) === "function") {
                callbackFunc = params.success;
            }

            Teamlab.addCrmTaskGroup({}, dataTask,
                {
                    success: callbackFunc,
                    error: function (params, error) {
                        var taskErrorBox = jq("#addTaskPanel .error-popup");
                        taskErrorBox.text(error[0]);
                        taskErrorBox.removeClass("display-none").show();
                    },
                    before: function (params) {
                        LoadingBanner.strLoading = ASC.CRM.Resources.CRMTaskResource.SavingTask;
                        LoadingBanner.showLoaderBtn("#addTaskPanel");
                    },
                    after: function (params) {
                        LoadingBanner.hideLoaderBtn("#addTaskPanel");
                    }
                });
        },


        initPanel: function(taskID, contact, params) {
            if (!jq("#taskActionViewAdvUsrSrContainer").next().is(".advanced-selector-container")) {
                jq("#taskActionViewAdvUsrSrContainer").useradvancedSelector({
                    showme: false,
                    inPopup: true,
                    onechosen: true,
                    showGroups: true,
                    withGuests: false
                });

                if (ASC.CRM.Data.isCrmAvailableForAllUsers == false) {
                    var users = [],
                        items = jq("#taskActionViewAdvUsrSrContainer").data("items");

                    for (var i = 0, n = ASC.CRM.Data.crmAvailableWithAdminList.length; i < n; i++) {
                        for (var j = 0, m = items.length; j < m; j++) {
                            if (ASC.CRM.Data.crmAvailableWithAdminList[i].id == items[j].id) {
                                users.push(items[j]);
                                continue;
                            }
                        }
                    }
                    jq("#taskActionViewAdvUsrSrContainer").useradvancedSelector("rewriteItemList", users, []);
                }

                jq("#taskActionViewAdvUsrSrContainer").on("showList", function (event, item) {
                    if (item.id == Teamlab.profile.id) {
                        jq("#notifyResponsible").prop("checked", false).prop("disabled", true);
                    } else {
                        jq("#notifyResponsible").prop("disabled", false).prop("checked", true);
                    }
                    jq("#taskActionViewAdvUsrSrContainer").attr("data-responsible-id", item.id)
                    jq("#taskActionViewAdvUsrSrContainer .taskResponsibleLabel").text(Encoder.htmlDecode(item.title));
                });


                if (typeof (params) != "undefined" && params != null && params.hasOwnProperty("taskResponsibleSelectorUserIDs")) {
                    var adminsGuids = [],
                        items = [],
                        taskResponsibleSelectorUserIDs = [],
                        taskResponsibleSelectorUsers = [];
                    for (var i = 0, n = ASC.CRM.Data.crmAdminList.length; i < n; i++) {
                        adminsGuids.push(ASC.CRM.Data.crmAdminList[i].id);
                    }
                    taskResponsibleSelectorUserIDs = params.taskResponsibleSelectorUserIDs.concat(adminsGuids);
                    items = jq("#taskActionViewAdvUsrSrContainer").data("items");
                    for (var i = 0, n = items.length; i < n; i++) {
                        if (taskResponsibleSelectorUserIDs.indexOf(items[i].id) != -1) {
                            taskResponsibleSelectorUsers.push(items[i]);
                        }
                    }
                    jq("#taskActionViewAdvUsrSrContainer").useradvancedSelector("rewriteItemList", taskResponsibleSelectorUsers, []);
                }
            }

            if (taskID > 0) {
                jq("div.noMatches").hide();

                var index = ASC.CRM.ListTaskView.findIndexOfTaskByID(taskID);
                if (index != -1) {
                    var task = ASC.CRM.ListTaskView.TaskList[index];
                    jq("#tbxTitle").val(task.title);
                    jq("#tbxDescribe").val(task.description);

                    jq("#taskDeadline").datepicker('setDate', task.deadLine);

                    if (task.deadLine.getHours() == 0 && task.deadLine.getMinutes() == 0) {
                        jq("#optDeadlineHours_-1").attr('selected', true);
                        jq("#optDeadlineMinutes_-1").attr('selected', true);
                    } else {
                        jq("#optDeadlineHours_" + task.deadLine.getHours()).attr('selected', true);
                        jq("#optDeadlineMinutes_" + task.deadLine.getMinutes()).attr('selected', true);
                    }

                    var obj = window.taskCategorySelector.getRowByContactID(task.category.id);
                    window.taskCategorySelector.changeContact(obj);

                    _initTaskResponsibleSelector(task.contact, task);
                    _initTaskContactSelector(task.contact);

                    jq("#taskAlertInterval").val(task.alertValue);

                    jq("#taskActionPopupOK").html(ASC.CRM.Resources.CRMJSResource.SaveChanges);
                    jq("#addTaskPanel div.containerHeaderBlock td:first").html(ASC.CRM.Resources.CRMJSResource.EditTask);

                }
                return index;
            } else {
                jq("#taskActionPopupOK").html(ASC.CRM.Resources.CRMJSResource.AddThisTask);
                jq("#addTaskPanel div.containerHeaderBlock td:first").html(ASC.CRM.Resources.CRMJSResource.AddNewTask);

                jq("#tbxTitle").val("");
                jq("#tbxDescribe").val("");


                jq("#taskDeadline").datepicker('setDate', new Date());
                _changeSelectionDeadlineButtons(0);

                jq("#optDeadlineHours_-1").attr('selected', true);
                jq("#optDeadlineMinutes_-1").attr('selected', true);

                var obj = window.taskCategorySelector.getRowByContactID(0);
                window.taskCategorySelector.changeContact(obj);

                _initTaskResponsibleSelector(contact, null);
                _initTaskContactSelector(contact);
                jq("#taskAlertInterval").val("1440");
                return -1;
            }

        },

        changeTime: function(obj) {
            var id = jq(obj).attr("id"),
                value = jq("#" + id + " option:selected").val();
            if (value == '-1') {
                jq("#optDeadlineHours_-1").attr("selected", "selected");
                jq("#optDeadlineMinutes_-1").attr("selected", "selected");
            }
        }
    }
};

jq(document).ready(function() {
    jq.dropdownToggle({
        switcherSelector: ".noContentBlock .hintCategories",
        dropdownID: "files_hintCategoriesPanel",
        fixWinSize: false
    });
});