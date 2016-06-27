using Lucene.Net.Expressions;
using Lucene.Net.Expressions.JS;
using Lucene.Net.Search;
using Xunit;

namespace Lucene.Net.Tests.Expressions
{
	public class TestExpressionSortField : Util.LuceneTestCase
	{
		[Fact]
		public virtual void TestToString()
		{
			Expression expr = JavascriptCompiler.Compile("sqrt(_score) + ln(popularity)");
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add(new SortField("_score", SortField.Type_e.SCORE));
			bindings.Add(new SortField("popularity", SortField.Type_e.INT));
			SortField sf = expr.GetSortField(bindings, true);
			Equal("<expr \"sqrt(_score) + ln(popularity)\">!", sf.ToString());
		}

		[Fact]
		public virtual void TestEquals()
		{
			Expression expr = JavascriptCompiler.Compile("sqrt(_score) + ln(popularity)");
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add(new SortField("_score", SortField.Type_e.SCORE));
			bindings.Add(new SortField("popularity", SortField.Type_e.INT));
			SimpleBindings otherBindings = new SimpleBindings();
			otherBindings.Add(new SortField("_score", SortField.Type_e.LONG));
			otherBindings.Add(new SortField("popularity", SortField.Type_e.INT));
			SortField sf1 = expr.GetSortField(bindings, true);
			// different order
			SortField sf2 = expr.GetSortField(bindings, false);
			False(sf1.Equals(sf2));
			// different bindings
			sf2 = expr.GetSortField(otherBindings, true);
			False(sf1.Equals(sf2));
			// different expression
			Expression other = JavascriptCompiler.Compile("popularity/2");
			sf2 = other.GetSortField(bindings, true);
			False(sf1.Equals(sf2));
			// null
			False(sf1.Equals(null));
			// same instance:
			Equal(sf1, sf1);
		}

		[Fact]
		public virtual void TestNeedsScores()
		{
			SimpleBindings bindings = new SimpleBindings();
			// refers to score directly
			Expression exprA = JavascriptCompiler.Compile("_score");
			// constant
			Expression exprB = JavascriptCompiler.Compile("0");
			// field
			Expression exprC = JavascriptCompiler.Compile("intfield");
			// score + constant
			Expression exprD = JavascriptCompiler.Compile("_score + 0");
			// field + constant
			Expression exprE = JavascriptCompiler.Compile("intfield + 0");
			// expression + constant (score ref'd)
			Expression exprF = JavascriptCompiler.Compile("a + 0");
			// expression + constant
			Expression exprG = JavascriptCompiler.Compile("e + 0");
			// several variables (score ref'd)
			Expression exprH = JavascriptCompiler.Compile("b / c + e * g - sqrt(f)");
			// several variables
			Expression exprI = JavascriptCompiler.Compile("b / c + e * g");
			bindings.Add(new SortField("_score", SortField.Type_e.SCORE));
			bindings.Add(new SortField("intfield", SortField.Type_e.INT));
			bindings.Add("a", exprA);
			bindings.Add("b", exprB);
			bindings.Add("c", exprC);
			bindings.Add("d", exprD);
			bindings.Add("e", exprE);
			bindings.Add("f", exprF);
			bindings.Add("g", exprG);
			bindings.Add("h", exprH);
			bindings.Add("i", exprI);
			True(exprA.GetSortField(bindings, true).NeedsScores());
			False(exprB.GetSortField(bindings, true).NeedsScores());
			False(exprC.GetSortField(bindings, true).NeedsScores());
			True(exprD.GetSortField(bindings, true).NeedsScores());
			False(exprE.GetSortField(bindings, true).NeedsScores());
			True(exprF.GetSortField(bindings, true).NeedsScores());
			False(exprG.GetSortField(bindings, true).NeedsScores());
			True(exprH.GetSortField(bindings, true).NeedsScores());
			False(exprI.GetSortField(bindings, false).NeedsScores());
		}
	}
}
