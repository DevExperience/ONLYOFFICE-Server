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
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading;
using ASC.Common.Utils;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using log4net;

namespace ASC.Common.Notify
{
    public class AWSEmail
    {
        public enum SendStatus
        {
            Ok,
            Failed,
            QuotaLimit
        }

        private readonly AmazonSimpleEmailServiceClient _emailService;
        private readonly ILog _log = LogHolder.Log("ASC.Notify.AmazonSES");

        //Static fields
        private static readonly TimeSpan RefreshTimeout;
        private static DateTime _lastRefresh;
        private static DateTime _lastSend;
        private static TimeSpan _sendWindow = TimeSpan.MinValue;
        private static GetSendQuotaResult _quota;
        private static readonly object SynchRoot = new object();


        static AWSEmail()
        {
            RefreshTimeout = TimeSpan.FromMinutes(30);
            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["ses.refreshTimeout"]))
            {
                TimeSpan.TryParse(ConfigurationManager.AppSettings["ses.refreshTimeout"], out RefreshTimeout);
            }
            _lastRefresh = DateTime.UtcNow - RefreshTimeout;//Set to refresh on first send
        }

        public AWSEmail()
        {
            var accessKey = ConfigurationManager.AppSettings["ses.accessKey"];
            var secretKey = ConfigurationManager.AppSettings["ses.secretKey"];

            _emailService = new AmazonSimpleEmailServiceClient(accessKey, secretKey);


        }

        public SendStatus SendEmail(MailMessage mailMessage)
        {
            //Check if we need to query stats
            RefreshQuotaIfNeeded();
            if (_quota != null)
            {
                lock (SynchRoot)
                {
                    if (_quota.Max24HourSend <= _quota.SentLast24Hours)
                    {
                        //Quota exceeded
                        //Queu next refresh to +24 hours
                        _lastRefresh = DateTime.UtcNow.AddHours(24);
                        _log.WarnFormat("quota limit reached. setting next check to: {0}", _lastRefresh);
                        return SendStatus.QuotaLimit;
                    }
                }
            }



            var destination = new Destination
            {
                ToAddresses = mailMessage.To.Select(adresses => adresses.Address).ToList(),
                BccAddresses = mailMessage.Bcc.Select(adresses => adresses.Address).ToList(),
                CcAddresses = mailMessage.CC.Select(adresses => adresses.Address).ToList(),

            };

            var body = new Body(new Content(mailMessage.Body) { Charset = Encoding.UTF8.WebName });

            if (mailMessage.AlternateViews.Count > 0)
            {
                //Get html body
                foreach (var alternateView in mailMessage.AlternateViews)
                {
                    if ("text/html".Equals(alternateView.ContentType.MediaType, StringComparison.OrdinalIgnoreCase))
                    {
                        var stream = alternateView.ContentStream;
                        var buf = new byte[stream.Length];
                        stream.Read(buf, 0, buf.Length);
                        stream.Seek(0, SeekOrigin.Begin);//NOTE:seek to begin to keep HTML body
                        body.Html = new Content(Encoding.UTF8.GetString(buf)) { Charset = Encoding.UTF8.WebName };
                        break;
                    }
                }
            }

            var message = new Message(new Content(mailMessage.Subject), body);

            var seRequest = new SendEmailRequest(mailMessage.From.ToEncodedStringEx(), destination, message);
            if (mailMessage.ReplyTo != null)
                seRequest.ReplyToAddresses.Add(mailMessage.ReplyTo.Address);

            ThrottleIfNeeded();

            var response = _emailService.SendEmail(seRequest);
            _lastSend = DateTime.UtcNow;
            return response != null ? SendStatus.Ok : SendStatus.Failed;
        }

        private void ThrottleIfNeeded()
        {
            //Check last send and throttle if needed
            if (_sendWindow != TimeSpan.MinValue)
            {
                if (DateTime.UtcNow - _lastSend <= _sendWindow)
                //Possible BUG: at high frequncies maybe bug with to little differences
                {
                    //This means that time passed from last send is less then message per second
                    _log.DebugFormat("send rate doesn't fit in send window. sleeping for:{0}", _sendWindow);
                    Thread.Sleep(_sendWindow);
                }
            }
        }

        private void RefreshQuotaIfNeeded()
        {
            if (!IsRefreshNeeded()) return;

            lock (SynchRoot)
            {
                if (IsRefreshNeeded())//Double check
                {
                    _log.DebugFormat("refreshing qouta. interval: {0} Last refresh was at: {1}", RefreshTimeout,
                                     _lastRefresh);

                    //Do quota refresh
                    _lastRefresh = DateTime.UtcNow.AddMinutes(1);
                    try
                    {
                        var quotaRequest = new GetSendQuotaRequest();
                        _quota = _emailService.GetSendQuota(quotaRequest).GetSendQuotaResult;
                        _sendWindow = TimeSpan.FromSeconds(1.0 / _quota.MaxSendRate);
                        _log.DebugFormat("quota: {0}/{1} at {2} mps. send window:{3}", _quota.SentLast24Hours,
                                         _quota.Max24HourSend, _quota.MaxSendRate, _sendWindow);
                    }
                    catch (Exception e)
                    {
                        _log.Error("error refreshing quota", e);
                    }
                }
            }
        }

        private static bool IsRefreshNeeded()
        {
            return (DateTime.UtcNow - _lastRefresh) > RefreshTimeout || _quota == null;
        }
    }
}
