using System;
using Lucene.Net.Expressions;
using Lucene.Net.Expressions.JS;
using Lucene.Net.Search;
using Xunit;

namespace Lucene.Net.Tests.Expressions
{
	/// <summary>Tests validation of bindings</summary>
	public class TestExpressionValidation : Util.LuceneTestCase
	{
		[Fact]
		public virtual void TestValidExternals()
		{
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add(new SortField("valid0", SortField.Type_e.INT));
			bindings.Add(new SortField("valid1", SortField.Type_e.INT));
			bindings.Add(new SortField("valid2", SortField.Type_e.INT));
			bindings.Add(new SortField("_score", SortField.Type_e.SCORE));
			bindings.Add("valide0", JavascriptCompiler.Compile("valid0 - valid1 + valid2 + _score"
				));
			bindings.Validate();
			bindings.Add("valide1", JavascriptCompiler.Compile("valide0 + valid0"));
			bindings.Validate();
			bindings.Add("valide2", JavascriptCompiler.Compile("valide0 * valide1"));
			bindings.Validate();
		}

		[Fact]
		public virtual void TestInvalidExternal()
		{
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add(new SortField("valid", SortField.Type_e.INT));
			bindings.Add("invalid", JavascriptCompiler.Compile("badreference"));
			var expected = Assert.Throws<ArgumentException>(() => bindings.Validate());
			True(expected.Message.Contains("Invalid reference"));
		}

		[Fact]
		public virtual void TestInvalidExternal2()
		{
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add(new SortField("valid", SortField.Type_e.INT));
			bindings.Add("invalid", JavascriptCompiler.Compile("valid + badreference"));
			var expected = Assert.Throws<ArgumentException>(() => bindings.Validate());
			True(expected.Message.Contains("Invalid reference"));
		}

		[Fact(Skip = "StackOverflowException can't be caught in .NET")]
		public virtual void TestSelfRecursion()
		{
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add("cycle0", JavascriptCompiler.Compile("cycle0"));
			var expected = Assert.Throws<ArgumentException>(() => bindings.Validate());
			True(expected.Message.Contains("Cycle detected"));
		}

		[Fact(Skip = "StackOverflowException can't be caught in .NET")]
		public virtual void TestCoRecursion()
		{
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add("cycle0", JavascriptCompiler.Compile("cycle1"));
			bindings.Add("cycle1", JavascriptCompiler.Compile("cycle0"));
			var expected = Assert.Throws<ArgumentException>(() => bindings.Validate());
			True(expected.Message.Contains("Cycle detected"));
		}

		[Fact(Skip = "StackOverflowException can't be caught in .NET")]
		public virtual void TestCoRecursion2()
		{
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add("cycle0", JavascriptCompiler.Compile("cycle1"));
			bindings.Add("cycle1", JavascriptCompiler.Compile("cycle2"));
			bindings.Add("cycle2", JavascriptCompiler.Compile("cycle0"));
			var expected = Assert.Throws<ArgumentException>(() => bindings.Validate());
			True(expected.Message.Contains("Cycle detected"));
		}

		[Fact(Skip = "StackOverflowException can't be caught in .NET")]
		public virtual void TestCoRecursion3()
		{
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add("cycle0", JavascriptCompiler.Compile("100"));
			bindings.Add("cycle1", JavascriptCompiler.Compile("cycle0 + cycle2"));
			bindings.Add("cycle2", JavascriptCompiler.Compile("cycle0 + cycle1"));
			var expected = Assert.Throws<ArgumentException>(() => bindings.Validate());
			True(expected.Message.Contains("Cycle detected"));
		}

		[Fact(Skip = "StackOverflowException can't be caught in .NET")]
		public virtual void TestCoRecursion4()
		{
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add("cycle0", JavascriptCompiler.Compile("100"));
			bindings.Add("cycle1", JavascriptCompiler.Compile("100"));
			bindings.Add("cycle2", JavascriptCompiler.Compile("cycle1 + cycle0 + cycle3"));
			bindings.Add("cycle3", JavascriptCompiler.Compile("cycle0 + cycle1 + cycle2"));
			var expected = Assert.Throws<ArgumentException>(() => bindings.Validate());
			True(expected.Message.Contains("Cycle detected"));
		}
	}
}
