using System.Windows.Media.Imaging;

public static class RGB
{
    // Variables para la camara RGB
    /// Bitmap that will hold color information
    public static WriteableBitmap colorBitmap;
    /// Intermediate storage for the color data received from the camera
    public static byte[] colorPixels;
}
