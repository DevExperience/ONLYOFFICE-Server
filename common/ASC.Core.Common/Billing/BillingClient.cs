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

using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Web;
using System.Xml.Linq;

namespace ASC.Core.Billing
{
    public class BillingClient : ClientBase<IService>, IDisposable
    {
        private readonly static ILog log = LogManager.GetLogger(typeof(TariffService));
        private readonly bool test;


        public BillingClient()
            : this(false)
        {
        }

        public BillingClient(bool test)
        {
            this.test = test;
        }


        public PaymentLast GetLastPayment(string portalId)
        {
            var result = Request("GetLatestActiveResourceEx", portalId);
            var xelement = ToXElement("<root>" + result + "</root>", true);
            var dedicated = xelement.Element("dedicated-resource");
            var payment = xelement.Element("payment");

            if (!test && GetValueString(payment.Element("status")) == "4")
            {
                throw new BillingException("Can not accept test payment.");
            }

            var autorenewal = string.Empty;
            try
            {
                autorenewal = Request("GetLatestAvangateLicenseRecurringStatus", portalId);
            }
            catch (BillingException err)
            {
                log.Error(err);
            }

            return new PaymentLast
            {
                ProductId = GetValueString(dedicated.Element("product-id")),
                StartDate = GetValueDateTime(dedicated.Element("start-date")),
                EndDate = GetValueDateTime(dedicated.Element("end-date")),
                Autorenewal = "enabled".Equals(autorenewal, StringComparison.InvariantCultureIgnoreCase),
            };
        }

        public IEnumerable<PaymentInfo> GetPayments(string portalId, DateTime from, DateTime to)
        {
            var result = string.Empty;
            if (from == DateTime.MinValue && to == DateTime.MaxValue)
            {
                result = Request("GetPayments", portalId);
            }
            else
            {
                result = Request("GetListOfPaymentsByTimeSpan", portalId, Tuple.Create("StartDate", from.ToString("yyyy-MM-dd HH:mm:ss")), Tuple.Create("EndDate", to.ToString("yyyy-MM-dd HH:mm:ss")));
            }
            var xelement = ToXElement(result, true);
            foreach (var x in xelement.Elements("payment"))
            {
                yield return ToPaymentInfo(x);
            }
        }

        public IDictionary<string, Tuple<Uri, Uri>> GetPaymentUrls(string portalId, string[] products)
        {
            var urls = new Dictionary<string, Tuple<Uri, Uri>>();

            var parameters = products
                .Distinct()
                .Select(p => Tuple.Create("ProductId", p))
                .Concat(new[] { Tuple.Create("PaymentSystemId", "1") })
                .ToArray();

            var paymentUrls = ToXElement(Request("GetBatchPaymentSystemUrl", portalId, parameters), false)
                .Elements()
                .ToDictionary(e => e.Attribute("id").Value, e => ToUrl(e.Attribute("value").Value));

            var upgradeUrls = new Dictionary<string, string>();
            try
            {
                upgradeUrls = ToXElement(Request("GetBatchPaymentSystemUpgradeUrl", portalId, parameters), false)
                    .Elements()
                    .ToDictionary(e => e.Attribute("id").Value, e => ToUrl(e.Attribute("value").Value));
            }
            catch (BillingException) { }

            foreach (var p in products)
            {
                var url = string.Empty;
                var paymentUrl = (Uri)null;
                var upgradeUrl = (Uri)null;
                if (paymentUrls.TryGetValue(p, out url) && !string.IsNullOrEmpty(url))
                {
                    paymentUrl = new Uri(url);
                }
                if (upgradeUrls.TryGetValue(p, out url) && !string.IsNullOrEmpty(url))
                {
                    upgradeUrl = new Uri(url);
                }
                urls[p] = Tuple.Create(paymentUrl, upgradeUrl);
            }
            return urls;
        }

