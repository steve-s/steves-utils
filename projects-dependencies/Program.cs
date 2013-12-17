using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ProjectsDependencies
{
	static class Program
	{
		private static IList<Project> projects = new List<Project>();

		public static void Main(string[] args)
		{
			if (args.Length == 0) {
				Console.WriteLine("Unexpected number of arguments");
				Console.WriteLine("Usage: proj-dep solution-file [-skip-tests]");
				Console.WriteLine("With -skip-tests all projects with a substring 'Test' in their name are skipped");
				return;
			}

			var solution = args.First();
			projects = GetProjects(solution);
			if (args.Length > 1 && string.CompareOrdinal(args[1], "-skip-tests") == 0) {
				projects = projects.Where(p => !p.Name.Contains("Test")).ToList();
			}

			Console.WriteLine("digraph {");
			foreach (var project in projects) {
				Console.WriteLine("{0} [];", project.Name);
			}

			var dir = Path.GetDirectoryName(solution);
			foreach (var project in projects) {
				var doc = XDocument.Load(Path.Combine(dir, project.File));
				var refs = doc.Root.XPathSelectElements("//*[local-name()='ProjectReference']");
				foreach (var reference in refs) {
					var refName = Path.GetFileNameWithoutExtension(reference.Attribute("Include").Value);
					Console.WriteLine("{0}->{1}[];", project.Name, refName.ProcessName());
				}
			}

			Console.WriteLine("}");
		}

		public static IList<Project> GetProjects(string solutionFile)
		{
			var result = new List<Project>();
			var text = File.ReadAllText(solutionFile);
			var matches = Regex.Matches(text, @"Project\([^,]*,(?<project>[^,]*)");
			foreach (var match in matches.Cast<Match>()) {
				var file = match.Result("${project}").Trim('\\', '"', ' ', '\t');
				if (file.EndsWith(@".csproj") == false) {
					continue;
				}

				result.Add(
					new Project
						{
							File = file,
							Name = Path.GetFileNameWithoutExtension(file).ProcessName()
						});
			}

			return result;
		}

		public static string ProcessName(this string name)
		{
			return name.Replace('.', '_').Replace(' ', '_').Replace("(", string.Empty).Replace(")", string.Empty);
		}

		public struct Project 
		{
			public string Name { get; set; }

			public string File { get; set; }
		}
	}
}
