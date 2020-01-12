using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DanTup.BrowserSelector;
using System.Collections.Generic;

namespace UnitTest
{
	[TestClass]
	public class TestConfigReader
	{
		static Dictionary<string, Browser> Browsers = new Dictionary<string, Browser> {
			{ "vivaldi", new Browser() { Name = "vivaldi", Location = "vivaldi.exe" } },
			{ "firefox", new Browser() { Name = "firefox", Location = "firefox.exe" } },
		};

		static UrlPreference MakeUrlPreference(KeyValuePair<string, string> kvp, Dictionary<string, Browser> browsers)
		{
			PrivateType configReaderType = new PrivateType(typeof(ConfigReader));
			return (UrlPreference)configReaderType.InvokeStatic("MakeUrlPreference", kvp, Browsers);
		}

		static KeyValuePair<string, string> SplitConfig(string configLine)
		{
			PrivateType configReaderType = new PrivateType(typeof(ConfigReader));
			return (KeyValuePair<string, string>)configReaderType.InvokeStatic("SplitConfig", configLine);
		}

		[TestMethod]
		public void SplitConfig_Host()
		{
			Assert.AreEqual(
				new KeyValuePair<string, string>("example.com", "vivaldi"),
				SplitConfig("example.com=vivaldi")
			);
			Assert.AreEqual(
				new KeyValuePair<string, string>("example.com", "vivaldi"),
				SplitConfig("example.com= vivaldi")
			);
			Assert.AreEqual(
				new KeyValuePair<string, string>("example.com", "vivaldi"),
				SplitConfig("example.com =vivaldi")
			);
			Assert.AreEqual(
				new KeyValuePair<string, string>("example.com", "vivaldi"),
				SplitConfig("example.com = vivaldi")
			);
			Assert.AreEqual(
				new KeyValuePair<string, string>("example.com", "vivaldi"),
				SplitConfig("example.com  =   vivaldi")
			);
			Assert.AreEqual(
				new KeyValuePair<string, string>("example.com", "vivaldi"),
				SplitConfig("example.com\t=\t\tvivaldi")
			);

			Assert.AreEqual(
				new KeyValuePair<string, string>("www.example.com", "vivaldi"),
				SplitConfig("www.example.com=vivaldi")
			);

			Assert.AreEqual(
				new KeyValuePair<string, string>("localhost", "vivaldi"),
				SplitConfig("localhost=vivaldi")
			);

			Assert.AreEqual(
				new KeyValuePair<string, string>("127.0.0.1", "vivaldi"),
				SplitConfig("127.0.0.1=vivaldi")
			);
		}

		[TestMethod]
		public void SplitConfig_Wildcard()
		{
			Assert.AreEqual(
				new KeyValuePair<string, string>("*.example.com", "vivaldi"),
				SplitConfig("*.example.com=vivaldi")
			);
			Assert.AreEqual(
				new KeyValuePair<string, string>("example.*", "vivaldi"),
				SplitConfig("example.*=vivaldi")
			);
			Assert.AreEqual(
				new KeyValuePair<string, string>("*.example.*", "vivaldi"),
				SplitConfig("*.example.*=vivaldi")
			);
		}

		[TestMethod]
		public void SplitConfig_Regex()
		{
			Assert.AreEqual(
				new KeyValuePair<string, string>(@"/example\.com/", "vivaldi"),
				SplitConfig(@"/example\.com/=vivaldi")
			);
			Assert.AreEqual(
				new KeyValuePair<string, string>(@"/example\.(com|net)/app/", "vivaldi"),
				SplitConfig(@"/example\.(com|net)/app/=vivaldi")
			);
			Assert.AreEqual(
				new KeyValuePair<string, string>(@"/example\.(com|net)/app\?foo/", "vivaldi"),
				SplitConfig(@"/example\.(com|net)/app\?foo/=vivaldi")
			);
		}

		[TestMethod]
		public void SplitConfig_HostPlusTransform()
		{
			Assert.AreEqual(
				new KeyValuePair<string, string>(@"example.com:s|foo|bar|", "vivaldi"),
				SplitConfig(@"example.com:s|foo|bar|=vivaldi")
			);
			Assert.AreEqual(
				new KeyValuePair<string, string>(@"example.com:s|^foo$|bar|", "vivaldi"),
				SplitConfig(@"example.com:s|^foo$|bar|=vivaldi")
			);
			Assert.AreEqual(
				new KeyValuePair<string, string>(@"example.com:s|^foo.com/(.*)|www.foo.com/$1|", "vivaldi"),
				SplitConfig(@"example.com:s|^foo.com/(.*)|www.foo.com/$1|=vivaldi")
			);
		}