        public string GetPaymentUrl(string portalId, string product, string language)
        {
            var parameters = new[] { Tuple.Create("ProductId", product), Tuple.Create("PaymentSystemId", "1") }.ToList();
            if (!string.IsNullOrEmpty(language))
            {
                parameters.Add(Tuple.Create("Language", language.Split('-')[0]));
            }
            if (test)
            {
                parameters.Add(Tuple.Create("Test", "true"));
            }

            var result = Request("GetPaymentSystemUrl", portalId, parameters.ToArray());
            var url = ToUrl(result);
            if (string.IsNullOrEmpty(url))
            {
                throw new BillingException(result);
            }
            return url;
        }

        public Invoice GetInvoice(string paymentId)
        {
            var result = Request("GetInvoice", null, Tuple.Create("PaymentId", paymentId));
            var xelement = ToXElement(result, true);
            return new Invoice
            {
                Sale = GetValueString(xelement.Element("sale")),
                Refund = GetValueString(xelement.Element("refund")),
            };
        }

        public PaymentOffice GetPaymentOffice(string portalId)
        {
            if (32 <= portalId.Length)
            {
                // installation or open-source
                try
                {
                    var result = Request("GetLatestActiveResourceInDetails", portalId);
                    var xelement = ToXElement(result, true);
                    var skey = xelement.Element("skey");
                    var resources = xelement.Element("resource-options");
                    return new PaymentOffice
                    {
                        Key1 = GetValueString(xelement.Element("customer-id")),
                        Key2 = skey != null ? skey.Value : ConfigurationManager.AppSettings["files.docservice.key"],
                        StartDate = GetValueDateTime(xelement.Element("start-date")),
                        EndDate = GetValueDateTime(xelement.Element("end-date")),
                        UsersCount = resources != null ? (int)GetValueDecimal(resources.Element("users-max")) : default(int),
                        Editing = resources != null ? (int)GetValueDecimal(resources.Element("editing")) == 1 : true,
                        CoEditing = resources != null ? (int)GetValueDecimal(resources.Element("co-editing")) == 1 : true,
                        Ad = resources != null ? (int)GetValueDecimal(resources.Element("ad")) == 1 : true,
                    };
                }
                catch (BillingException error)
                {
                    log.Error(error);
                    return new PaymentOffice { Key1 = portalId, };
                }
            }
            else
            {
                // SAAS
                return new PaymentOffice
                {
                    Key1 = portalId,
                    Key2 = ConfigurationManager.AppSettings["files.docservice.key"],
                    EndDate = DateTime.MaxValue,
                    UsersCount = int.MaxValue,
                    CoEditing = true,
                    Editing = true,
                };
            }
        }

        public string AuthorizedPartner(string partnerId, bool setAuthorized, DateTime startDate = default(DateTime))
        {
            try
            {
                return Request("SetPartnerStatus",
                               partnerId.Replace("-", ""),
                               Tuple.Create("Security", ConfigurationManager.AppSettings["core.payment.security"]),
                               Tuple.Create("ProductId", ConfigurationManager.AppSettings["core.payment-partners-product"]),
                               Tuple.Create("Status", setAuthorized ? "1" : "0"),
                               Tuple.Create("RecreateSKey", "0"),
                               Tuple.Create("Renewal", (!setAuthorized || startDate == default(DateTime) || startDate == DateTime.MinValue
                                                            ? string.Empty
                                                            : startDate.ToString("yyyy-MM-dd HH:mm:ss"))));
            }
            catch (BillingException error)
            {
                log.Error(error);
                return string.Empty;
            }
        }

