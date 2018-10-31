﻿#if NETFX_CORE

using Windows.UI.Xaml;

#else

using System.Windows;

#endif

namespace Cauldron.XAML
{
    /// <summary>
    /// Holds information about the DependencyProperty
    /// </summary>
    public sealed class DependencyPropertyInfo
    {
        /*
         * UWP does not have a Name property in DependencyProperties... Which can come handy sometimes...
         * That is why this class exist as a wrapper...
         */

        internal DependencyPropertyInfo(DependencyProperty dependencyProperty, string name)
        {
            this.DependencyProperty = dependencyProperty;

#if NETFX_CORE

            if (name.EndsWith("Property"))
                this.Name = name.Substring(0, name.Length - "Property".Length);
            else
                this.Name = name;

#else
            this.Name = dependencyProperty.Name;
#endif
        }

        /// <summary>
        /// Gets the inclosed <see cref="DependencyProperty"/>.
        /// </summary>
        public DependencyProperty DependencyProperty { get; private set; }

        /// <summary>
        /// Gets the name of the <see cref="DependencyProperty"/>.
        /// </summary>
        public string Name { get; private set; }

        /// <exlude/>
        public static implicit operator DependencyProperty(DependencyPropertyInfo dependencyPropertyInfo) => dependencyPropertyInfo.DependencyProperty;
    }
}