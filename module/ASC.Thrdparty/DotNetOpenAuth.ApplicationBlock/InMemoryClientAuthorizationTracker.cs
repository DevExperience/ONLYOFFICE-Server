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

//-----------------------------------------------------------------------
// <copyright file="InMemoryClientAuthorizationTracker.cs" company="Outercurve Foundation">
//     Copyright (c) Outercurve Foundation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace DotNetOpenAuth.ApplicationBlock {
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.ServiceModel;
	using System.Text;
	using System.Threading;
	using DotNetOpenAuth.Messaging;
	using DotNetOpenAuth.OAuth2;

#if SAMPLESONLY
	internal class InMemoryClientAuthorizationTracker : IClientAuthorizationTracker {
		private readonly Dictionary<int, IAuthorizationState> savedStates = new Dictionary<int, IAuthorizationState>();
		private int stateCounter;

		#region Implementation of IClientTokenManager

		/// <summary>
		/// Gets the state of the authorization for a given callback URL and client state.
		/// </summary>
		/// <param name="callbackUrl">The callback URL.</param>
		/// <param name="clientState">State of the client stored at the beginning of an authorization request.</param>
		/// <returns>The authorization state; may be <c>null</c> if no authorization state matches.</returns>
		public IAuthorizationState GetAuthorizationState(Uri callbackUrl, string clientState) {
			IAuthorizationState state;
			if (this.savedStates.TryGetValue(int.Parse(clientState), out state)) {
				if (state.Callback != callbackUrl) {
					throw new DotNetOpenAuth.Messaging.ProtocolException("Client state and callback URL do not match.");
				}
			}

			return state;
		}

		#endregion

		internal IAuthorizationState NewAuthorization(HashSet<string> scope, out string clientState) {
			int counter = Interlocked.Increment(ref this.stateCounter);
			clientState = counter.ToString(CultureInfo.InvariantCulture);
			return this.savedStates[counter] = new AuthorizationState(scope);
		}
	}
#endif
}
