using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GigeVisionLibrary.Test.Wpf
{
    public class LightImage2 : System.Windows.Controls.Image
    {
        // Using a DependencyProperty as the backing store for ImagePtr. This enables animation,
        // styling, binding, etc...
        public static readonly DependencyProperty ImagePtrProperty =
            DependencyProperty.Register("ImagePtr", typeof(IntPtr), typeof(LightImage2), new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for Height. This enables animation,
        // styling, binding, etc...
        public static readonly DependencyProperty HeightImageProperty =
            DependencyProperty.Register("HeightImage", typeof(int), typeof(LightImage2), new PropertyMetadata(0));

        // Using a DependencyProperty as the backing store for Width. This enables animation,
        // styling, binding, etc...
        public static readonly DependencyProperty WidthImageProperty =
            DependencyProperty.Register("WidthImage", typeof(int), typeof(LightImage2), new PropertyMetadata(0));

        private WriteableBitmap SourceImage;

        private Int32Rect rectBitmap;

        public IntPtr ImagePtr
        {
            get => (IntPtr)GetValue(ImagePtrProperty);
            set
            {
                SetValue(ImagePtrProperty, value);
                SourceImage.WritePixels(rectBitmap, ImagePtr, WidthImage * HeightImage, WidthImage);
                Source = SourceImage;
            }
        }

        public int HeightImage
        {
            get => (int)GetValue(HeightImageProperty);
            set
            {
                SetValue(HeightImageProperty, value);
                SetupImage();
            }
        }

        public int WidthImage
        {
            get => (int)GetValue(WidthImageProperty);
            set
            {
                SetValue(WidthImageProperty, value);
                SetupImage();
            }
        }

        private void SetupImage()
        {
            if (Width != 0 && HeightImage != 0)
            {
                rectBitmap = new Int32Rect(0, 0, WidthImage, HeightImage);
                List<Color> colors = new List<Color>
                                              {
                                                  Colors.Gray
                                              };
                BitmapPalette myPalette = new BitmapPalette(colors);
                SourceImage = new WriteableBitmap(WidthImage, HeightImage, 96, 96, PixelFormats.Gray8, myPalette);
            }
        }
    }
}