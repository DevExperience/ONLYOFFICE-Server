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

namespace ASC.Mail.Net.Dns.Client
{
    /// <summary>
    /// HINFO record.
    /// </summary>
    public class DNS_rr_HINFO : DNS_rr_base
    {
        #region Members

        private readonly string m_CPU = "";
        private readonly string m_OS = "";

        #endregion

        #region Properties

        /// <summary>
        /// Gets host's CPU.
        /// </summary>
        public string CPU
        {
            get { return m_CPU; }
        }

        /// <summary>
        /// Gets host's OS.
        /// </summary>
        public string OS
        {
            get { return m_OS; }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="cpu">Host CPU.</param>
        /// <param name="os">Host OS.</param>
        /// <param name="ttl">TTL value.</param>
        public DNS_rr_HINFO(string cpu, string os, int ttl) : base(QTYPE.HINFO, ttl)
        {
            m_CPU = cpu;
            m_OS = os;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Parses resource record from reply data.
        /// </summary>
        /// <param name="reply">DNS server reply data.</param>
        /// <param name="offset">Current offset in reply data.</param>
        /// <param name="rdLength">Resource record data length.</param>
        /// <param name="ttl">Time to live in seconds.</param>
        public static DNS_rr_HINFO Parse(byte[] reply, ref int offset, int rdLength, int ttl)
        {
            /* RFC 1035 3.3.2. HINFO RDATA format

			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			/                      CPU                      /
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			/                       OS                      /
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			
			CPU     A <character-string> which specifies the CPU type.

			OS      A <character-string> which specifies the operating
					system type.
					
					Standard values for CPU and OS can be found in [RFC-1010].

			*/

            // CPU
            string cpu = Dns_Client.ReadCharacterString(reply, ref offset);

            // OS
            string os = Dns_Client.ReadCharacterString(reply, ref offset);

            return new DNS_rr_HINFO(cpu, os, ttl);
        }

        #endregion
    }
}