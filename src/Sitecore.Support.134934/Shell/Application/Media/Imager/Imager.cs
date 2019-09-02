namespace Sitecore.Support.Shell.Applications.Media.Imager
{
    using Sitecore;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.IO;
    using Sitecore.Resources.Media;
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;

    internal class Imager
    {
        /// <summary>
        /// Crops the specified file.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="work">The work.</param>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <returns></returns>
        public static string Crop(string file, string work, int x, int y, int width, int height)
        {
            if (x < 0)
            {
                width += x;
                x = 0;
            }
            if (y < 0)
            {
                height += y;
                y = 0;
            }
            if (file.Length > 0)
            {
                using (Bitmap bitmap2 = new Bitmap(MainUtil.MapPath(work)))
                {
                    using (Bitmap bitmap = new Bitmap(width, height))
                    {
                        Rectangle srcRect = new Rectangle(x, y, width, height);
                        bitmap.SetResolution(bitmap2.HorizontalResolution, bitmap2.VerticalResolution);
                        using (Graphics graphics = Graphics.FromImage(bitmap))
                        {
                            graphics.DrawImage(bitmap2, new Rectangle(0, 0, bitmap.Width, bitmap.Height), srcRect, GraphicsUnit.Pixel);
                        }
                        string temporaryFile = FileUtilSupport.GetTemporaryFile(file);
                        SaveBitmap(bitmap, temporaryFile, bitmap2.RawFormat);
                        return MainUtil.UnmapPath(temporaryFile);
                    }
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Mirrors the specified file.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="work">The work.</param>
        /// <param name="direction">The direction.</param>
        /// <returns></returns>
        public static string Mirror(string file, string work, string direction)
        {
            if (file.Length > 0)
            {
                using (Bitmap bitmap = new Bitmap(MainUtil.MapPath(work)))
                {
                    ImageFormat rawFormat = bitmap.RawFormat;
                    if (string.Compare(direction, "horizontal", StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    }
                    else
                    {
                        bitmap.RotateFlip(RotateFlipType.Rotate180FlipX);
                    }
                    string temporaryFile = FileUtilSupport.GetTemporaryFile(file);
                    SaveBitmap(bitmap, temporaryFile, rawFormat);
                    return MainUtil.UnmapPath(temporaryFile);
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Resizes the specified file.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="work">The work.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <returns></returns>
        public static string Resize(string file, string work, int width, int height)
        {
            if (file.Length > 0 && width > 0 && height > 0)
            {
                using (Bitmap bitmap = new Bitmap(MainUtil.MapPath(work)))
                {
                    using (Bitmap bitmap2 = new Bitmap(bitmap, width, height))
                    {
                        Rectangle rect = new Rectangle(0, 0, width, height);
                        using (Graphics graphics = Graphics.FromImage(bitmap2))
                        {
                            graphics.DrawImage(bitmap, rect);
                        }
                        string temporaryFile = FileUtilSupport.GetTemporaryFile(file);
                        bitmap2.SetResolution(bitmap.HorizontalResolution, bitmap.VerticalResolution);
                        SaveBitmap(bitmap2, temporaryFile, bitmap.RawFormat);
                        return MainUtil.UnmapPath(temporaryFile);
                    }
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Rotates the specified file.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="work">The work.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <returns></returns>
        public static string Rotate(string file, string work, string direction, out int width, out int height)
        {
            if (file.Length > 0)
            {
                using (Bitmap bitmap = new Bitmap(MainUtil.MapPath(work)))
                {
                    ImageFormat rawFormat = bitmap.RawFormat;
                    if (string.Compare(direction, "left", StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    }
                    else
                    {
                        bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    }
                    width = bitmap.Width;
                    height = bitmap.Height;
                    string temporaryFile = FileUtilSupport.GetTemporaryFile(file);
                    SaveBitmap(bitmap, temporaryFile, rawFormat);
                    return MainUtil.UnmapPath(temporaryFile);
                }
            }
            width = 0;
            height = 0;
            return string.Empty;
        }

        /// <summary>
        /// Creates the temporary file.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The temporary file.</returns>
        internal static string CreateTemporaryFile(MediaItem item)
        {
            string extension = item.Extension;
            string temporaryFile = FileUtilSupport.GetTemporaryFile("imager." + extension);
            ImageMedia imageMedia = MediaManager.GetMedia(MediaUri.Parse(item)) as ImageMedia;
            if (imageMedia == null)
            {
                return string.Empty;
            }
            using (Image image = imageMedia.GetImage())
            {
                if (image == null)
                {
                    return string.Empty;
                }
                int @int = MainUtil.GetInt(item.InnerItem["Width"], 0);
                int int2 = MainUtil.GetInt(item.InnerItem["Height"], 0);
                Bitmap bitmap = (@int <= 0 || int2 <= 0) ? new Bitmap(image) : new Bitmap(image, @int, int2);
                ImageFormat imageFormat = MediaManager.Config.GetImageFormat(extension);
                Assert.IsNotNull(imageFormat, typeof(ImageFormat), "Extension: '{0}'.", extension);
                SaveBitmap(bitmap, temporaryFile, imageFormat);
                return MainUtil.UnmapPath(temporaryFile);
            }
        }

        /// <summary>
        /// Creates the temporary file.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>The temporary file.</returns>
        internal static string CreateTemporaryFile(string file)
        {
            string extension = FileUtil.GetExtension(file);
            string temporaryFile = FileUtilSupport.GetTemporaryFile(extension);
            using (Image image = Image.FromFile(FileUtil.MapPath(file)))
            {
                if (image != null)
                {
                    Bitmap bitmap = new Bitmap(image);
                    ImageFormat imageFormat = MediaManager.Config.GetImageFormat(extension);
                    Assert.IsNotNull(imageFormat, typeof(ImageFormat), "Extension: '{0}'.", extension);
                    SaveBitmap(bitmap, temporaryFile, imageFormat);
                    return temporaryFile;
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Saves the bitmap.
        /// </summary>
        /// <param name="bitmap">The bitmap.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="imageFormat">The image format.</param>
        protected static void SaveBitmap(Bitmap bitmap, string filename, ImageFormat imageFormat)
        {
            try
            {
                bitmap.Save(MainUtil.MapPath(filename), imageFormat);
            }
            catch (Exception exception)
            {
                Log.Error("Could not save bitmap file: " + filename, exception, typeof(Imager));
            }
        }
    }
}