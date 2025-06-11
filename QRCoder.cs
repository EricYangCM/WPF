*** Nuget에서 QRCoder 추가하기 ***

using QRCoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NFS_LightingCtrlSystem_v1
{
    public class QRCoder
    {

        public static BitmapImage GenerateQRCode(string content, int pixelsPerModule = 20)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q))
            using (QRCode qrCode = new QRCode(qrCodeData))
            using (Bitmap qrBitmap = qrCode.GetGraphic(pixelsPerModule))
            {
                return ConvertBitmapToBitmapImage(qrBitmap);
            }
        }

        private static BitmapImage ConvertBitmapToBitmapImage(Bitmap bitmap)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Png);
                ms.Position = 0;

                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                image.Freeze(); // for cross-thread access
                return image;
            }
        }
    }
}
