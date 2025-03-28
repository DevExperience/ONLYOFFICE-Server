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
using ASC.Xmpp.Core;
using ASC.Xmpp.Core.protocol;
using ASC.Xmpp.Core.protocol.client;
using ASC.Xmpp.Core.protocol.x;
using ASC.Xmpp.Core.protocol.x.tm.history;
using ASC.Xmpp.Server.Handler;
using ASC.Xmpp.Server.Storage;
using ASC.Xmpp.Server.Streams;
using ASC.Xmpp.Server.Utils;
using log4net;

namespace ASC.Xmpp.Server.Services.Jabber
{
    [XmppHandler(typeof(Message))]
    [XmppHandler(typeof(History))]
    [XmppHandler(typeof(PrivateLog))]
    public class MessageArchiveHandler : XmppStanzaHandler
    {
        private DbMessageArchive archiveStore;

        private static readonly int BUFFER_SIZE = 25;

        private List<Message> messageBuffer = new List<Message>(BUFFER_SIZE);

        private static readonly ILog log = LogManager.GetLogger(typeof(MessageArchiveHandler));


        public override void OnRegister(IServiceProvider serviceProvider)
        {
            archiveStore = ((StorageManager)serviceProvider.GetService(typeof(StorageManager))).GetStorage<DbMessageArchive>("archive");
        }

        public override void OnUnregister(IServiceProvider serviceProvider)
        {
            FlushMessageBuffer();
        }

        public override IQ HandleIQ(XmppStream stream, IQ iq, XmppHandlerContext context)
        {
            if (iq.Query is PrivateLog && iq.Type == IqType.get) return GetPrivateLog(stream, iq, context);
            if (iq.Query is PrivateLog && iq.Type == IqType.set) return SetPrivateLog(stream, iq, context);
            if (iq.Query is PrivateLog && (iq.Type == IqType.result || iq.Type == IqType.error)) return null;
            if (iq.Query is History && iq.Type == IqType.get) return GetHistory(stream, iq, context);
            return XmppStanzaError.ToServiceUnavailable(iq);
        }

        public override void HandleMessage(XmppStream stream, Message message, XmppHandlerContext context)
        {
            if (!message.HasTo) return;

            if (archiveStore.GetMessageLogging(message.From, message.To))
            {
                if (!string.IsNullOrEmpty(message.Body) ||
                    !string.IsNullOrEmpty(message.Subject) ||
                    !string.IsNullOrEmpty(message.Thread) ||
                    message.Html != null)
                {
                    var flush = false;
                    lock (messageBuffer)
                    {
                        //Add xdelay
                        if (message.XDelay == null)
                        {
                            message.XDelay = new Delay();
                            message.XDelay.Stamp = DateTime.UtcNow;
                        }
                        messageBuffer.Add(message);

                        flush = BUFFER_SIZE <= messageBuffer.Count;
                    }
                    if (flush) FlushMessageBuffer();
                }
            }
        }


        private IQ GetPrivateLog(XmppStream stream, IQ iq, XmppHandlerContext context)
        {
            if (!iq.HasTo) return XmppStanzaError.ToBadRequest(iq);

            var privateLog = (PrivateLog)iq.Query;
            privateLog.RemoveAllChildNodes();
            var logging = archiveStore.GetMessageLogging(iq.From, iq.To);
            privateLog.AddChild(new PrivateLogItem() { Jid = iq.To, Log = logging });

            iq.SwitchDirection();
            iq.Type = IqType.result;
            return iq;
        }

        private IQ SetPrivateLog(XmppStream stream, IQ iq, XmppHandlerContext context)
        {
            var privateLog = (PrivateLog)iq.Query;
            foreach (var item in privateLog.SelectElements<PrivateLogItem>())
            {
                archiveStore.SetMessageLogging(iq.From, item.Jid, item.Log);
                var to = new Jid(item.Jid.Bare);
                var session = context.SessionManager.GetSession(to);
                if (session != null)
                {
                    var info = new IQ(IqType.set);
                    info.Id = UniqueId.CreateNewId();
                    info.From = iq.From;
                    info.To = session.Jid;
                    info.Query = new PrivateLog();
                    info.Query.AddChild(new PrivateLogItem() { Jid = iq.From, Log = item.Log });
                    context.Sender.SendTo(session, info);
                }
            }
            privateLog.RemoveAllChildNodes();

            iq.SwitchDirection();
            iq.Type = IqType.result;
            return iq;
        }

        private IQ GetHistory(XmppStream stream, IQ iq, XmppHandlerContext context)
        {
            if (!iq.HasTo) return XmppStanzaError.ToServiceUnavailable(iq);

            FlushMessageBuffer();

            var history = (History)iq.Query;
            history.RemoveAllChildNodes();
            foreach (var m in archiveStore.GetMessages(iq.From, iq.To, history.From, history.To, history.Count))
            {
                history.AddChild(HistoryItem.FromMessage(m));
            }

            iq.Type = IqType.result;
            iq.SwitchDirection();
            return iq;
        }

        public void FlushMessageBuffer()
        {
            Message[] buffercopy = null;
            lock (messageBuffer)
            {
                buffercopy = messageBuffer.ToArray();
                messageBuffer.Clear();
            }

            if (buffercopy.Length == 0) return;

            try
            {
                archiveStore.SaveMessages(buffercopy);
                log.DebugFormat("Flush messages buffer, count: {0}", buffercopy.Length);
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Error flush messages buffer, count: {0}, exception: {1}", buffercopy.Length, ex);
            }
        }
    }
}