        private string Request(string method, string portalId, params Tuple<string, string>[] parameters)
        {
            var request = new XElement(method);
            if (!string.IsNullOrEmpty(portalId))
            {
                request.Add(new XElement("PortalId", portalId));
            }
            request.Add(parameters.Select(p => new XElement(p.Item1, p.Item2)).ToArray());

            var responce = Channel.Request(new Message { Type = MessageType.Data, Content = request.ToString(SaveOptions.DisableFormatting), });
            if (responce.Type == MessageType.Data)
            {
                var result = responce.Content;
                var invalidChar = ((char)65279).ToString();
                return result.Contains(invalidChar) ? result.Replace(invalidChar, string.Empty) : result;
            }
            else
            {
                throw new BillingException(responce.Content);
            }
        }

        private XElement ToXElement(string xml, bool htmlDecode)
        {
            if (htmlDecode && xml.Contains("&"))
            {
                xml = HttpUtility.HtmlDecode(xml);
            }
            return XElement.Parse(xml);
        }

        private PaymentInfo ToPaymentInfo(XElement x)
        {
            return new PaymentInfo
            {
                ID = (int)GetValueDecimal(x.Element("id")),
                Status = (int)GetValueDecimal(x.Element("status")),
                PaymentType = GetValueString(x.Element("reserved-str-2")),
                ExchangeRate = (double)GetValueDecimal(x.Element("exch-rate")),
                GrossSum = (double)GetValueDecimal(x.Element("gross-sum")),
                Name = (GetValueString(x.Element("fname")) + " " + GetValueString(x.Element("lname"))).Trim(),
                Email = GetValueString(x.Element("email")),
                Date = GetValueDateTime(x.Element("payment-date")),
                Price = GetValueDecimal(x.Element("price")),
                Currency = GetValueString(x.Element("payment-currency")),
                Method = GetValueString(x.Element("payment-method")),
                CartId = GetValueString(x.Element("cart-id")),
                ProductId = GetValueString(x.Element("product-ref")),
            };
        }

        private string ToUrl(string s)
        {
            s = s.Trim();
            if (s.StartsWith("error", StringComparison.InvariantCultureIgnoreCase))
            {
                return string.Empty;
            }
            if (test && !s.Contains("&DOTEST = 1"))
            {
                s += "&DOTEST=1";
            }
            return s;
        }

        private string GetValueString(XElement xelement)
        {
            return xelement != null ? xelement.Value : default(string);
        }

        private DateTime GetValueDateTime(XElement xelement)
        {
            return xelement != null ?
                DateTime.ParseExact(xelement.Value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) :
                default(DateTime);
        }

        private Decimal GetValueDecimal(XElement xelement)
        {
            if (xelement == null || string.IsNullOrEmpty(xelement.Value))
            {
                return default(Decimal);
            }
            var sep = CultureInfo.InvariantCulture.NumberFormat.CurrencyDecimalSeparator;
            var value = Decimal.Zero;
            return Decimal.TryParse(xelement.Value.Replace(".", sep).Replace(",", sep), NumberStyles.Currency, CultureInfo.InvariantCulture, out value) ? value : default(Decimal);
        }

        void IDisposable.Dispose()
        {
            try
            {
                Close();
            }
            catch (CommunicationException)
            {
                Abort();
            }
            catch (TimeoutException)
            {
                Abort();
            }
            catch (Exception)
            {
                Abort();
                throw;
            }
        }
    }


    [ServiceContract]
    public interface IService
    {
        [OperationContract]
        Message Request(Message message);
    }

    [DataContract(Name = "Message", Namespace = "http://schemas.datacontract.org/2004/07/teamlabservice")]
    [Serializable]
    public class Message
    {
        [DataMember]
        public string Content
        {
            get;
            set;
        }

        [DataMember]
        public MessageType Type
        {
            get;
            set;
        }
    }

    [DataContract(Name = "MessageType", Namespace = "http://schemas.datacontract.org/2004/07/teamlabservice")]
    public enum MessageType
    {
        [EnumMember]
        Undefined = 0,

        [EnumMember]
        Data = 1,

        [EnumMember]
        Error = 2,
    }

    [Serializable]
    public class BillingException : Exception
    {
        public BillingException(string message)
            : base(message)
        {
        }

        protected BillingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
