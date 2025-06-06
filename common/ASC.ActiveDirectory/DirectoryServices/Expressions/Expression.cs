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

namespace ASC.ActiveDirectory.Expressions
{
    /// <summary>
    
    /// </summary>
    public class Expression : ICloneable
    {
        private Op _op;
        private bool _negative = false;
        private string _attributeName;
        private string _attributeValue;

        internal Expression() { }

        /// <summary>
        
        /// </summary>
        
        
        public Expression(string attrbuteName, Op op)
        {
            if (op != Op.Exists && op != Op.NotExists)
                throw new ArgumentException("op");

            if (String.IsNullOrEmpty(attrbuteName))
                throw new ArgumentException("attrbuteName");

            _op = op;
            _attributeName = attrbuteName;
            _attributeValue = "*";
        }

        /// <summary>
        
        /// </summary>
        
        
        
        public Expression(string attrbuteName, Op op, string attrbuteValue)
        {
            if (op == Op.Exists || op == Op.NotExists)
                throw new ArgumentException("op");

            if (String.IsNullOrEmpty(attrbuteName))
                throw new ArgumentException("attrbuteName");

            _op = op;
            _attributeName = attrbuteName;
            _attributeValue = attrbuteValue;
        }

        /// <summary>
        
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string sop = string.Empty;
            switch(_op)
            {
                case Op.NotExists:
                case Op.Exists:
                case Op.Equal:
                case Op.NotEqual:
                    sop = "=";
                    break;
                case Op.Greater:

                    sop =">";
                    break;
                case Op.GreaterOrEqual:
                    sop =">=";
                    break;
                case Op.Less:
                    sop ="<";
                    break;
                case Op.LessOrEqual:
                    sop ="<=";
                    break;
            }
                            
            string expressionString = "({0}{1}{2}{3})";
            expressionString = 
                String.Format(
                    expressionString,
                    
                    ((((int)_op & 0x010000) == 0x010000 &&  !_negative) || _negative) ? "!" : "",
                    _attributeName,
                    sop,
                    _attributeValue
                );

            return expressionString;
        }

        /// <summary>
        
        /// </summary>
        
        public Expression Negative()
        {
            _negative = !_negative;
            return this;
        }

        #region вспомогательные 
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attrbuteName"></param>
        /// <returns></returns>
        public static Expression Exists(string attrbuteName)
        { return new Expression(attrbuteName, Op.Exists); }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attrbuteName"></param>
        /// <returns></returns>
        public static Expression NotExists(string attrbuteName)
        { return new Expression(attrbuteName, Op.NotExists); }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attrbuteName"></param>
        /// <param name="attrbuteValue"></param>
        /// <returns></returns>
        public static Expression Equal(string attrbuteName, string attrbuteValue)
        { return new Expression(attrbuteName, Op.Equal,attrbuteValue); }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attrbuteName"></param>
        /// <param name="attrbuteValue"></param>
        /// <returns></returns>
        public static Expression NotEqual(string attrbuteName, string attrbuteValue)
        { return new Expression(attrbuteName, Op.NotEqual, attrbuteValue); }
        #endregion

        #region ICloneable Members

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            return MemberwiseClone();
        }

        #endregion
    }
}
