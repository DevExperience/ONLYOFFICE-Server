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

// // --------------------------------------------------------------------------------------------------------------------
// // <copyright company="Ascensio System Limited" file="Range.cs">
// //   
// // </copyright>
// // <summary>
// //   (c) Copyright Ascensio System Limited 2008-2012
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

using ASC.Xmpp.Core.utils.Xml.Dom;

namespace ASC.Xmpp.Core.protocol.extensions.filetransfer
{
    /// <summary>
    ///   When range is sent in the offer, it should have no attributes. This signifies that the sender can do ranged transfers. When no range element is sent in the Stream Initiation result, the Sender MUST send the complete file starting at offset 0. More generally, data is sent over the stream byte for byte starting at the offset position for the length specified.
    /// </summary>
    public class Range : Element
    {
        /*
		<range offset='252' length='179'/>		    	
		*/

        public Range()
        {
            TagName = "range";
            Namespace = Uri.SI_FILE_TRANSFER;
        }

        public Range(long offset, long length) : this()
        {
            Offset = offset;
            Length = length;
        }

        /// <summary>
        ///   Specifies the position, in bytes, to start transferring the file data from. This defaults to zero (0) if not specified.
        /// </summary>
        public long Offset
        {
            get { return GetAttributeLong("offset"); }
            set { SetAttribute("offset", value.ToString()); }
        }

        /// <summary>
        ///   Specifies the number of bytes to retrieve starting at offset. This defaults to the length of the file from offset to the end.
        /// </summary>
        public long Length
        {
            get { return GetAttributeLong("length"); }
            set { SetAttribute("length", value.ToString()); }
        }
    }
}