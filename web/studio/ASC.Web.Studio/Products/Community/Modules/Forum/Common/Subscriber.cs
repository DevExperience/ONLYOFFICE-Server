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
using System.Text;
using AjaxPro;
using ASC.Core;
using ASC.Forum;
using ASC.Web.Studio.Utility;

namespace ASC.Web.Community.Forum
{
    [AjaxNamespace("Subscriber")]
    public class Subscriber : ISubscriberView
    {
        public Subscriber()
        {
            ForumManager.Instance.PresenterFactory.GetPresenter<ISubscriberView>().SetView(this);
        }

        #region Topic

        [AjaxMethod(HttpSessionStateRequirement.ReadWrite)]
        public AjaxResponse SubscribeOnTopic(int idTopic, int statusNotify)
        {
            var resp = new AjaxResponse();

            if (statusNotify == 1 && Subscribe != null)
            {
                Subscribe(this, new SubscribeEventArgs(SubscriptionConstants.NewPostInTopic,
                                                       idTopic.ToString(),
                                                       SecurityContext.CurrentAccount.ID));

                resp.rs1 = "1";
                resp.rs2 = RenderTopicSubscription(false, idTopic);
                resp.rs3 = "subscribed";
            }
            else if (UnSubscribe != null)
            {
                UnSubscribe(this, new SubscribeEventArgs(SubscriptionConstants.NewPostInTopic,
                                                         idTopic.ToString(),
                                                         SecurityContext.CurrentAccount.ID));

                resp.rs1 = "1";
                resp.rs2 = RenderTopicSubscription(true, idTopic);
                resp.rs3 = "unsubscribed";
            }
            else
            {
                resp.rs1 = "0";
                resp.rs2 = Resources.ForumResource.ErrorSubscription;
            }
            return resp;
        }

        public string RenderTopicSubscription(bool isSubscribe, int topicID)
        {
            var sb = new StringBuilder();

            sb.Append("<a id=\"statusSubscribe\"  class=\"" +
                      "follow-status " + (IsTopicSubscribe(topicID) ? "subscribed" : "unsubscribed") +
                      "\" title=\"" + (!isSubscribe ? Resources.ForumResource.UnSubscribeOnTopic : Resources.ForumResource.SubscribeOnTopic) +
                      "\" href=\"#\"onclick=\"ForumSubscriber.SubscribeOnTopic('" + topicID + "', " + (isSubscribe ? 1 : 0) + "); return false;\"></a>");

            return sb.ToString();
        }

        public bool IsTopicSubscribe(int topicID)
        {
            if (GetSubscriptionState != null)
                GetSubscriptionState(this, new SubscribeEventArgs(SubscriptionConstants.NewPostInTopic,
                                                                  topicID.ToString(),
                                                                  SecurityContext.CurrentAccount.ID));

            return IsSubscribe;
        }

        #endregion

        #region Thread

        public string RenderThreadSubscription(bool isSubscribe, int threadID)
        {
            var sb = new StringBuilder();

            sb.Append("<a id=\"statusSubscribe\"  class=\"" +
                      "follow-status " + (IsThreadSubscribe(threadID) ? "subscribed" : "unsubscribed") +
                      "\" title=\"" + (!isSubscribe ? Resources.ForumResource.UnSubscribeOnThread : Resources.ForumResource.SubscribeOnThread) +
                      "\" href=\"#\" onclick=\"ForumSubscriber.SubscribeOnThread('" + threadID + "', " + (isSubscribe ? 1 : 0) + "); return false;\"></a>");

            return sb.ToString();
        }

        public bool IsThreadSubscribe(int threadID)
        {
            if (GetSubscriptionState != null)
                GetSubscriptionState(this, new SubscribeEventArgs(SubscriptionConstants.NewPostInThread,
                                                                  threadID.ToString(),
                                                                  SecurityContext.CurrentAccount.ID));

            return IsSubscribe;
        }

        [AjaxMethod(HttpSessionStateRequirement.ReadWrite)]
        public AjaxResponse SubscribeOnThread(int idThread, int statusNotify)
        {
            var resp = new AjaxResponse();
            if (statusNotify == 1 && Subscribe != null)
            {
                Subscribe(this, new SubscribeEventArgs(SubscriptionConstants.NewPostInThread,
                                                       idThread.ToString(),
                                                       SecurityContext.CurrentAccount.ID));

                resp.rs1 = "1";
                resp.rs2 = RenderThreadSubscription(false, idThread);
                resp.rs3 = "subscribed";
            }
            else if (UnSubscribe != null)
            {
                UnSubscribe(this, new SubscribeEventArgs(SubscriptionConstants.NewPostInThread,
                                                         idThread.ToString(),
                                                         SecurityContext.CurrentAccount.ID));

                resp.rs1 = "1";
                resp.rs2 = RenderThreadSubscription(true, idThread);
                resp.rs3 = "unsubscribed";
            }
            else
            {
                resp.rs1 = "0";
                resp.rs2 = Resources.ForumResource.ErrorSubscription;
            }
            return resp;
        }

        #endregion

        #region Topic on forum

        public string RenderNewTopicSubscription(bool isSubscribe)
        {
            var sb = new StringBuilder();

            sb.Append("<div id=\"forum_subcribeOnNewTopicBox\">");
            sb.Append("<a class='linkAction' href=\"#\" onclick=\"ForumSubscriber.SubscribeOnNewTopics(" + (isSubscribe ? 1 : 0) + "); return false;\">" + (!isSubscribe ? Resources.ForumResource.UnsubscribeOnNewTopicInForum : Resources.ForumResource.SubscribeOnNewTopicInForum) + "</a>");
            sb.Append("</div>");

            return sb.ToString();
        }

        public bool IsNewTopicSubscribe()
        {
            if (GetSubscriptionState != null)
                GetSubscriptionState(this, new SubscribeEventArgs(SubscriptionConstants.NewTopicInForum,
                                                                  null,
                                                                  SecurityContext.CurrentAccount.ID));

            return IsSubscribe;
        }

        [AjaxMethod(HttpSessionStateRequirement.ReadWrite)]
        public AjaxResponse SubscribeOnNewTopic(int statusNotify)
        {
            var resp = new AjaxResponse();
            if (statusNotify == 1 && Subscribe != null)
            {
                Subscribe(this, new SubscribeEventArgs(SubscriptionConstants.NewTopicInForum,
                                                       null,
                                                       SecurityContext.CurrentAccount.ID));

                resp.rs1 = "1";
                resp.rs2 = RenderNewTopicSubscription(false);
            }
            else if (UnSubscribe != null)
            {
                UnSubscribe(this, new SubscribeEventArgs(SubscriptionConstants.NewTopicInForum,
                                                         null,
                                                         SecurityContext.CurrentAccount.ID));

                resp.rs1 = "1";
                resp.rs2 = RenderNewTopicSubscription(true);
            }
            else
            {
                resp.rs1 = "0";
                resp.rs2 = Resources.ForumResource.ErrorSubscription;
            }
            return resp;
        }

        #endregion

        #region ISubscriberView Members

        public event EventHandler<SubscribeEventArgs> GetSubscriptionState;

        public bool IsSubscribe { get; set; }

        public event EventHandler<SubscribeEventArgs> Subscribe;

        public event EventHandler<SubscribeEventArgs> UnSubscribe;

        #endregion
    }
}