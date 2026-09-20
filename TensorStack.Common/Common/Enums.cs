// Copyright (c) Adam Clark. All rights reserved.
// Licensed under the Apache 2.0 License.

using System;
using System.ComponentModel.DataAnnotations;

namespace TensorStack.Common
{
    /// <summary>
    /// Enum Optimization
    /// </summary>
    public enum Optimization
    {
        /// <summary>
        /// No Optimizations
        /// </summary>
        None = 0,

        /// <summary>
        /// Basic Optimizations
        /// </summary>
        Basic = 1,

        /// <summary>
        /// Extended Optimizations
        /// </summary>
        Extended = 2,

        /// <summary>
        /// All Optimizations
        /// </summary>
        All = 99
    }


    /// <summary>
    /// Normalization
    /// </summary>
    public enum Normalization
    {
        None = 0,
        ZeroToOne = 1,
        OneToOne = 2,
        MinMaxZeroToOne = 3,
        MinMaxOneToOne = 4
    }


    /// <summary>
    /// ResizeMode
    /// </summary>
    public enum ResizeMode
    {
        /// <summary>
        /// Strech Image
        /// </summary>
        Stretch = 0,

        /// <summary>
        /// Center Crop Image
        /// </summary>
        Crop = 1,

        /// <summary>
        /// LetterBox Center Image
        /// </summary>
        LetterBox = 2
    }


    /// <summary>
    /// Enum ResizeMethod
    /// </summary>
    public enum ResizeMethod
    {
        Bilinear = 0,
        Bicubic = 1
    }


    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum enumObj)
        {
            return enumObj.GetDisplayAttribute()?.GetName() ?? enumObj.ToString();
        }


        public static string GetShortName(this Enum enumObj)
        {
            return enumObj.GetDisplayAttribute()?.GetShortName() ?? enumObj.ToString();
        }


        public static string GetDescription(this Enum enumObj)
        {
            return enumObj.GetDisplayAttribute()?.GetDescription() ?? string.Empty;
        }


        private static DisplayAttribute GetDisplayAttribute(this Enum enumObj)
        {
            var fieldInfo = enumObj.GetType().GetField(enumObj.ToString());
            var attribArray = fieldInfo.GetCustomAttributes(false);
            if (attribArray.Length > 0)
            {
                foreach (var att in attribArray)
                {
                    if (att is DisplayAttribute display)
                        return display;
                }
            }
            return null;
        }
    }
}
