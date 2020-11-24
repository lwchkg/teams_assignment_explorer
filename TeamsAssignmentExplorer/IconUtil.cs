using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace TeamsAssignmentExplorer
{
    class IconUtil
    {
        private static Dictionary<string, BitmapSource> iconStore;

        public static BitmapSource GetIconForExtension(string extension)
        {
            if (iconStore == null)
                iconStore = new Dictionary<string, BitmapSource>();

            if (!iconStore.ContainsKey(extension))
            {
                using (var ico = IconTools.GetIconForExtension(extension, ShellIconSize.SmallIcon))
                {
                    var image = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                        ico.Handle, System.Windows.Int32Rect.Empty,
                        BitmapSizeOptions.FromWidthAndHeight(16, 16));
                    iconStore[extension] = image;
                }
            }

            return iconStore[extension];
        }
    }
}
