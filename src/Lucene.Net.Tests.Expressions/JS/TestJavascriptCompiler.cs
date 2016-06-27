using System;
using Lucene.Net.Expressions.JS;
using Xunit;

namespace Lucene.Net.Tests.Expressions.JS
{
	public class TestJavascriptCompiler : Util.LuceneTestCase
	{
		[Fact]
		public virtual void TestValidCompiles()
		{
			NotNull(JavascriptCompiler.Compile("100"));
			NotNull(JavascriptCompiler.Compile("valid0+100"));
			NotNull(JavascriptCompiler.Compile("valid0+\n100"));
			NotNull(JavascriptCompiler.Compile("logn(2, 20+10-5.0)"));
		}

		[Fact]
		public virtual void TestValidNamespaces()
		{
			NotNull(JavascriptCompiler.Compile("object.valid0"));
			NotNull(JavascriptCompiler.Compile("object0.object1.valid1"));
		}

		//TODO: change all exceptions to ParseExceptions
		[Fact]
		public virtual void TestInvalidNamespaces()
		{
			Assert.ThrowsAny<Exception>(() => JavascriptCompiler.Compile("object.0invalid"));
			Assert.ThrowsAny<Exception>(() => JavascriptCompiler.Compile("0.invalid"));
			Assert.ThrowsAny<Exception>(() => JavascriptCompiler.Compile("object..invalid"));
			Assert.ThrowsAny<Exception>(() => JavascriptCompiler.Compile(".invalid"));
		}

		//expected
		[Fact]
		public virtual void TestInvalidCompiles()
		{
			Assert.ThrowsAny<Exception>(() => JavascriptCompiler.Compile("100 100"));
			// expected exception
			Assert.ThrowsAny<Exception>(() => JavascriptCompiler.Compile("7*/-8"));
			Assert.ThrowsAny<Exception>(() => JavascriptCompiler.Compile("0y1234"));
			// expected exception
			Assert.ThrowsAny<Exception>(() => JavascriptCompiler.Compile("500EE"));
			// expected exception
			Assert.ThrowsAny<Exception>(() => JavascriptCompiler.Compile("500.5EE"));
		}

		[Fact]
		public virtual void TestEmpty()
		{
			Assert.ThrowsAny<Exception>(() => JavascriptCompiler.Compile(string.Empty));
			Assert.ThrowsAny<Exception>(() => JavascriptCompiler.Compile("()"));
			Assert.ThrowsAny<Exception>(() => JavascriptCompiler.Compile("   \r\n   \n \t"));
		}

		// expected exception
		[Fact]
		public virtual void TestNull()
		{
			Assert.Throws<ArgumentNullException>(() => JavascriptCompiler.Compile(null));
		}

		// expected exception
		[Fact]
		public virtual void TestWrongArity()
		{
			var expected = Assert.Throws<ArgumentNullException>(() => JavascriptCompiler.Compile("tan()"));
			True(expected.Message.Contains("arguments for method call"));

			var expected2 = Assert.Throws<ArgumentException>(() => JavascriptCompiler.Compile("tan(1, 1)"));
			True(expected2.Message.Contains("arguments for method call"));
		}
	}
}
