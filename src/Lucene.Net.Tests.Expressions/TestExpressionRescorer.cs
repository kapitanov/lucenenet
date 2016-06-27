using Lucene.Net.Documents;
using Lucene.Net.Expressions;
using Lucene.Net.Expressions.JS;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Xunit;

namespace Lucene.Net.Tests.Expressions
{
	public class TestExpressionRescorer : Lucene.Net.Util.LuceneTestCase
	{
		internal IndexSearcher searcher;

		internal DirectoryReader reader;

		internal Directory dir;

		public TestExpressionRescorer() : base()
		{
			dir = NewDirectory();
			var iw = new RandomIndexWriter(Random(), dir);
			var doc = new Document
			{
			    NewStringField("id", "1", Field.Store.YES),
			    NewTextField("body", "some contents and more contents", Field.Store.NO),
			    new NumericDocValuesField("popularity", 5)
			};
		    iw.AddDocument(doc);
			doc = new Document
			{
			    NewStringField("id", "2", Field.Store.YES),
			    NewTextField("body", "another document with different contents", Field.Store
			        .NO),
			    new NumericDocValuesField("popularity", 20)
			};
		    iw.AddDocument(doc);
			doc = new Document
			{
			    NewStringField("id", "3", Field.Store.YES),
			    NewTextField("body", "crappy contents", Field.Store.NO),
			    new NumericDocValuesField("popularity", 2)
			};
		    iw.AddDocument(doc);
			reader = iw.Reader;
			searcher = new IndexSearcher(reader);
			iw.Dispose();
		}

		public override void Dispose()
		{
			reader.Dispose();
			dir.Dispose();
			base.Dispose();
		}

		[Fact]
		public virtual void TestBasic()
		{
			// create a sort field and sort by it (reverse order)
			Query query = new TermQuery(new Term("body", "contents"));
			IndexReader r = searcher.IndexReader;
			// Just first pass query
			TopDocs hits = searcher.Search(query, 10);
			Equal(3, hits.TotalHits);
			Equal("3", r.Document(hits.ScoreDocs[0].Doc).Get("id"));
			Equal("1", r.Document(hits.ScoreDocs[1].Doc).Get("id"));
			Equal("2", r.Document(hits.ScoreDocs[2].Doc).Get("id"));
			// Now, rescore:
			Expression e = JavascriptCompiler.Compile("sqrt(_score) + ln(popularity)");
			SimpleBindings bindings = new SimpleBindings();
			bindings.Add(new SortField("popularity", SortField.Type_e.INT));
			bindings.Add(new SortField("_score", SortField.Type_e.SCORE));
			Rescorer rescorer = e.GetRescorer(bindings);
			hits = rescorer.Rescore(searcher, hits, 10);
			Equal(3, hits.TotalHits);
			Equal("2", r.Document(hits.ScoreDocs[0].Doc).Get("id"));
			Equal("1", r.Document(hits.ScoreDocs[1].Doc).Get("id"));
			Equal("3", r.Document(hits.ScoreDocs[2].Doc).Get("id"));
			string expl = rescorer.Explain(searcher, searcher.Explain(query, hits.ScoreDocs[0].Doc), hits.ScoreDocs[0].Doc).ToString();
			// Confirm the explanation breaks out the individual
			// variables:
			True(expl.Contains("= variable \"popularity\""));
			// Confirm the explanation includes first pass details:
			True(expl.Contains("= first pass score"));
			True(expl.Contains("body:contents in"));
		}
	}
}
