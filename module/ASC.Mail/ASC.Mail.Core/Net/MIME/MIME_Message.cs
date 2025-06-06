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

namespace ASC.Mail.Net.MIME
{
    #region usings

    using System;
    using System.Collections.Generic;
    using System.IO;
    using IO;

    #endregion

    /// <summary>
    /// Represents a MIME message. Defined in RFC 2045 2.3.
    /// </summary>
    public class MIME_Message : MIME_Entity
    {
        #region Members

        private bool m_IsDisposed = false;

        #endregion

        #region Properties

        /// <summary>
        /// Gets all MIME entities as list.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is disposed and this property is accessed.</exception>
        public MIME_Entity[] AllEntities
        {
            get
            {
                if (m_IsDisposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }

                List<MIME_Entity> retVal = new List<MIME_Entity>();
                Queue<MIME_Entity> entitiesQueue = new Queue<MIME_Entity>();
                entitiesQueue.Enqueue(this);

                while (entitiesQueue.Count > 0)
                {
                    MIME_Entity currentEntity = entitiesQueue.Dequeue();

                    // Current entity is multipart entity, add it's body-parts for processing.
                    if (Body != null && currentEntity.Body.GetType().IsSubclassOf(typeof (MIME_b_Multipart)))
                    {
                        foreach (MIME_Entity bodypart in ((MIME_b_Multipart) currentEntity.Body).BodyParts)
                        {
                            entitiesQueue.Enqueue(bodypart);
                        }
                    }

                    retVal.Add(currentEntity);
                }

                return retVal.ToArray();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Parses MIME message from the specified file.
        /// </summary>
        /// <param name="file">File name with path from where to parse MIME message.</param>
        /// <returns>Returns parsed MIME message.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>file</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public static MIME_Message ParseFromFile(string file)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }
            if (file == "")
            {
                throw new ArgumentException("Argument 'file' value must be specified.");
            }

            return ParseFromStream(File.OpenRead(file));
        }

        /// <summary>
        /// Parses MIME message from the specified stream.
        /// </summary>
        /// <param name="stream">Stream from where to parse MIME message. Parsing starts from current stream position.</param>
        /// <returns>Returns parsed MIME message.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> is null.</exception>
        public static MIME_Message ParseFromStream(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            MIME_Message retVal = new MIME_Message();
            retVal.Parse(new SmartStream(stream, false), new MIME_h_ContentType("text/plain"));

            return retVal;
        }

        /// <summary>
        /// Creates attachment entity.
        /// </summary>
        /// <param name="file">File name with optional path.</param>
        /// <returns>Returns created attachment entity.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>file</b> is null reference.</exception>
        public static MIME_Entity CreateAttachment(string file)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }

            MIME_Entity retVal = new MIME_Entity();
            MIME_b_Application body = new MIME_b_Application(MIME_MediaTypes.Application.octet_stream);
            retVal.Body = body;
            body.SetBodyDataFromFile(file, MIME_TransferEncodings.Base64);
            retVal.ContentType.Param_Name = Path.GetFileName(file);

            FileInfo fileInfo = new FileInfo(file);
            MIME_h_ContentDisposition disposition =
                new MIME_h_ContentDisposition(MIME_DispositionTypes.Attachment);
            disposition.Param_FileName = Path.GetFileName(file);
            disposition.Param_Size = fileInfo.Length;
            disposition.Param_CreationDate = fileInfo.CreationTime;
            disposition.Param_ModificationDate = fileInfo.LastWriteTime;
            disposition.Param_ReadDate = fileInfo.LastAccessTime;
            retVal.ContentDisposition = disposition;

            return retVal;
        }

        /// <summary>
        /// Gets MIME entity with the specified Content-ID. Returns null if no such entity.
        /// </summary>
        /// <param name="cid">Content ID.</param>
        /// <returns>Returns MIME entity with the specified Content-ID or null if no such entity.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this class is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>cid</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public MIME_Entity GetEntityByCID(string cid)
        {
            if (m_IsDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
            if (cid == null)
            {
                throw new ArgumentNullException("cid");
            }
            if (cid == "")
            {
                throw new ArgumentException("Argument 'cid' value must be specified.", "cid");
            }

            foreach (MIME_Entity entity in AllEntities)
            {
                if (entity.ContentID == cid)
                {
                    return entity;
                }
            }

            return null;
        }

        #endregion

        // TODO:
        //public MIME_Entity GetEntityByPartsSpecifier(string partsSpecifier)
    }
}