		// These are tests that should succeed but don't currently
		public void SplitConfig_Failure()
		{
			// The parsing function chokes on the equals in "foo=bar" and mistakenly splits the line there.
			Assert.AreNotEqual(
				new KeyValuePair<string, string>(@"/example\.(com|net)/app\?foo=bar/", "vivaldi"),
				SplitConfig(@"/example\.(com|net)/app\?foo=bar/=vivaldi")
			);
		}

		[TestMethod]
		public void MakeUrlPreference_Host()
		{
			var kvp = new KeyValuePair<string, string>("example.com", "vivaldi");
			UrlPreference up = MakeUrlPreference(kvp, Browsers);
			Assert.AreEqual("vivaldi", up.Browser.Name);
			Assert.AreEqual("vivaldi.exe", up.Browser.Location);
			Assert.AreEqual("example.com", up.UrlPattern);
			Assert.IsNull(up.Transform);
		}

		[TestMethod]
		public void MakeUrlPreference_Wildcard()
		{
			var kvp = new KeyValuePair<string, string>("*.example.com", "firefox");
			UrlPreference up = MakeUrlPreference(kvp, Browsers);
			Assert.AreEqual("firefox", up.Browser.Name);
			Assert.AreEqual("firefox.exe", up.Browser.Location);
			Assert.AreEqual("*.example.com", up.UrlPattern);
			Assert.IsNull(up.Transform);
		}

		[TestMethod]
		public void MakeUrlPreference_Regex()
		{
			var kvp = new KeyValuePair<string, string>(@"/example\.(com|net)/app/", "vivaldi");
			UrlPreference up = MakeUrlPreference(kvp, Browsers);
			Assert.AreEqual("vivaldi", up.Browser.Name);
			Assert.AreEqual("vivaldi.exe", up.Browser.Location);
			Assert.AreEqual(@"/example\.(com|net)/app/", up.UrlPattern);
			Assert.IsNull(up.Transform);
		}

