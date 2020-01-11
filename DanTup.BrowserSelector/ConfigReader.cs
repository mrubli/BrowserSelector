using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace DanTup.BrowserSelector
{

	public class ConfigSyntaxException
		: Exception
	{
		public ConfigSyntaxException() { }
		public ConfigSyntaxException(string message) : base(message) { }
		public ConfigSyntaxException(string message, Exception inner) : base(message, inner) { }
	}

	static class ConfigReader
	{
		/// <summary>
		/// Config lives in the same folder as the EXE, name "BrowserSelector.ini".
		/// </summary>
		public static string ConfigPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "BrowserSelector.ini");

		static internal IEnumerable<UrlPreference> GetUrlPreferences()
		{
			if (!File.Exists(ConfigPath))
				throw new InvalidOperationException(string.Format("The config file was not found:\r\n{0}\r\n", ConfigPath));

			// Poor mans INI file reading... Skip comment lines (TODO: support comments on other lines).
			var configLines =
				File.ReadAllLines(ConfigPath)
				.Select(l => l.Trim())
				.Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith(";") && !l.StartsWith("#"));

			// Read the browsers section into a dictionary.
			var browsers = GetConfig(configLines, "browsers")
				.Select(SplitConfig)
				.Select(kvp => new Browser { Name = kvp.Key, Location = kvp.Value })
				.ToDictionary(b => b.Name);

			// If there weren't any at all, force IE in there (nobody should create a config file like this!).
			if (!browsers.Any())
				browsers.Add("ie", new Browser { Name = "ie", Location = @"iexplore.exe ""{0}""" });

			// Read the url preferences, and add a catchall ("*") for the first browser.
			var urls = GetConfig(configLines, "urls")
				.Select(SplitConfig)
				.Select(kvp => MakeUrlPreference(kvp, browsers))
				.Union(new[] { new UrlPreference { UrlPattern = "*", Browser = browsers.FirstOrDefault().Value } }) // Add in a catchall that uses the first browser
				.Where(up => up.Browser != null);

			return urls;
		}

		static (RegexOptions options, string invalidFlags) MakeRegexOptions(string flags)
		{
			var options = RegexOptions.None;
			var invalidFlags = "";
			foreach(var c in flags)
			{
				switch(c)
				{
					case 'i':
						options |= RegexOptions.IgnoreCase;
						break;
					default:
						invalidFlags += c;
						break;
				}
			}
			return (options, invalidFlags);
		}

		static UrlPreference MakeUrlPreference(KeyValuePair<string, string> kvp, Dictionary<string, Browser> browsers)
		{
			// Split the value at the first colon. Note that Split() always returns at least one item.
			var parts = kvp.Value.Split(new char[] { ':' }, 2);

			// Check if there is a URL transform present.
			// Currently the only recognized URL transform is of the form "s|find|replace|flags".
			Func<string, string> transform = null;
			if (parts.Length > 1)
			{
				// Check for the find/replace URL transform.
				// Note: The below regex is only sufficient for the simplest of cases because it does not handle
				// escaping of special characters.
				var transformRegex = new Regex(@"^s(?<delim>\S)(?<find>[^|]*)\k<delim>(?<replace>[^|]*)\k<delim>(?<flags>[a-z]*)$", RegexOptions.Compiled);
				var match = transformRegex.Match(parts[1]);
				if(!match.Success)
				{
					throw new ConfigSyntaxException(
						$"Unknown URL transform:\r\n\t{parts[1]}\r\n\r\n" +
						$"URL preference in question:\r\n\t{kvp.Key}={kvp.Value}\r\n\r\n" +
						"Recognized URL transforms are:\r\n\ts|pattern|replacement| – String substitution"
					);
				}

				// Check the regex flags
				var (regexOptions, invalidFlags) = MakeRegexOptions(match.Groups["flags"].Value);
				if(invalidFlags.Length > 0)
				{
					throw new ConfigSyntaxException(
						$"Invalid URL transform replacement regex option(s):\r\n\t{invalidFlags}\r\n\r\n" +
						$"URL preference in question:\r\n\t{kvp.Key}={kvp.Value}\r\n\r\n" +
						"Recognized regex options are:\r\n\ti – Ignore case"
					);
				}

				// Compile the given regex and create a transformation function to do the work
				//MessageBox.Show($"find='{match.Groups["find"]}', replace='{match.Groups["replace"]}'");
				Regex userRegex;
				try
				{
					userRegex = new Regex(match.Groups["find"].Value, RegexOptions.Compiled | regexOptions);
				}
				catch(ArgumentException e)
				{
					throw new ConfigSyntaxException(
						$"Invalid URL transform replacement regex:\r\n\t{parts[1]}\r\n{e.Message}\r\n\r\n" +
						$"URL preference in question:\r\n\t{kvp.Key}={kvp.Value}",
						e
					);
				}
				var userReplacement = match.Groups["replace"].Value;
				transform = url => userRegex.Replace(url, userReplacement);
			}
			return new UrlPreference {
				UrlPattern = kvp.Key,
				Browser = browsers.ContainsKey(parts[0]) ? browsers[parts[0]] : null,
				Transform = transform
			};
		}

		static IEnumerable<string> GetConfig(IEnumerable<string> configLines, string configName)
		{
			// Read everything from [configName] up to the next [section].
			return configLines
				.SkipWhile(l => !l.StartsWith(string.Format("[{0}]", configName), StringComparison.OrdinalIgnoreCase))
				.Skip(1)
				.TakeWhile(l => !l.StartsWith("[", StringComparison.OrdinalIgnoreCase))
				.Where(l => l.Contains('='));
		}

		/// <summary>
		/// Splits a line on the first '=' (poor INI parsing).
		/// </summary>
		static KeyValuePair<string, string> SplitConfig(string configLine)
		{
			var parts = configLine.Split(new[] { '=' }, 2);
			return new KeyValuePair<string, string>(parts[0].Trim(), parts[1].Trim());
		}

		public static void CreateSampleIni()
		{
			Assembly assembly;
			Stream stream;
			StringBuilder result;

			assembly = Assembly.GetExecutingAssembly();
			//stream = assembly.GetManifestResourceStream("DanTup.BrowserSelector.BrowserSelector.ini");
			stream = assembly.GetManifestResourceStream(assembly.GetManifestResourceNames()[0]);
			if (stream == null)
			{
				return;
			}

			result = new StringBuilder();

			using (StreamReader reader = new StreamReader(stream))
			{
				result.Append(reader.ReadToEnd());
				reader.Close();
			}

			if (result.Length > 0)
			{
				if (File.Exists(ConfigPath))
				{
					string newName = GetBackupFileName(ConfigPath);
					File.Move(ConfigPath, newName);
				}

				File.WriteAllText(ConfigPath, result.ToString());
			}
		}

		static string GetBackupFileName(string fileName)
		{
			string newName;
			string fname;
			string fext;
			int index = 0;

			fname = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName);
			fext = Path.GetExtension(fileName);

			do
			{
				newName = string.Format("{0}.{1:0000}{2}", fname, ++index, fext);
			} while (File.Exists(newName));

			return newName;
		}
	}

	class Browser
	{
		public string Name { get; set; }
		public string Location { get; set; }
	}

	class UrlPreference
	{
		public string UrlPattern { get; set; }
		public Browser Browser { get; set; }
		public Func<string, string> Transform { get; set; }
	}
}
