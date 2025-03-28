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

namespace ASC.Mail.Net.SIP.Message
{
    #region usings

    using System;
    using System.Net;
    using System.Text;
    using Stack;

    #endregion

    /// <summary>
    /// Implements SIP "via-parm" value. Defined in RFC 3261.
    /// </summary>
    /// <remarks>
    /// <code>
    /// RFC 3261 Syntax:
    ///     via-parm          =  sent-protocol LWS sent-by *( SEMI via-params )
    ///     via-params        =  via-ttl / via-maddr / via-received / via-branch / via-extension
    ///     via-ttl           =  "ttl" EQUAL ttl
    ///     via-maddr         =  "maddr" EQUAL host
    ///     via-received      =  "received" EQUAL (IPv4address / IPv6address)
    ///     via-branch        =  "branch" EQUAL token
    ///     via-extension     =  generic-param
    ///     sent-protocol     =  protocol-name SLASH protocol-version SLASH transport
    ///     protocol-name     =  "SIP" / token
    ///     protocol-version  =  token
    ///     transport         =  "UDP" / "TCP" / "TLS" / "SCTP" / other-transport
    ///     sent-by           =  host [ COLON port ]
    ///     ttl               =  1*3DIGIT ; 0 to 255
    ///         
    ///     Via extentions:
    ///       // RFC 3486
    ///       via-compression  =  "comp" EQUAL ("sigcomp" / other-compression)
    ///       // RFC 3581
    ///       response-port  =  "rport" [EQUAL 1*DIGIT]
    /// </code>
    /// </remarks>
    public class SIP_t_ViaParm : SIP_t_ValueWithParams
    {
        #region Members

        private string m_ProtocolName = "";
        private string m_ProtocolTransport = "";
        private string m_ProtocolVersion = "";
        private HostEndPoint m_pSentBy;

        #endregion

        #region Properties

        /// <summary>
        /// Gets sent protocol name. Normally this is always SIP.
        /// </summary>
        public string ProtocolName
        {
            get { return m_ProtocolName; }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("Property ProtocolName can't be null or empty !");
                }

