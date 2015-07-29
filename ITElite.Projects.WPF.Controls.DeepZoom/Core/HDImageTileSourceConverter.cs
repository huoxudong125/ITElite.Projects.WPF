using System;
using System.ComponentModel;
using System.Globalization;

namespace ITElite.Projects.WPF.Controls.DeepZoom.Core
{
    internal class HDImageTileSourceConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof (string))
                return true;
            return base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof (string))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var inputString = value as string;
            if (inputString != null)
            {
                try
                {
                    // This is the only important line of code in this file :)
                    return new HDImageTileSource(new Uri(inputString, UriKind.RelativeOrAbsolute));
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        string.Format("Cannot convert '{0}' ({1}) - {2}", value, value.GetType(), ex.Message), ex);
                }
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
            Type destinationType)
        {
            if (destinationType == null)
                throw new ArgumentNullException("destinationType");

            var tileSource = value as HDImageTileSource;

            if (tileSource != null)
                if (CanConvertTo(context, destinationType))
                {
                    var uri = tileSource.HdImagesSourrceUri;
                    return uri.IsAbsoluteUri ? uri.AbsoluteUri : uri.OriginalString;
                }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}