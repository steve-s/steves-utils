
using System;
using System.Drawing.Imaging;
using System.IO;
using System.Drawing;
using System.Linq;

namespace Resize
{
	public static class Program
	{
		private static void Main(string[] args)
		{
			int percentage;
			if (args.Length < 1 || int.TryParse(args[0], out percentage) == false) {
				Console.WriteLine("Wrong arguments\nusage: resize [percentage]\nexample: resize 40");
				return;
			}

			string searchPattern = "*";
			if (args.Length > 1) {
				searchPattern = args[1];
			}

			Directory.CreateDirectory("backup");

			var encoder = Encoder.Quality;
			var jpegEncoder = ImageCodecInfo.GetImageDecoders().First(x => x.FormatID == ImageFormat.Jpeg.Guid);
			var @params = new EncoderParameters(1);
			@params.Param[0] = new EncoderParameter(encoder, 50L);

			double factor = ((double)percentage) / 100;
			foreach (var file in Directory.GetFiles(".", searchPattern)) {				
				var img = Image.FromFile(file);

				// Prevent using images internal thumbnail
				img.RotateFlip(RotateFlipType.Rotate180FlipNone);
				img.RotateFlip(RotateFlipType.Rotate180FlipNone);

				var newOne = img.GetThumbnailImage(
					(int)Math.Round(img.Width*factor), 
					(int)Math.Round(img.Height*factor), 
					null, 
					IntPtr.Zero);

				img.Dispose();
				File.Move(file, Path.Combine(Path.GetDirectoryName(file), "backup", Path.GetFileName(file)));
				newOne.Save(file, jpegEncoder, @params);
				newOne.Dispose();
				Console.WriteLine(file);
			}
		}
	}
}