                m_ProtocolName = value;
            }
        }

        /// <summary>
        /// Gets sent protocol version. 
        /// </summary>
        public string ProtocolVersion
        {
            get { return m_ProtocolVersion; }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("Property ProtocolVersion can't be null or empty !");
                }

                m_ProtocolVersion = value;
            }
        }

        /// <summary>
        /// Gets sent protocol transport. Currently known values are: UDP,TCP,TLS,SCTP. This value is always in upper-case.
        /// </summary>
        public string ProtocolTransport
        {
            get { return m_ProtocolTransport.ToUpper(); }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("Property ProtocolTransport can't be null or empty !");
                }

                m_ProtocolTransport = value;
            }
        }

        /// <summary>
        /// Gets host name or IP with optional port. Examples: lumiosft.ee,10.0.0.1:80.
        /// </summary>
        /// <exception cref="ArgumentNullException">Is raised when null reference passed.</exception>
        public HostEndPoint SentBy
        {
            get { return m_pSentBy; }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                m_pSentBy = value;
            }
        }

        /// <summary>
        /// Gets sent-by port, if no port explicity set, transport default is returned.
        /// </summary>
        public int SentByPortWithDefault
        {
            get
            {
                if (m_pSentBy.Port != -1)
                {
                    return m_pSentBy.Port;
                }
                else
                {
                    if (ProtocolTransport == SIP_Transport.TLS)
                    {
                        return 5061;
                    }
                    else
                    {
                        return 5060;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets 'branch' parameter value. The branch parameter in the Via header field values 
        /// serves as a transaction identifier. The value of the branch parameter MUST start
        /// with the magic cookie "z9hG4bK". Value null means that branch paramter doesn't exist.
        /// </summary>
        public string Branch
        {
            get
            {
                SIP_Parameter parameter = Parameters["branch"];
                if (parameter != null)
                {
                    return parameter.Value;
                }
                else
                {
                    return null;
                }
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    Parameters.Remove("branch");
                }
                else
                {
                    if (!value.StartsWith("z9hG4bK"))
                    {
                        throw new ArgumentException(
                            "Property Branch value must start with magic cookie 'z9hG4bK' !");
                    }

                    Parameters.Set("branch", value);
                }
            }
        }

        /// <summary>
        /// Gets or sets 'comp' parameter value. Value null means not specified. Defined in RFC 3486.
        /// </summary>
        public string Comp
        {
            get
            {
                SIP_Parameter parameter = Parameters["comp"];
                if (parameter != null)
                {
                    return parameter.Value;
                }
                else
                {
                    return null;
                }
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    Parameters.Remove("comp");
                }
                else
                {
                    Parameters.Set("comp", value);
                }
            }
        }

        /// <summary>
        /// Gets or sets 'maddr' parameter value. Value null means not specified.
        /// </summary>
        public string Maddr
        {
            get
            {
                SIP_Parameter parameter = Parameters["maddr"];
                if (parameter != null)
                {
                    return parameter.Value;
                }
                else
                {
                    return null;
                }
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    Parameters.Remove("maddr");
                }
                else
                {
                    Parameters.Set("maddr", value);
                }
            }
        }

        /// <summary>
        /// Gets or sets 'received' parameter value. Value null means not specified.
        /// </summary>
        public IPAddress Received
        {
            get
            {
                SIP_Parameter parameter = Parameters["received"];
                if (parameter != null)
                {
                    return IPAddress.Parse(parameter.Value);
                }
                else
                {
                    return null;
                }
            }

            set
            {
                if (value == null)
                {
                    Parameters.Remove("received");
                }
                else
                {
                    Parameters.Set("received", value.ToString());
                }
            }
        }

        /// <summary>
        /// Gets or sets 'rport' parameter value. Value -1 means not specified and value 0 means empty "" rport.
        /// </summary>
        public int RPort
        {
            get
            {
                SIP_Parameter parameter = Parameters["rport"];
                if (parameter != null)
                {
                    if (parameter.Value == "")
                    {
                        return 0;
                    }
                    else
                    {
                        return Convert.ToInt32(parameter.Value);
                    }
                }
                else
                {
                    return -1;
                }
            }

            set
            {
                if (value < 0)
                {
                    Parameters.Remove("rport");
                }
                else if (value == 0)
                {
                    Parameters.Set("rport", "");
                }
                else
                {
                    Parameters.Set("rport", value.ToString());
                }
            }
        }

        /// <summary>
        /// Gets or sets 'ttl' parameter value. Value -1 means not specified.
        /// </summary>
        public int Ttl
        {
            get
            {
                SIP_Parameter parameter = Parameters["ttl"];
                if (parameter != null)
                {
                    return Convert.ToInt32(parameter.Value);
                }
                else
                {
                    return -1;
                }
            }

            set
            {
                if (value < 0)
                {
                    Parameters.Remove("ttl");
                }
                else
                {
                    Parameters.Set("ttl", value.ToString());
                }
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Defualt constructor.
        /// </summary>
        public SIP_t_ViaParm()
        {
            m_ProtocolName = "SIP";
            m_ProtocolVersion = "2.0";
            m_ProtocolTransport = "UDP";
            m_pSentBy = new HostEndPoint("localhost", -1);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates new branch paramter value.
        /// </summary>
        /// <returns></returns>
        public static string CreateBranch()
        {
            // The value of the branch parameter MUST start with the magic cookie "z9hG4bK".

            return "z9hG4bK-" + Guid.NewGuid().ToString().Replace("-", "");
        }

        /// <summary>
        /// Parses "via-parm" from specified value.
        /// </summary>
        /// <param name="value">SIP "via-parm" value.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public void Parse(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("reader");
            }

            Parse(new StringReader(value));
        }

        /// <summary>
        /// Parses "via-parm" from specified reader.
        /// </summary>
        /// <param name="reader">Reader from where to parse.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public override void Parse(StringReader reader)
        {
            /*
                via-parm          =  sent-protocol LWS sent-by *( SEMI via-params )
                via-params        =  via-ttl / via-maddr / via-received / via-branch / via-extension
                via-ttl           =  "ttl" EQUAL ttl
                via-maddr         =  "maddr" EQUAL host
                via-received      =  "received" EQUAL (IPv4address / IPv6address)
                via-branch        =  "branch" EQUAL token
                via-extension     =  generic-param
                sent-protocol     =  protocol-name SLASH protocol-version
                                     SLASH transport
                protocol-name     =  "SIP" / token
                protocol-version  =  token
                transport         =  "UDP" / "TCP" / "TLS" / "SCTP" / other-transport
                sent-by           =  host [ COLON port ]
                ttl               =  1*3DIGIT ; 0 to 255
             
                Via extentions:
                // RFC 3486
                via-compression  =  "comp" EQUAL ("sigcomp" / other-compression)
                // RFC 3581
                response-port  =  "rport" [EQUAL 1*DIGIT]
             
                Examples:
                Via: SIP/2.0/UDP 127.0.0.1:58716;branch=z9hG4bK-d87543-4d7e71216b27df6e-1--d87543-
                // Specifically, LWS on either side of the ":" or "/" is allowed.
                Via: SIP / 2.0 / UDP 127.0.0.1:58716;branch=z9hG4bK-d87543-4d7e71216b27df6e-1--d87543-
            */

            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            // protocol-name
            string word = reader.QuotedReadToDelimiter('/');
            if (word == null)
            {
                throw new SIP_ParseException("Via header field protocol-name is missing !");
            }
            ProtocolName = word.Trim();
            // protocol-version
            word = reader.QuotedReadToDelimiter('/');
            if (word == null)
            {
                throw new SIP_ParseException("Via header field protocol-version is missing !");
            }
            ProtocolVersion = word.Trim();
            // transport
            word = reader.ReadWord();
            if (word == null)
            {
                throw new SIP_ParseException("Via header field transport is missing !");
            }
            ProtocolTransport = word.Trim();
            // sent-by
            word = reader.QuotedReadToDelimiter(new[] {';', ','}, false);
            if (word == null)
            {
                throw new SIP_ParseException("Via header field sent-by is missing !");
            }
            SentBy = HostEndPoint.Parse(word.Trim());

            // Parse parameters
            ParseParameters(reader);
        }

        /// <summary>
        /// Converts this to valid "via-parm" value.
        /// </summary>
        /// <returns>Returns "via-parm" value.</returns>
        public override string ToStringValue()
        {
            /*
                Via               =  ( "Via" / "v" ) HCOLON via-parm *(COMMA via-parm)
                via-parm          =  sent-protocol LWS sent-by *( SEMI via-params )
                via-params        =  via-ttl / via-maddr / via-received / via-branch / via-extension
                via-ttl           =  "ttl" EQUAL ttl
                via-maddr         =  "maddr" EQUAL host
                via-received      =  "received" EQUAL (IPv4address / IPv6address)
                via-branch        =  "branch" EQUAL token
                via-extension     =  generic-param
                sent-protocol     =  protocol-name SLASH protocol-version
                                     SLASH transport
                protocol-name     =  "SIP" / token
                protocol-version  =  token
                transport         =  "UDP" / "TCP" / "TLS" / "SCTP" / other-transport
                sent-by           =  host [ COLON port ]
                ttl               =  1*3DIGIT ; 0 to 255
             
                Via extentions:
                // RFC 3486
                via-compression  =  "comp" EQUAL ("sigcomp" / other-compression)
                // RFC 3581
                response-port  =  "rport" [EQUAL 1*DIGIT]
             
                Examples:
                Via: SIP/2.0/UDP 127.0.0.1:58716;branch=z9hG4bK-d87543-4d7e71216b27df6e-1--d87543-
                // Specifically, LWS on either side of the ":" or "/" is allowed.
                Via: SIP / 2.0 / UDP 127.0.0.1:58716;branch=z9hG4bK-d87543-4d7e71216b27df6e-1--d87543-
            */

            StringBuilder retVal = new StringBuilder();
            retVal.Append(ProtocolName + "/" + ProtocolVersion + "/" + ProtocolTransport + " ");
            retVal.Append(SentBy.ToString());
            retVal.Append(ParametersToString());

            return retVal.ToString();
        }

        #endregion
    }
}