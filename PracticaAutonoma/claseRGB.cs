/*
 *  Código realizado por: Daniel Bazo Correa y Clara Rubio Almagro
 *  Fecha: 01/05/2023
 *  Asignatura de Sistemas electrónicos interactivos
 *  ETSIT Universidad de Málaga
 *  
 *  Clase de control de RGB
 */

// LIBRERIAS
using System.Windows.Media.Imaging;

public static class RGB
{
    // VARIABLES PARA CÁMARA RGB
    public static WriteableBitmap colorBitmap;      // Mapa de bit que contiene la información del color
    public static byte[] colorPixels;               // Almacenamiento del color recibido por la cámara
}
