using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public class Encoder
{
		private static uint SPI_FILE_MAGIC_BASE = 318570496u; //0x12FD0000

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct spi_file_head_t
		{
			public uint magic_num;
			public uint file_size;
		}

		public byte[] StructToBytes(object obj)
		{
			int num = Marshal.SizeOf(obj);
			IntPtr intPtr = Marshal.AllocHGlobal(num);
			Marshal.StructureToPtr(obj, intPtr, false);
			byte[] array = new byte[num];
			Marshal.Copy(intPtr, array, 0, num);
			Marshal.FreeHGlobal(intPtr);
			return array;
		}
		

//		private static Bitmap ResizeImage(Bitmap bmp, int newW, int newH)
//		{
//			try
//			{
//				Bitmap bitmap = new Bitmap(newW, newH, PixelFormat.Format24bppRgb);
//				Graphics graphics = Graphics.FromImage(bitmap);
//				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
//				graphics.DrawImage(bmp, new Rectangle(0, 0, newW, newH), new Rectangle(0, 0, bmp.Width, bmp.Height), GraphicsUnit.Pixel);
//				graphics.Dispose();
//				return bitmap;
//			}
//			catch (Exception ex)
//			{
//				System.Console.WriteLine(ex.ToString());
//				return null;
//			}
//		}

		private int generatePicDat(int width, int height, string fileName, byte[] dat)
		{
			if (fileName.Length > 0)
			{
				try
				{
					int num = 0;
					Bitmap bmp = new Bitmap(fileName);

// Resize not allowed for mask file
//					bmp = ResizeImage(bmp, width, height);
//					if (bmp == null)
//					{
//						System.Console.WriteLine("Unable to scale file:" + fileName);
//						return -1;
//					}

					if ((bmp.Width != width) || (bmp.Height != height))
					{
						System.Console.WriteLine("Invalid input file resolution:" + bmp.Width + "x" + bmp.Height);
						return -1;
					}

//					//map pixels to output array...
//					BitmapData bitmapData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
//						ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
//					
//					IntPtr p = bitmapData.Scan0;
//					
//					for (int y = 0; y < bmp.Height; y++)
//					{
//						for (int x = 0; x < bmp.Width; x++)
//						{
//							dat[y * bitmapData.Width + x] = System.Runtime.InteropServices.Marshal.ReadByte(p, y * bitmapData.Stride + x);
//							//Check and trim white range
//							if (dat[y * bitmapData.Width + x] > 0xFD) dat[y * bitmapData.Width + x] = 0xFD;
//							num++;
//						}
//					}
//					
//					bmp.UnlockBits(bitmapData);

					//Mapped image is not editor friendly, move to 24bppRgb
					for (int y = 0; y < bmp.Height; y++)
					{
						for (int x = 0; x < bmp.Width; x++)
						{
							Color pixel = bmp.GetPixel(x, y);
							dat[y * bmp.Width + x] = (byte)((pixel.B + pixel.G + pixel.R)/3);
							//Check and trim white range
							if (dat[y * bmp.Width + x] > 0xFD) dat[y * bmp.Width + x] = 0xFD;
							num++;
						}
					}

					return num;

				}
				catch (Exception ex)
				{
					System.Console.WriteLine("Unable to open image file:" + fileName + ex.ToString());
					return -1;
				}
			}
			System.Console.WriteLine("Invalid file name:" + fileName);
			return -1;
		}

		public int GenerateMask(string SourceFile)
		{
			int num = 26; //Photon LCD mask width
			int num2 = 43; //Photon LCD mask height
		
			byte[] array = new byte[num * num2 * 2];
			spi_file_head_t spi_file_head_t = default(spi_file_head_t);
			spi_file_head_t.magic_num = (SPI_FILE_MAGIC_BASE | 0x1F);
			int num3 = generatePicDat(num, num2, SourceFile, array);
			if (num3 > 0)
			{
				spi_file_head_t.file_size = (uint)num3;
				string directoryName = AppDomain.CurrentDomain.BaseDirectory; //Path.GetDirectoryName(SourceFile);
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(SourceFile);
				Path.GetExtension(SourceFile);
				string text = directoryName + "\\" + fileNameWithoutExtension + ".lcdimg";
				StreamWriter streamWriter;
				try
				{
					streamWriter = new StreamWriter(text);
				}
				catch (Exception)
				{
					System.Console.WriteLine("Unable to generate file:" + text);
					return -1;
				}
				byte[] array2 = StructToBytes(spi_file_head_t);
				streamWriter.BaseStream.Write(array2, 0, array2.Length);
				streamWriter.BaseStream.Write(array, 0, num3);
				streamWriter.Close();
				return 0;
			}
			else
				return -1;
		}
}

class MainClass
{
    static int Main(string[] args)
    {
        // Test if input arguments were supplied:
        if (args.Length < 1)
        {
            System.Console.WriteLine("Please enter input file name.");
            System.Console.WriteLine("Usage: LCDmaskGenerator <input bmp file>");
            return 1;
        }

        // Get the images.
        Encoder encoder = new Encoder();
        int result = encoder.GenerateMask(args[0]);
        
        if (result < 0)
        {
            System.Console.WriteLine("LCD mask generation failed!");
            return 1;
        }
        else
        {
            System.Console.WriteLine("LCD mask file generated sucessfully");
            return 0;
        }
    }
}

