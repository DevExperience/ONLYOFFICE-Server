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

#region usings

using System;
using System.Runtime.Remoting;
using System.Runtime.Serialization;

#endregion

namespace ASC.Common.Security.Authorizing
{
    [Serializable]
    public class AuthorizingException : RemotingException
    {
        private readonly string _Message;

        public AuthorizingException(string message)
            : base(message)
        {
        }

        public AuthorizingException(ISubject subject, IAction[] actions)
        {
            if (subject == null) throw new ArgumentNullException("subject");
            if (actions == null || actions.Length == 0) throw new ArgumentNullException("actions");
            Subject = subject;
            Actions = actions;
            string sactions = "";
            Array.ForEach(actions, action => { sactions += action.ToString() + ", "; });
            _Message = String.Format(
                "\"{0}\" access denied \"{1}\"",
                subject,
                sactions
                );
        }

        public AuthorizingException(ISubject subject, IAction[] actions, ISubject[] denySubjects, IAction[] denyActions)
        {
            _Message = FormatErrorMessage(subject, actions, denySubjects, denyActions);
        }

        protected AuthorizingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _Message = info.GetValue("_Message", typeof(string)) as string;
            Subject = info.GetValue("Subject", typeof(ISubject)) as ISubject;
            Actions = info.GetValue("Actions", typeof(IAction[])) as IAction[];
        }

        public override string Message
        {
            get { return _Message; }
        }

        public ISubject Subject { get; internal set; }
        public IAction[] Actions { get; internal set; }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Subject", Subject, typeof(ISubject));
            info.AddValue("_Message", _Message, typeof(string));
            info.AddValue("Actions", Actions, typeof(IAction[]));
            base.GetObjectData(info, context);
        }

        internal static string FormatErrorMessage(ISubject subject, IAction[] actions, ISubject[] denySubjects,
                                                  IAction[] denyActions)
        {
            if (subject == null) throw new ArgumentNullException("subject");
            if (actions == null || actions.Length == 0) throw new ArgumentNullException("actions");
            if (denySubjects == null || denySubjects.Length == 0) throw new ArgumentNullException("denySubjects");
            if (denyActions == null || denyActions.Length == 0) throw new ArgumentNullException("denyActions");
            if (actions.Length != denySubjects.Length || actions.Length != denyActions.Length)
                throw new ArgumentException();
            string reasons = "";
            for (int i = 0; i < actions.Length; i++)
            {
                string reason = "";
                if (denySubjects[i] != null && denyActions[i] != null)
                    reason = String.Format("{0}:{1} access denied {2}.",
                                           actions[i].Name,
                                           (denySubjects[i] is IRole ? "role:" : "") + denySubjects[i].Name,
                                           denyActions[i].Name
                        );
                else
                    reason = String.Format("{0}: access denied.", actions[i].Name);
                if (i != actions.Length - 1)
                    reason += ", ";
                reasons += reason;
            }
            string sactions = "";
            Array.ForEach(actions, action => { sactions += action.ToString() + ", "; });
            string message = String.Format(
                "\"{0}\" access denied \"{1}\". Cause: {2}.",
                (subject is IRole ? "role:" : "") + subject.Name,
                sactions,
                reasons
                );
            return message;
        }
    }
}