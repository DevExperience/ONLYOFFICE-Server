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
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace ASC.Web.Core.Helpers
{
    /// <summary>
    /// Defines a type converter for enum values that converts enum values to 
    /// and from string representations using resources
    /// </summary>
    /// <remarks>
    /// This class makes localization of display values for enums in a project easy.  Simply
    /// derive a class from this class and pass the ResourceManagerin the constructor.  
    /// 
    /// Then define the enum values in the resource editor.   The names of
    /// the resources are simply the enum value prefixed by the enum type name with an
    /// underscore separator eg MyEnum_MyValue.  You can then use the TypeConverter attribute
    /// to make the LocalizedEnumConverter the default TypeConverter for the enums in your
    /// project.
    /// </remarks>
    public class ResourceEnumConverter : System.ComponentModel.EnumConverter
    {
        private class LookupTable : Dictionary<string, object> { }
        private Dictionary<CultureInfo, LookupTable> _lookupTables = new Dictionary<CultureInfo, LookupTable>();
        private System.Resources.ResourceManager _resourceManager;
        private bool _isFlagEnum = false;
        private Array _flagValues;

        /// <summary>
        /// Get the lookup table for the given culture (creating if necessary)
        /// </summary>
        /// <param name="culture"></param>
        /// <returns></returns>
        private LookupTable GetLookupTable(CultureInfo culture)
        {
            LookupTable result = null;
            if (culture == null)
                culture = CultureInfo.CurrentCulture;

            if (!_lookupTables.TryGetValue(culture, out result))
            {
                result = new LookupTable();
                foreach (object value in GetStandardValues())
                {
                    string text = GetValueText(culture, value);
                    if (text != null)
                    {
                        result.Add(text, value);
                    }
                }
                _lookupTables.Add(culture, result);
            }
            return result;
        }

        /// <summary>
        /// Return the text to display for a simple value in the given culture
        /// </summary>
        /// <param name="culture">The culture to get the text for</param>
        /// <param name="value">The enum value to get the text for</param>
        /// <returns>The localized text</returns>
        private string GetValueText(CultureInfo culture, object value)
        {
            Type type = value.GetType();
            string resourceName = string.Format("{0}_{1}", type.Name, value.ToString());
            string result = _resourceManager.GetString(resourceName, culture);
            if (result == null)
                result = resourceName;
            return result;
        }

        /// <summary>
        /// Return true if the given value is can be represented using a single bit
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool IsSingleBitValue(ulong value)
        {
            switch (value)
            {
                case 0:
                    return false;
                case 1:
                    return true;
            }
            return ((value & (value - 1)) == 0);
        }

        /// <summary>
        /// Return the text to display for a flag value in the given culture
        /// </summary>
        /// <param name="culture">The culture to get the text for</param>
        /// <param name="value">The flag enum value to get the text for</param>
        /// <returns>The localized text</returns>
        private string GetFlagValueText(CultureInfo culture, object value)
        {
            // if there is a standard value then use it
            //
            if (Enum.IsDefined(value.GetType(), value))
            {
                return GetValueText(culture, value);
            }

            // otherwise find the combination of flag bit values
            // that makes up the value
            //
            ulong lValue = Convert.ToUInt32(value);
            string result = null;
            foreach (object flagValue in _flagValues)
            {
                ulong lFlagValue = Convert.ToUInt32(flagValue);
                if (IsSingleBitValue(lFlagValue))
                {
                    if ((lFlagValue & lValue) == lFlagValue)
                    {
                        string valueText = GetValueText(culture, flagValue);
                        if (result == null)
                        {
                            result = valueText;
                        }
                        else
                        {
                            result = string.Format("{0}, {1}", result, valueText);
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Return the Enum value for a simple (non-flagged enum)
        /// </summary>
        /// <param name="culture">The culture to convert using</param>
        /// <param name="text">The text to convert</param>
        /// <returns>The enum value</returns>
        private object GetValue(CultureInfo culture, string text)
        {
            LookupTable lookupTable = GetLookupTable(culture);
            object result = null;
            lookupTable.TryGetValue(text, out result);
            return result;
        }

        /// <summary>
        /// Return the Enum value for a flagged enum
        /// </summary>
        /// <param name="culture">The culture to convert using</param>
        /// <param name="text">The text to convert</param>
        /// <returns>The enum value</returns>
        private object GetFlagValue(CultureInfo culture, string text)
        {
            LookupTable lookupTable = GetLookupTable(culture);
            string[] textValues = text.Split(',');
            ulong result = 0;
            foreach (string textValue in textValues)
            {
                object value = null;
                string trimmedTextValue = textValue.Trim();
                if (!lookupTable.TryGetValue(trimmedTextValue, out value))
                {
                    return null;
                }
                result |= Convert.ToUInt32(value);
            }
            return Enum.ToObject(EnumType, result);
        }

        /// <summary>
        /// Create a new instance of the converter using translations from the given resource manager
        /// </summary>
        /// <param name="type"></param>
        /// <param name="resourceManager"></param>
        public ResourceEnumConverter(Type type, System.Resources.ResourceManager resourceManager)
            : base(type)
        {
            _resourceManager = resourceManager;
            object[] flagAttributes = type.GetCustomAttributes(typeof(FlagsAttribute), true);
            _isFlagEnum = flagAttributes.Length > 0;
            if (_isFlagEnum)
            {
                _flagValues = Enum.GetValues(type);
            }
        }

        /// <summary>
        /// Convert string values to enum values
        /// </summary>
        /// <param name="context"></param>
        /// <param name="culture"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is string)
            {
                object result = (_isFlagEnum) ?
                    GetFlagValue(culture, (string)value): GetValue(culture, (string)value);
                if (result == null)
                {
                    result = base.ConvertFrom(context, culture, value);
                }
                return result;
            }
            else
            {
                return base.ConvertFrom(context, culture, value);
            }
        }

        /// <summary>
        /// Convert the enum value to a string
        /// </summary>
        /// <param name="context"></param>
        /// <param name="culture"></param>
        /// <param name="value"></param>
        /// <param name="destinationType"></param>
        /// <returns></returns>
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (value != null && destinationType == typeof(string))
            {
                object result = (_isFlagEnum) ? 
                    GetFlagValueText(culture, value) : GetValueText(culture, value);
                return result;
            }
            else
            {
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        /// <summary>
        /// Convert the given enum value to string using the registered type converter
        /// </summary>
        /// <param name="value">The enum value to convert to string</param>
        /// <returns>The localized string value for the enum</returns>
        static public string ConvertToString(Enum value)
        {
            TypeConverter converter = TypeDescriptor.GetConverter(value.GetType());
            return converter.ConvertToString(value);
        }

        /// <summary>
        /// Return a list of the enum values and their associated display text for the given enum type
        /// </summary>
        /// <param name="enumType">The enum type to get the values for</param>
        /// <param name="culture">The culture to get the text for</param>
        /// <returns>
        /// A list of KeyValuePairs where the key is the enum value and the value is the text to display
        /// </returns>
        /// <remarks>
        /// This method can be used to provide localized binding to enums in ASP.NET applications.   Unlike 
        /// windows forms the standard ASP.NET controls do not use TypeConverters to convert from enum values
        /// to the displayed text.   You can bind an ASP.NET control to the list returned by this method by setting
        /// the DataValueField to "Key" and theDataTextField to "Value". 
        /// </remarks>
        static public List<KeyValuePair<Enum, string>> GetValues(Type enumType, CultureInfo culture)
        {
            List<KeyValuePair<Enum, string>> result = new List<KeyValuePair<Enum, string>>();
            TypeConverter converter = TypeDescriptor.GetConverter(enumType);
            foreach (Enum value in Enum.GetValues(enumType))
            {
                KeyValuePair<Enum, string> pair = new KeyValuePair<Enum, string>(value, converter.ConvertToString(null, culture, value));
                result.Add(pair); 
            }
            return result;
        }

        /// <summary>
        /// Return a list of the enum values and their associated display text for the given enum type in the current UI Culture
        /// </summary>
        /// <param name="enumType">The enum type to get the values for</param>
        /// <returns>
        /// A list of KeyValuePairs where the key is the enum value and the value is the text to display
        /// </returns>
        /// <remarks>
        /// This method can be used to provide localized binding to enums in ASP.NET applications.   Unlike 
        /// windows forms the standard ASP.NET controls do not use TypeConverters to convert from enum values
        /// to the displayed text.   You can bind an ASP.NET control to the list returned by this method by setting
        /// the DataValueField to "Key" and theDataTextField to "Value". 
        /// </remarks>
        static public List<KeyValuePair<Enum, string>> GetValues(Type enumType)
        {
            return GetValues(enumType, CultureInfo.CurrentUICulture);
        }
    }
}
