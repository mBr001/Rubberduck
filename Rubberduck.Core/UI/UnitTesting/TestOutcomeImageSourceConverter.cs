using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Rubberduck.UnitTesting;
using Rubberduck.Resources;

namespace Rubberduck.UI.UnitTesting
{
    public class TestOutcomeImageSourceConverter : IValueConverter
    {
        private static readonly IDictionary<TestOutcome,ImageSource> Icons = 
            new Dictionary<TestOutcome, ImageSource>
            {
                { TestOutcome.Unknown, ToImageSource(RubberduckUI.question_white) },
                { TestOutcome.Succeeded, ToImageSource(RubberduckUI.tick_circle) },
                { TestOutcome.Failed, ToImageSource(RubberduckUI.cross_circle) },
                { TestOutcome.Inconclusive, ToImageSource(RubberduckUI.exclamation) },
                { TestOutcome.Ignored, ToImageSource(RubberduckUI.minus_white) },
            };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value?.GetType() != typeof(TestOutcome))
            {
                return null;
            }

            var outcome = (TestOutcome)value;
            return Icons[outcome];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static ImageSource ToImageSource(Image source)
        {
            var ms = new MemoryStream();
            ((Bitmap)source).Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            var image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();

            return image;
        }
    }
}