		[TestMethod]
		public void MakeUrlPreference_HostPlusTransform()
		{
			// Basic
			{
				var kvp = new KeyValuePair<string, string>(@"example.com", "vivaldi:s|foo|bar|");
				UrlPreference up = MakeUrlPreference(kvp, Browsers);
				Assert.AreEqual("vivaldi", up.Browser.Name);
				Assert.AreEqual("vivaldi.exe", up.Browser.Location);
				Assert.AreEqual("example.com", up.UrlPattern);
				Assert.IsNotNull(up.Transform);
				Assert.AreEqual("bar", up.Transform("foo"));
				Assert.AreEqual("Foo", up.Transform("Foo"));	// Case-sensitive
			}

			// Case-insensitive flag
			{
				var kvp = new KeyValuePair<string, string>(@"example.com", "vivaldi:s|foo|bar|i");
				UrlPreference up = MakeUrlPreference(kvp, Browsers);
				Assert.AreEqual("vivaldi", up.Browser.Name);
				Assert.AreEqual("vivaldi.exe", up.Browser.Location);
				Assert.AreEqual("example.com", up.UrlPattern);
				Assert.IsNotNull(up.Transform);
				Assert.AreEqual("bar", up.Transform("foo"));
				Assert.AreEqual("bar", up.Transform("Foo"));	// Case-insensitive
			}

			// Basic with different delimiters
			{
				var kvp = new KeyValuePair<string, string>(@"example.com", "vivaldi:s/foo/bar/");
				UrlPreference up = MakeUrlPreference(kvp, Browsers);
				Assert.AreEqual("vivaldi", up.Browser.Name);
				Assert.AreEqual("vivaldi.exe", up.Browser.Location);
				Assert.AreEqual("example.com", up.UrlPattern);
				Assert.IsNotNull(up.Transform);
				Assert.AreEqual("bar", up.Transform("foo"));
				Assert.AreEqual("Foo", up.Transform("Foo"));
			}
			{
				var kvp = new KeyValuePair<string, string>(@"example.com", "vivaldi:sXfooXbarX");
				UrlPreference up = MakeUrlPreference(kvp, Browsers);
				Assert.AreEqual("vivaldi", up.Browser.Name);
				Assert.AreEqual("vivaldi.exe", up.Browser.Location);
				Assert.AreEqual("example.com", up.UrlPattern);
				Assert.IsNotNull(up.Transform);
				Assert.AreEqual("bar", up.Transform("foo"));
				Assert.AreEqual("Foo", up.Transform("Foo"));
			}
			{
				var kvp = new KeyValuePair<string, string>(@"example.com", "vivaldi:s⦁foo⦁bar⦁");
				UrlPreference up = MakeUrlPreference(kvp, Browsers);
				Assert.AreEqual("vivaldi", up.Browser.Name);
				Assert.AreEqual("vivaldi.exe", up.Browser.Location);
				Assert.AreEqual("example.com", up.UrlPattern);
				Assert.IsNotNull(up.Transform);
				Assert.AreEqual("bar", up.Transform("foo"));
				Assert.AreEqual("Foo", up.Transform("Foo"));
			}

			// Example: Host redirection
			{
				var kvp = new KeyValuePair<string, string>(
					@"evil.com",
					@"vivaldi:s|://evil\.com/view\?id=([^&#]*)|://good.com/get/$1|"
				);
				UrlPreference up = MakeUrlPreference(kvp, Browsers);
				Assert.AreEqual("vivaldi", up.Browser.Name);
				Assert.AreEqual("vivaldi.exe", up.Browser.Location);
				Assert.AreEqual("evil.com", up.UrlPattern);
				Assert.IsNotNull(up.Transform);
				Assert.AreEqual(@"http://good.com/get/abc123", up.Transform(@"http://evil.com/view?id=abc123"));
				Assert.AreEqual(@"https://good.com/get/abc123", up.Transform(@"https://evil.com/view?id=abc123"));
			}
			// Same as above but with named capturing groups
			{
				var kvp = new KeyValuePair<string, string>(
					@"evil.com",
					@"vivaldi:s|://evil\.com/view\?id=(?<id>[^&#]*)|://good.com/get/${id}|"
				);
				UrlPreference up = MakeUrlPreference(kvp, Browsers);
				Assert.AreEqual("vivaldi", up.Browser.Name);
				Assert.AreEqual("vivaldi.exe", up.Browser.Location);
				Assert.AreEqual("evil.com", up.UrlPattern);
				Assert.IsNotNull(up.Transform);
				Assert.AreEqual(@"http://good.com/get/abc123", up.Transform(@"http://evil.com/view?id=abc123"));
				Assert.AreEqual(@"https://good.com/get/abc123", up.Transform(@"https://evil.com/view?id=abc123"));
			}

			// Example: URL parameter removal
			{
				var kvp = new KeyValuePair<string, string>(
					@"clickbait.com",
					@"vivaldi:s|utm_[^&#]*&?(#)?|$1|"
				);
				UrlPreference up = MakeUrlPreference(kvp, Browsers);
				Assert.AreEqual("vivaldi", up.Browser.Name);
				Assert.AreEqual("vivaldi.exe", up.Browser.Location);
				Assert.AreEqual("clickbait.com", up.UrlPattern);
				Assert.IsNotNull(up.Transform);
				Assert.AreEqual(@"https://clickbait.com/article?",
					up.Transform(@"https://clickbait.com/article?utm_source=bla&utm_medium=ugh"));
				Assert.AreEqual(@"https://clickbait.com/article?article_id=123",
					up.Transform(@"https://clickbait.com/article?utm_source=bla&utm_medium=ugh&article_id=123"));
				Assert.AreEqual(@"https://clickbait.com/article?article_id=123&",
					up.Transform(@"https://clickbait.com/article?utm_source=bla&article_id=123&utm_medium=ugh"));
				Assert.AreEqual(@"https://clickbait.com/article?article_id=123&user_id=abc#comments",
					up.Transform(@"https://clickbait.com/article?utm_source=bla&article_id=123&utm_medium=ugh&user_id=abc#comments"));
			}
		}


		[TestMethod]
		public void MakeUrlPreference_HostPlusTransform_Failure1()
		{
			// This test currently fails because we don't parse the find part but regex-match the delimiters
			{
				var kvp = new KeyValuePair<string, string>(@"example.com", @"vivaldi:s|(foo|bar)|boo|");
				UrlPreference up = MakeUrlPreference(kvp, Browsers);
				Assert.AreEqual("vivaldi", up.Browser.Name);
				Assert.AreEqual("vivaldi.exe", up.Browser.Location);
				Assert.AreEqual("example.com", up.UrlPattern);
				Assert.IsNotNull(up.Transform);
				Assert.AreEqual("boo", up.Transform("foo"));
				Assert.AreEqual("boo", up.Transform("bar"));
			}
		}

		[TestMethod]
		public void MakeUrlPreference_HostPlusTransform_Failure2()
		{
			// This test currently fails because we don't support escape characters in the replace part
			{
				var kvp = new KeyValuePair<string, string>(@"example.com", @"vivaldi:s|ApipeB|A\|B|");
				UrlPreference up = MakeUrlPreference(kvp, Browsers);
				Assert.AreEqual("vivaldi", up.Browser.Name);
				Assert.AreEqual("vivaldi.exe", up.Browser.Location);
				Assert.AreEqual("example.com", up.UrlPattern);
				Assert.IsNotNull(up.Transform);
				Assert.AreEqual("ApipeB", up.Transform("A|B"));
			}
		}
	}
}
