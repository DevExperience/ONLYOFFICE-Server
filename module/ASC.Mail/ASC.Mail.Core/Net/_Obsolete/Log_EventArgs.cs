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

namespace ASC.Mail.Net
{
    /// <summary>
    /// Provides data for the SessionLog event.
    /// </summary>
    public class Log_EventArgs
    {
        #region Members

        private readonly bool m_FirstLogPart = true;
        private readonly bool m_LastLogPart;
        private readonly SocketLogger m_pLoggger;

        #endregion

        #region Properties

        /// <summary>
        /// Gets log text.
        /// </summary>
        public string LogText
        {
            get { return SocketLogger.LogEntriesToString(m_pLoggger, m_FirstLogPart, m_LastLogPart); }
        }

        /// <summary>
        /// Gets logger.
        /// </summary>
        public SocketLogger Logger
        {
            get { return m_pLoggger; }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="logger">Socket logger.</param>
        /// <param name="firstLogPart">Specifies if first log part of multipart log.</param>
        /// <param name="lastLogPart">Specifies if last log part (logging ended).</param>
        public Log_EventArgs(SocketLogger logger, bool firstLogPart, bool lastLogPart)
        {
            m_pLoggger = logger;
            m_FirstLogPart = firstLogPart;
            m_LastLogPart = lastLogPart;
        }

        #endregion
    }
}