using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GigeVisionLibrary.Test.Wpf
{
    public class LightImage : FrameworkElement
    {
        // Using a DependencyProperty as the backing store for ImagePtr. This enables animation,
        // styling, binding, etc...
        public static readonly DependencyProperty ImagePtrProperty =
            DependencyProperty.Register("ImagePtr", typeof(IntPtr), typeof(LightImage), new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for Height. This enables animation,
        // styling, binding, etc...
        public static readonly DependencyProperty HeightImageProperty =
            DependencyProperty.Register("Height", typeof(int), typeof(LightImage), new PropertyMetadata(0));

        // Using a DependencyProperty as the backing store for Width. This enables animation,
        // styling, binding, etc...
        public static readonly DependencyProperty WidthImageProperty =
            DependencyProperty.Register("Width", typeof(int), typeof(LightImage), new PropertyMetadata(0));

        // Using a DependencyProperty as the backing store for Stretch. This enables animation,
        // styling, binding, etc...
        public static readonly DependencyProperty StretchProperty =
            DependencyProperty.Register("Stretch", typeof(Stretch), typeof(LightImage), new PropertyMetadata(Stretch.None));

        private WriteableBitmap Source;

        private Int32Rect rectBitmap;

        private Rect rectImage;

        public Stretch Stretch
        {
            get { return (Stretch)GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        public IntPtr ImagePtr
        {
            get { return (IntPtr)GetValue(ImagePtrProperty); }
            set
            {
                SetValue(ImagePtrProperty, value);
                Render();
            }
        }

        public int HeightImage
        {
            get { return (int)GetValue(HeightImageProperty); }
            set
            {
                SetValue(HeightImageProperty, value);
                SetupImage();
            }
        }

        public int WidthImage
        {
            get { return (int)GetValue(WidthImageProperty); }
            set
            {
                SetValue(WidthImageProperty, value);
                SetupImage();
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (ImagePtr != (IntPtr)0)
            {
                switch (Stretch)
                {
                    case Stretch.None:
                    case Stretch.Fill:
                        Source.WritePixels(rectBitmap, ImagePtr, WidthImage * HeightImage, WidthImage);
                        break;

                    case Stretch.Uniform:
                        break;

                    case Stretch.UniformToFill:
                        break;
                }
                drawingContext.DrawImage(Source, rectImage);
            }
        }

        private void SetupImage()
        {
            if (WidthImage != 0 && HeightImage != 0)
            {
                rectBitmap = new Int32Rect(0, 0, WidthImage, HeightImage);
                rectImage = new Rect(0, 0, WidthImage, HeightImage);
                List<System.Windows.Media.Color> colors = new List<System.Windows.Media.Color>
                                                          {
                                                              Colors.Gray
                                                          };
                BitmapPalette myPalette = new BitmapPalette(colors);
                Source = new WriteableBitmap(WidthImage, HeightImage, 96, 96, PixelFormats.Gray8, myPalette);
            }
        }

        private void Render()
        {
            InvalidateVisual();
        }
    }
}