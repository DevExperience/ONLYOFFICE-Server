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
using System.Runtime.Serialization;
using ASC.Api.Enums;
using ASC.Api.Impl;

#endregion

namespace ASC.Api.Interfaces
{
    public interface IApiStandartResponce
    {
        object Response { get; set; }
        ErrorWrapper Error { get; set; }
        ApiStatus Status { get; set; }
        long Code { get; set; }
        long Count { get; set; }
        long StartIndex { get; set; }
        long? NextPage { get; set; }
        long? TotalCount { get; set; }
        ApiContext ApiContext { get; set; }
    }

    [DataContract(Name = "error", Namespace = "")]
    public class ErrorWrapper
    {
        public ErrorWrapper()
        {
        }

        public ErrorWrapper(Exception exception)
        {
            //Unwrap
            while (exception.InnerException != null)
            {
                exception = exception.InnerException;
            }
            Message = exception.Message;
#if (DEBUG)
            Type = exception.GetType().ToString();
            Stack = exception.StackTrace;
#endif
        }

        [DataMember(Name = "message", EmitDefaultValue = false, Order = 2)]
        public string Message { get; set; }

        [DataMember(Name = "type", EmitDefaultValue = false, Order = 3)]
        public string Type { get; set; }

        [DataMember(Name = "stack", EmitDefaultValue = false, Order = 3)]
        public string Stack { get; set; }
    }
}