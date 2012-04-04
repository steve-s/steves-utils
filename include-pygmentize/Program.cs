
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace IncludePygmentize
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("include-pygmentize");
                Console.WriteLine("no file specified");
                Console.WriteLine("usage: include-pygmentize filename.tex");
                Console.WriteLine("note: use 'pygmentize -O full -l yourlexer -f latex' to get the preamble needed by pygments.");
                Console.WriteLine("You can save this preamble to a separate file pygments.tex and then just use \\include{pygments}.");
                return;
            }

            var writer = Console.Out;
            foreach (var filename in args)
            {
                try
                {
                    ProcessFile(filename, writer);
                }
                catch (IOException ex)
                {
                    Console.WriteLine(
                        "Cannot process file {0}. Exception of type {1} with message: {2}", 
                        filename, 
                        ex.GetType().Name,
                        ex.Message);
                }
            }
        }

        private static void ProcessFile(string filename, TextWriter writer)
        {
            var options = string.Empty;
            var reader = new StreamReader(File.OpenRead(filename));

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith("%pygmentize_options:"))
                {
                    options = line.Substring("%pygmentize_options:".Length);
                    writer.WriteLine(line);
                }
                else if (line.Trim().StartsWith("%pygmentize_begin"))
                {
                    ProcessPygmentize(line, reader, writer, options);
                }
                else
                {
                    writer.WriteLine(line);
                }
            }
        }

        private static void ProcessPygmentize(string line, TextReader reader, TextWriter writer, string options)
        {
            writer.WriteLine(line);
            int indentCount = 0;
            var indent = new StringBuilder();
            while (char.IsWhiteSpace(line[indentCount]))
            {
                indent.Append(line[indentCount]);
                indentCount++;
            }

            var lexer = line.Trim().Substring("%pygmentize_begin".Length).Trim();
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                                        {
                                            CreateNoWindow = true,
                                            UseShellExecute = false,
                                            Arguments = string.Format("{0} -l {1} -f tex", options, lexer),
                                            FileName = "pygmentize",
                                            RedirectStandardOutput = true,
                                            RedirectStandardInput = true
                                        };
                process.Start();
                process.StandardInput.AutoFlush = true;
                string nextLine;
                while ((nextLine = reader.ReadLine()) != null && 
                    nextLine.StartsWith("%pygmentize_end") == false)
                {
                    string inputLine = nextLine.Trim('%');
                    process.StandardInput.WriteLine(inputLine);
                    writer.WriteLine(nextLine);
                }

                if (nextLine == null)
                {
                    throw new FormatException(line, "Cannot find corresponding %pygmentize_end");
                }

                writer.WriteLine(nextLine);
                process.StandardInput.Close();
                            
                while (process.StandardOutput.EndOfStream == false)
                {
                    writer.WriteLine(indent + process.StandardOutput.ReadLine());
                }
            }
        }

        private class NullTextWriter : TextWriter
        {
            public override Encoding Encoding
            {
                get { return Encoding.ASCII; }
            }

            public override void Write(char value)
            {
            }
        }

        private class FormatException : ApplicationException
        {
            public FormatException(string line, string message) 
                : base(string.Format("Syntax error at line '{0}'. {1}", line, message))
            {
            }
        }
    }
}
