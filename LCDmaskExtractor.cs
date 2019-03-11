using System;
using System.IO;
//using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;

public class Extractor
{
		private static uint SPI_FILE_MAGIC_BASE = 318570496u; //0x12FD0000
		private static int SPECIAL_BIT = 1 << 1;
		private static int SPECIAL_BIT_MASK = ~SPECIAL_BIT;
		private byte[] fileDat = new byte[0];

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct spi_file_head_t
		{
			public uint magic_num;
			public uint file_size;
		}

		public object BytesToStruct(byte[] buf, int len, Type type)
		{
			IntPtr intPtr = Marshal.AllocHGlobal(len);
			Marshal.Copy(buf, 0, intPtr, len);
			object result = Marshal.PtrToStructure(intPtr, type);
			Marshal.FreeHGlobal(intPtr);
			return result;
		}

		public object BytesToStruct(byte[] buf, Type type)
		{
			return BytesToStruct(buf, buf.Length, type);
		}

		private bool GetDataPicture(int w, int h, byte[] data, string fileName)
		{
			//Bitmap bitmap = new Bitmap(w, h, PixelFormat.Format8bppIndexed);
			Bitmap bitmap = new Bitmap(w, h, PixelFormat.Format24bppRgb);
			
//			//build and fillup the grayscale-palette
//			ColorPalette palette = bitmap.Palette;
//			for (int i = 0; i < 256; i++)
//			{
//				palette.Entries[i] = Color.FromArgb(i, i, i);
//			}
//			bitmap.Palette = palette;
//
//			//map pixels to colors...
//			BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
//				ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
//			
//			IntPtr p = bitmapData.Scan0;
//			
//			for (int y = 0; y < bitmap.Height; y++)
//			{
//				for (int x = 0; x < bitmap.Width; x++)
//				{
//					System.Runtime.InteropServices.Marshal.WriteByte(p, y * bitmapData.Stride + x, data[y * bitmapData.Width + x]);
//				}
//			}
//			
//			bitmap.UnlockBits(bitmapData);

			//Mapped image is not editor friendly, move to 24bppRgb
			for (int y = 0; y < bitmap.Height; y++)
			{
				for (int x = 0; x < bitmap.Width; x++)
				{
					Color color = Color.FromArgb(data[y * bitmap.Width + x], data[y * bitmap.Width + x], data[y * bitmap.Width + x]);
					bitmap.SetPixel(x, y, color);
				}
			}
			
			//save bmp file
			try
			{
				bitmap.Save(fileName, ImageFormat.Bmp);
				bitmap.Dispose();
				return true;
			}
			catch (Exception ex)
			{
				System.Console.WriteLine(ex.ToString());
			}
			return false;
		}

		public int ExtractMask(string InFile)
		{
			StreamReader streamReader;
			try
			{
				streamReader = new StreamReader(InFile);
			}
			catch (Exception)
			{
				System.Console.WriteLine("Unable to open LCD mask file:" + InFile);
				return -1;
			}
			string directoryName = AppDomain.CurrentDomain.BaseDirectory; //Path.GetDirectoryName(InFile);
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(InFile);
			Path.GetExtension(InFile);
			string text2 = directoryName + fileNameWithoutExtension + ".bmp";
			FileInfo fileInfo = new FileInfo(InFile);
			fileDat = new byte[fileInfo.Length];
			streamReader.BaseStream.Read(fileDat, 0, fileDat.Length);
			streamReader.Close();
			spi_file_head_t spi_file_head_t = default(spi_file_head_t);
			byte[] array = new byte[Marshal.SizeOf((object)spi_file_head_t)];
			Buffer.BlockCopy(fileDat, 0, array, 0, Marshal.SizeOf((object)spi_file_head_t));
			spi_file_head_t = (spi_file_head_t)BytesToStruct(array, Marshal.SizeOf((object)spi_file_head_t), spi_file_head_t.GetType());
			if (spi_file_head_t.magic_num == (SPI_FILE_MAGIC_BASE | 0x1F))
			{
				byte[] array6 = new byte[spi_file_head_t.file_size];
				Buffer.BlockCopy(fileDat, Marshal.SizeOf((object)spi_file_head_t), array6, 0, (int)spi_file_head_t.file_size);
				if (!GetDataPicture(26, 43, array6, text2))
				{
					return -1;
				}
			}
			else
			{
				System.Console.WriteLine("Invalid LCD mask file!");
				return -1;
			}
		return 0;
		}
}

class MainClass
{
    static int Main(string[] args)
    {
        // Test if input arguments were supplied:
        if (args.Length < 1)
        {
            System.Console.WriteLine("Please enter binary LCD mask file.");
            System.Console.WriteLine("Usage: LCDmaskExtractor <input .lcdimg binary> ");
            return 1;
        }

        // Get the mask image.
        Extractor extractor = new Extractor();
        int result = extractor.ExtractMask(args[0]);
        if (result != 0)
        {
            System.Console.WriteLine("LCD mask file cannot be extracted!");
            return 1;
        }
        else
            System.Console.WriteLine("Extracted image from LCD mask file.");

        return 0;
    }
}

