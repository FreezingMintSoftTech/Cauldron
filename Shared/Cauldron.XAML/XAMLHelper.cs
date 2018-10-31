﻿using System;
using System.Collections;
using System.Linq;
using Cauldron.Collections;
using System.Collections.Generic;

#if WINDOWS_UWP

using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml;
using Windows.ApplicationModel;

#else

using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows;
using System.ComponentModel;

#endif

namespace Cauldron.XAML
{
    /// <summary>
    /// Provides helper methods for XAML
    /// </summary>
    public static class XAMLHelper
    {
        private static Dictionary<string, string> xmlNamespaces = new Dictionary<string, string>();

        /// <exclude/>
        static XAMLHelper()
        {
#if WINDOWS_UWP
#else
            xmlNamespaces.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
            xmlNamespaces.Add("cauldron", "clr-namespace:Cauldron.XAML;assembly=Cauldron.Win32.WPF");
#endif
        }

        /// <summary>
        /// Gets a collection of namespaces that is used by <see cref="XAMLHelper.ParseToInline(string)"/>.
        /// </summary>
        public static Dictionary<string, string> InlineNamespaces => xmlNamespaces;

        /// <summary>
        /// Determines whether the <see cref="DependencyObject"/> is in design mode or not.
        /// <para/>
        /// ATTENTION: This is only a wrapper for the DesignerProperties.GetIsInDesignMode(DependencyObject) method (WPF) and the DesignMode.DesignModeEnabled method (UWP).
        /// </summary>
        /// <param name="dependencyObject">The element from which the property value is read. In UWP this is ignored and can be null.</param>
        /// <exception cref="InvalidOperationException"><paramref name="dependencyObject"/> is null.</exception>
        /// <returns>true if the process is running in design mode; otherwise false.</returns>
        public static bool GetIsInDesignMode(DependencyObject dependencyObject)
        {
#if WINDOWS_UWP
            return DesignMode.DesignModeEnabled;
#else
            return DesignerProperties.GetIsInDesignMode(dependencyObject);

#endif
        }

        /// <summary>
        /// Returns the value of the property described by the path.
        /// </summary>
        /// <param name="source">The object where to start finding the property</param>
        /// <param name="path">The path of the property separated by period (.)</param>
        /// <returns>The value of the property</returns>
        public static object GetPropertyValueFromPath(object source, string path)
        {
            var pSource = XAMLHelper.GetSourceFromPath(source, path);
            var bindingPath = path.Split('.');
            var propertyName = bindingPath[bindingPath.Length - 1];

            var propertyInfo = pSource.GetType().GetPropertyEx(propertyName);

            if (propertyInfo == null)
                return null;

            return propertyInfo.GetValue(pSource);
        }

        /// <summary>
        /// Returns the source object of the property described by the path.
        /// </summary>
        /// <param name="source">The object where to start finding the property</param>
        /// <param name="path">The path of the property separated by period (.)</param>
        /// <returns>The value of the property</returns>
        public static object GetSourceFromPath(object source, string path)
        {
            var bindingPath = path.Split('.');

            // let us follow the path and change the source accordingly
            for (int i = 0; i < bindingPath.Length - 1; i++)
            {
                if (source == null)
                    break;

                var section = bindingPath[i];

                // is this an array?
                if (section.Right(1) == "]")
                {
                    // lets get the indexer between []
                    var indexer = section.EnclosedIn("[", "]").ToInteger();
                    var name = section.Left(section.IndexOf('['));
                    var propertyInfo = source.GetType().GetPropertyEx(name);

                    if (propertyInfo == null)
                    {
                        // the path is invalid...
                        source = null;
                        break;
                    }

                    var array = propertyInfo.GetValue(source) as IEnumerable;
                    source = array.Operations().ElementAt(indexer);
                }
                else
                {
                    var propertyInfo = source.GetType().GetPropertyEx(section);

                    if (propertyInfo == null)
                    {
                        // the path is invalid...
                        source = null;
                        break;
                    }

                    source = propertyInfo.GetValue(source);
                }
            }

            return source;
        }

        /// <summary>
        /// Reads the XAML input in the specified text string and returns a <see cref="Span"/> wrapping the an object that
        /// corresponds to the root of the specified markup.
        /// <para/>
        /// Be careful using this feature in UWP release. Behaviours, action and converters does not work most of the time.
        /// </summary>
        /// <param name="xamlText">The input XAML, as a single text string.</param>
        /// <returns>The <see cref="Span"/> root of the created object tree.</returns>
        public static Span ParseToInline(string xamlText)
        {
            if (xamlText == null)
                throw new ArgumentNullException(nameof(xamlText));

#if WINDOWS_UWP
            var xmlns = new string[] /* Will not work in UWP Release */
              {
                        @"xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""",
                        @"xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""",
                        @"xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""",
                        @"xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""",
                        @"xmlns:controls=""using:Cauldron.XAML.Controls""",
              }.Join(" ");
#else
            var xmlns = xmlNamespaces
                .Select(x => (string.IsNullOrEmpty(x.Key) ? "xmlns" : $"xmlns:{x.Key}") + "=\"" + x.Value + "\"")
                .Join(" ");
#endif

            var xaml = xamlText.Replace("<Inline>\r\n", $"<Span {xmlns}>")
                      .Replace("\r\n</Inline>", "</Span>")
                      .Replace("<Inline>", $"<Span {xmlns}>")
                      .Replace("</Inline>", "</Span>")
                      .Replace("''", "\"")
                      .Replace("\r\n", "<LineBreak/>")
                      .Replace("<br>", "<LineBreak/>")
                      .Replace(@"\r\n", "<LineBreak/>")
                      .Replace(@"\n\n", "<LineBreak/><LineBreak/>");
            try
            {
#if WINDOWS_UWP
                return XamlReader.Load(xaml) as Span;
#else

                return XamlReader.Parse(xaml) as Span;
#endif
            }
            catch (Exception e)
            {
                var span = new Span();
                span.Inlines.Add(new Run { Text = e.Message });
                return span;
            }
        }
    }
}