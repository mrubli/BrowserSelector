using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DanTup.BrowserSelector;
using System.Collections.Generic;

namespace UnitTest
{
	[TestClass]
	public class TestConfigReader
	{
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

		// These are tests that should succeed but don't currently
		public void SplitConfig_Failure()
		{
			// The parsing function chokes on the equals in "foo=bar" and mistakenly splits the line there.
			Assert.AreNotEqual(
				new KeyValuePair<string, string>(@"/example\.(com|net)/app\?foo=bar/", "vivaldi"),
				SplitConfig(@"/example\.(com|net)/app\?foo=bar/=vivaldi")
			);
		}
	}
}
