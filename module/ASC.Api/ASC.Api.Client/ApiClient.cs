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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Web;
using System.Xml.Linq;

namespace ASC.Api.Client
{
    public class ApiClient
    {
        public string Host { get; private set; }

        public string ApiRoot { get; private set; }

        public UriScheme UriScheme { get; set; }

        public ApiClient(string host)
            : this(host, ApiClientConfiguration.GetSection())
        {

        }

        public ApiClient(string host, ApiClientConfiguration config)
            : this(host, config.ApiRoot, config.UriScheme)
        {
            
        }

        public ApiClient(string host, string apiRoot, UriScheme uriScheme)
        {
            if (apiRoot.Length > 0 && apiRoot[0] == '~')
            {
                if (HttpContext.Current != null && HttpContext.Current.Request.Cookies.Get("asc_auth_key") != null)
                    apiRoot = VirtualPathUtility.ToAbsolute(apiRoot);
                else
                    apiRoot = apiRoot.TrimStart('~');
            }

            Host = host;
            ApiRoot = apiRoot.Trim('/');
            UriScheme = uriScheme;
        }

        public string Authorize(string email, string password)
        {
            ApiResponse response = GetResponse(
                new ApiRequest("authentication")
                    .WithMethod(HttpMethod.Post)
                    .WithResponseType(ResponseType.Xml)
                    .WithParameter("userName", email)
                    .WithParameter("password", password));

            var xml = XElement.Load(new StringReader(response.Response));
            return xml.Element("token").Value;
        }

        public ApiResponse GetResponse(ApiRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            HttpWebRequest webRequest = CreateWebRequest(request);
            HttpWebResponse webResponse;
            try
            {
                webResponse = (HttpWebResponse)webRequest.GetResponse();
            }
            catch (WebException exception)
            {
                if (exception.Response == null)
                    throw;

                webResponse = (HttpWebResponse)exception.Response;
                if (new ContentType(webResponse.ContentType).MediaType == "text/html") // generic http error
                    throw new HttpErrorException((int)webResponse.StatusCode, webResponse.StatusDescription);
            }

            using (webResponse)
            using (var responseStream = webResponse.GetResponseStream())
            {
                if (new ContentType(webResponse.ContentType).MediaType == "application/json")
                {
                    return ResponseParser.ParseJsonResponse(responseStream);
                }

                return ResponseParser.ParseXmlResponse(responseStream);
            }
        }

        private HttpWebRequest CreateWebRequest(ApiRequest request)
        {
            var uri = new UriBuilder(UriScheme.ToString().ToLower(), Host) {Path = CreatePath(request)};

            if (request.Method == HttpMethod.Get || request.Method == HttpMethod.Delete)
                uri.Query = CreateQuery(request);
            
            var httpRequest = (HttpWebRequest)WebRequest.Create(uri.ToString());
            httpRequest.Method = request.Method.ToString().ToUpper();
            httpRequest.AllowAutoRedirect = true;

            if (!string.IsNullOrWhiteSpace(request.AuthToken))
                httpRequest.Headers["Authorization"] = request.AuthToken;

            httpRequest.Headers.Add(request.Headers);

            if (request.Method != HttpMethod.Get && request.Method != HttpMethod.Delete)
            {
                if (request.Files.Any())
                {
                    WriteMultipart(httpRequest, request);
                }
                else
                {
                    WriteUrlencoded(httpRequest, request);
                }
            }

            return httpRequest;
        }

        private string CreatePath(ApiRequest request)
        {
            return ApiRoot + "/" + request.Url + "." + request.ResponseType.ToString().ToLower();
        }

        private string CreateQuery(ApiRequest request)
        {
            var sb = new StringBuilder();
            foreach (var parameter in EnumerateParameters(request))
            {
                sb.AppendFormat("{0}={1}&", parameter.Name, parameter.Value);
            }

            if (sb.Length > 0) 
                sb.Remove(sb.Length - 1, 1);
            
            return sb.ToString();
        }

        private void WriteMultipart(HttpWebRequest httpRequest, ApiRequest request)
        {
            string boundary = DateTime.UtcNow.Ticks.ToString("x");

            httpRequest.ContentType = "multipart/form-data; boundary=" + boundary;

            using (var requestStream = httpRequest.GetRequestStream())
            {
                foreach (var parameter in EnumerateParameters(request))
                {
                    requestStream.WriteString("\r\n--{0}\r\nContent-Disposition: form-data; name=\"{1}\";\r\n\r\n{2}", boundary, parameter.Name, parameter.Value);
                }

                foreach (var file in request.Files)
                {
                    requestStream.WriteString("\r\n--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\nContent-Transfer-Encoding: binary\r\n\r\n",
                                              boundary,
                                              Path.GetFileNameWithoutExtension(file.Name),
                                              Path.GetFileName(file.Name),
                                              !string.IsNullOrEmpty(file.ContentType) ? file.ContentType : "application/octet-stream");

                    file.Data.CopyTo(requestStream);
                }

                requestStream.WriteString("\r\n--{0}--\r\n", boundary);
            }
        }

        private void WriteUrlencoded(HttpWebRequest httpRequest, ApiRequest request)
        {
            httpRequest.ContentType = "application/x-www-form-urlencoded";

            using (var requestStream = httpRequest.GetRequestStream())
            using (var streamWriter = new StreamWriter(requestStream))
            {
                streamWriter.Write(CreateQuery(request));
            }
        }

        private static IEnumerable<RequestParameter> EnumerateParameters(ApiRequest request)
        {
            foreach (var parameter in request.Parameters.Where(parameter => !string.IsNullOrEmpty(parameter.Name) && parameter.Value != null))
            {
                var enumerable = parameter.Value as IEnumerable;
                if (enumerable != null && !(parameter.Value is string))
                {
                    foreach (var value in enumerable)
                    {
                        yield return new RequestParameter {Name = parameter.Name + "[]", Value = value};
                    }
                }
                else if (parameter.Value is DateTime)
                {
                    yield return new RequestParameter {Name = parameter.Name, Value = ((DateTime)parameter.Value).ToString("o")};
                }
                else
                {
                    yield return parameter;
                }
            }
        }
    }
}
