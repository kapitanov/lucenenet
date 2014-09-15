using Lucene.Net.Documents;

namespace Lucene.Net.Search.Similarities
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using FieldInvertState = Lucene.Net.Index.FieldInvertState;
    using NumericDocValues = Lucene.Net.Index.NumericDocValues;

    /// <summary>
    /// Implementation of <seealso cref="Similarity"/> with the Vector Space Model.
    /// <p>
    /// Expert: Scoring API.
    /// <p>TFIDFSimilarity defines the components of Lucene scoring.
    /// Overriding computation of these components is a convenient
    /// way to alter Lucene scoring.
    ///
    /// <p>Suggested reading:
    /// <a href="http://nlp.stanford.edu/IR-book/html/htmledition/queries-as-vectors-1.html">
    /// Introduction To Information Retrieval, Chapter 6</a>.
    ///
    /// <p>The following describes how Lucene scoring evolves from
    /// underlying information retrieval models to (efficient) implementation.
    /// We first brief on <i>VSM Score</i>,
    /// then derive from it <i>Lucene's Conceptual Scoring Formula</i>,
    /// from which, finally, evolves <i>Lucene's Practical Scoring Function</i>
    /// (the latter is connected directly with Lucene classes and methods).
    ///
    /// <p>Lucene combines
    /// <a href="http://en.wikipedia.org/wiki/Standard_Boolean_model">
    /// Boolean model (BM) of Information Retrieval</a>
    /// with
    /// <a href="http://en.wikipedia.org/wiki/Vector_Space_Model">
    /// Vector Space Model (VSM) of Information Retrieval</a> -
    /// documents "approved" by BM are scored by VSM.
    ///
    /// <p>In VSM, documents and queries are represented as
    /// weighted vectors in a multi-dimensional space,
    /// where each distinct index term is a dimension,
    /// and weights are
    /// <a href="http://en.wikipedia.org/wiki/Tfidf">Tf-idf</a> values.
    ///
    /// <p>VSM does not require weights to be <i>Tf-idf</i> values,
    /// but <i>Tf-idf</i> values are believed to produce search results of high quality,
    /// and so Lucene is using <i>Tf-idf</i>.
    /// <i>Tf</i> and <i>Idf</i> are described in more detail below,
    /// but for now, for completion, let's just say that
    /// for given term <i>t</i> and document (or query) <i>x</i>,
    /// <i>Tf(t,x)</i> varies with the number of occurrences of term <i>t</i> in <i>x</i>
    /// (when one increases so does the other) and
    /// <i>idf(t)</i> similarly varies with the inverse of the
    /// number of index documents containing term <i>t</i>.
    ///
    /// <p><i>VSM score</i> of document <i>d</i> for query <i>q</i> is the
    /// <a href="http://en.wikipedia.org/wiki/Cosine_similarity">
    /// Cosine Similarity</a>
    /// of the weighted query vectors <i>V(q)</i> and <i>V(d)</i>:
    ///
    ///  <br>&nbsp;<br>
    ///  <table cellpadding="2" cellspacing="2" border="0" align="center" style="width:auto">
    ///    <tr><td>
    ///    <table cellpadding="1" cellspacing="0" border="1" align="center">
    ///      <tr><td>
    ///      <table cellpadding="2" cellspacing="2" border="0" align="center">
    ///        <tr>
    ///          <td valign="middle" align="right" rowspan="1">
    ///            cosine-similarity(q,d) &nbsp; = &nbsp;
    ///          </td>
    ///          <td valign="middle" align="center">
    ///            <table>
    ///               <tr><td align="center" style="text-align: center"><small>V(q)&nbsp;&middot;&nbsp;V(d)</small></td></tr>
    ///               <tr><td align="center" style="text-align: center">&ndash;&ndash;&ndash;&ndash;&ndash;&ndash;&ndash;&ndash;&ndash;</td></tr>
    ///               <tr><td align="center" style="text-align: center"><small>|V(q)|&nbsp;|V(d)|</small></td></tr>
    ///            </table>
    ///          </td>
    ///        </tr>
    ///      </table>
    ///      </td></tr>
    ///    </table>
    ///    </td></tr>
    ///    <tr><td>
    ///    <center><font size=-1><u>VSM Score</u></font></center>
    ///    </td></tr>
    ///  </table>
    ///  <br>&nbsp;<br>
    ///
    ///
    /// Where <i>V(q)</i> &middot; <i>V(d)</i> is the
    /// <a href="http://en.wikipedia.org/wiki/Dot_product">dot product</a>
    /// of the weighted vectors,
    /// and <i>|V(q)|</i> and <i>|V(d)|</i> are their
    /// <a href="http://en.wikipedia.org/wiki/Euclidean_norm#Euclidean_norm">Euclidean norms</a>.
    ///
    /// <p>Note: the above equation can be viewed as the dot product of
    /// the normalized weighted vectors, in the sense that dividing
    /// <i>V(q)</i> by its euclidean norm is normalizing it to a unit vector.
    ///
    /// <p>Lucene refines <i>VSM score</i> for both search quality and usability:
    /// <ul>
    ///  <li>Normalizing <i>V(d)</i> to the unit vector is known to be problematic in that
    ///  it removes all document length information.
    ///  For some documents removing this info is probably ok,
    ///  e.g. a document made by duplicating a certain paragraph <i>10</i> times,
    ///  especially if that paragraph is made of distinct terms.
    ///  But for a document which contains no duplicated paragraphs,
    ///  this might be wrong.
    ///  To avoid this problem, a different document length normalization
    ///  factor is used, which normalizes to a vector equal to or larger
    ///  than the unit vector: <i>doc-len-norm(d)</i>.
    ///  </li>
    ///
    ///  <li>At indexing, users can specify that certain documents are more
    ///  important than others, by assigning a document boost.
    ///  For this, the score of each document is also multiplied by its boost value
    ///  <i>doc-boost(d)</i>.
    ///  </li>
    ///
    ///  <li>Lucene is field based, hence each query term applies to a single
    ///  field, document length normalization is by the length of the certain field,
    ///  and in addition to document boost there are also document fields boosts.
    ///  </li>
    ///
    ///  <li>The same field can be added to a document during indexing several times,
    ///  and so the boost of that field is the multiplication of the boosts of
    ///  the separate additions (or parts) of that field within the document.
    ///  </li>
    ///
    ///  <li>At search time users can specify boosts to each query, sub-query, and
    ///  each query term, hence the contribution of a query term to the score of
    ///  a document is multiplied by the boost of that query term <i>query-boost(q)</i>.
    ///  </li>
    ///
    ///  <li>A document may match a multi term query without containing all
    ///  the terms of that query (this is correct for some of the queries),
    ///  and users can further reward documents matching more query terms
    ///  through a coordination factor, which is usually larger when
    ///  more terms are matched: <i>coord-factor(q,d)</i>.
    ///  </li>
    /// </ul>
    ///
    /// <p>Under the simplifying assumption of a single field in the index,
    /// we get <i>Lucene's Conceptual scoring formula</i>:
    ///
    ///  <br>&nbsp;<br>
    ///  <table cellpadding="2" cellspacing="2" border="0" align="center" style="width:auto">
    ///    <tr><td>
    ///    <table cellpadding="1" cellspacing="0" border="1" align="center">
    ///      <tr><td>
    ///      <table cellpadding="2" cellspacing="2" border="0" align="center">
    ///        <tr>
    ///          <td valign="middle" align="right" rowspan="1">
    ///            score(q,d) &nbsp; = &nbsp;
    ///            <font color="#FF9933">coord-factor(q,d)</font> &middot; &nbsp;
    ///            <font color="#CCCC00">query-boost(q)</font> &middot; &nbsp;
    ///          </td>
    ///          <td valign="middle" align="center">
    ///            <table>
    ///               <tr><td align="center" style="text-align: center"><small><font color="#993399">V(q)&nbsp;&middot;&nbsp;V(d)</font></small></td></tr>
    ///               <tr><td align="center" style="text-align: center">&ndash;&ndash;&ndash;&ndash;&ndash;&ndash;&ndash;&ndash;&ndash;</td></tr>
    ///               <tr><td align="center" style="text-align: center"><small><font color="#FF33CC">|V(q)|</font></small></td></tr>
    ///            </table>
    ///          </td>
    ///          <td valign="middle" align="right" rowspan="1">
    ///            &nbsp; &middot; &nbsp; <font color="#3399FF">doc-len-norm(d)</font>
    ///            &nbsp; &middot; &nbsp; <font color="#3399FF">doc-boost(d)</font>
    ///          </td>
    ///        </tr>
    ///      </table>
    ///      </td></tr>
    ///    </table>
    ///    </td></tr>
    ///    <tr><td>
    ///    <center><font size=-1><u>Lucene Conceptual Scoring Formula</u></font></center>
    ///    </td></tr>
    ///  </table>
    ///  <br>&nbsp;<br>
    ///
    /// <p>The conceptual formula is a simplification in the sense that (1) terms and documents
    /// are fielded and (2) boosts are usually per query term rather than per query.
    ///
    /// <p>We now describe how Lucene implements this conceptual scoring formula, and
    /// derive from it <i>Lucene's Practical Scoring Function</i>.
    ///
    /// <p>For efficient score computation some scoring components
    /// are computed and aggregated in advance:
    ///
    /// <ul>
    ///  <li><i>Query-boost</i> for the query (actually for each query term)
    ///  is known when search starts.
    ///  </li>
    ///
    ///  <li>Query Euclidean norm <i>|V(q)|</i> can be computed when search starts,
    ///  as it is independent of the document being scored.
    ///  From search optimization perspective, it is a valid question
    ///  why bother to normalize the query at all, because all
    ///  scored documents will be multiplied by the same <i>|V(q)|</i>,
    ///  and hence documents ranks (their order by score) will not
    ///  be affected by this normalization.
    ///  There are two good reasons to keep this normalization:
    ///  <ul>
    ///   <li>Recall that
    ///   <a href="http://en.wikipedia.org/wiki/Cosine_similarity">
    ///   Cosine Similarity</a> can be used find how similar
    ///   two documents are. One can use Lucene for e.g.
    ///   clustering, and use a document as a query to compute
    ///   its similarity to other documents.
    ///   In this use case it is important that the score of document <i>d3</i>
    ///   for query <i>d1</i> is comparable to the score of document <i>d3</i>
    ///   for query <i>d2</i>. In other words, scores of a document for two
    ///   distinct queries should be comparable.
    ///   There are other applications that may require this.
    ///   And this is exactly what normalizing the query vector <i>V(q)</i>
    ///   provides: comparability (to a certain extent) of two or more queries.
    ///   </li>
    ///
    ///   <li>Applying query normalization on the scores helps to keep the
    ///   scores around the unit vector, hence preventing loss of score data
    ///   because of floating point precision limitations.
    ///   </li>
    ///  </ul>
    ///  </li>
    ///
    ///  <li>Document length norm <i>doc-len-norm(d)</i> and document
    ///  boost <i>doc-boost(d)</i> are known at indexing time.
    ///  They are computed in advance and their multiplication
    ///  is saved as a single value in the index: <i>norm(d)</i>.
    ///  (In the equations below, <i>norm(t in d)</i> means <i>norm(field(t) in doc d)</i>
    ///  where <i>field(t)</i> is the field associated with term <i>t</i>.)
    ///  </li>
    /// </ul>
    ///
    /// <p><i>Lucene's Practical Scoring Function</i> is derived from the above.
    /// The color codes demonstrate how it relates
    /// to those of the <i>conceptual</i> formula:
    ///
    /// <P>
    /// <table cellpadding="2" cellspacing="2" border="0" align="center" style="width:auto">
    ///  <tr><td>
    ///  <table cellpadding="" cellspacing="2" border="2" align="center">
    ///  <tr><td>
    ///   <table cellpadding="2" cellspacing="2" border="0" align="center">
    ///   <tr>
    ///     <td valign="middle" align="right" rowspan="1">
    ///       score(q,d) &nbsp; = &nbsp;
    ///       <A HREF="#formula_coord"><font color="#FF9933">coord(q,d)</font></A> &nbsp;&middot;&nbsp;
    ///       <A HREF="#formula_queryNorm"><font color="#FF33CC">queryNorm(q)</font></A> &nbsp;&middot;&nbsp;
    ///     </td>
    ///     <td valign="bottom" align="center" rowspan="1" style="text-align: center">
    ///       <big><big><big>&sum;</big></big></big>
    ///     </td>
    ///     <td valign="middle" align="right" rowspan="1">
    ///       <big><big>(</big></big>
    ///       <A HREF="#formula_tf"><font color="#993399">tf(t in d)</font></A> &nbsp;&middot;&nbsp;
    ///       <A HREF="#formula_idf"><font color="#993399">idf(t)</font></A><sup>2</sup> &nbsp;&middot;&nbsp;
    ///       <A HREF="#formula_termBoost"><font color="#CCCC00">t.getBoost()</font></A>&nbsp;&middot;&nbsp;
    ///       <A HREF="#formula_norm"><font color="#3399FF">norm(t,d)</font></A>
    ///       <big><big>)</big></big>
    ///     </td>
    ///   </tr>
    ///   <tr valigh="top">
    ///    <td></td>
    ///    <td align="center" style="text-align: center"><small>t in q</small></td>
    ///    <td></td>
    ///   </tr>
    ///   </table>
    ///  </td></tr>
    ///  </table>
    /// </td></tr>
    /// <tr><td>
    ///  <center><font size=-1><u>Lucene Practical Scoring Function</u></font></center>
    /// </td></tr>
    /// </table>
    ///
    /// <p> where
    /// <ol>
    ///    <li>
    ///      <A NAME="formula_tf"></A>
    ///      <b><i>tf(t in d)</i></b>
    ///      correlates to the term's <i>frequency</i>,
    ///      defined as the number of times term <i>t</i> appears in the currently scored document <i>d</i>.
    ///      Documents that have more occurrences of a given term receive a higher score.
    ///      Note that <i>tf(t in q)</i> is assumed to be <i>1</i> and therefore it does not appear in this equation,
    ///      However if a query contains twice the same term, there will be
    ///      two term-queries with that same term and hence the computation would still be correct (although
    ///      not very efficient).
    ///      The default computation for <i>tf(t in d)</i> in
    ///      <seealso cref="Lucene.Net.Search.Similarities.DefaultSimilarity#tf(float) DefaultSimilarity"/> is:
    ///
    ///      <br>&nbsp;<br>
    ///      <table cellpadding="2" cellspacing="2" border="0" align="center" style="width:auto">
    ///        <tr>
    ///          <td valign="middle" align="right" rowspan="1">
    ///            <seealso cref="Lucene.Net.Search.Similarities.DefaultSimilarity#tf(float) tf(t in d)"/> &nbsp; = &nbsp;
    ///          </td>
    ///          <td valign="top" align="center" rowspan="1">
    ///               frequency<sup><big>&frac12;</big></sup>
    ///          </td>
    ///        </tr>
    ///      </table>
    ///      <br>&nbsp;<br>
    ///    </li>
    ///
    ///    <li>
    ///      <A NAME="formula_idf"></A>
    ///      <b><i>idf(t)</i></b> stands for Inverse Document Frequency. this value
    ///      correlates to the inverse of <i>docFreq</i>
    ///      (the number of documents in which the term <i>t</i> appears).
    ///      this means rarer terms give higher contribution to the total score.
    ///      <i>idf(t)</i> appears for <i>t</i> in both the query and the document,
    ///      hence it is squared in the equation.
    ///      The default computation for <i>idf(t)</i> in
    ///      <seealso cref="Lucene.Net.Search.Similarities.DefaultSimilarity#idf(long, long) DefaultSimilarity"/> is:
    ///
    ///      <br>&nbsp;<br>
    ///      <table cellpadding="2" cellspacing="2" border="0" align="center" style="width:auto">
    ///        <tr>
    ///          <td valign="middle" align="right">
    ///            <seealso cref="Lucene.Net.Search.Similarities.DefaultSimilarity#idf(long, long) idf(t)"/>&nbsp; = &nbsp;
    ///          </td>
    ///          <td valign="middle" align="center">
    ///            1 + log <big>(</big>
    ///          </td>
    ///          <td valign="middle" align="center">
    ///            <table>
    ///               <tr><td align="center" style="text-align: center"><small>numDocs</small></td></tr>
    ///               <tr><td align="center" style="text-align: center">&ndash;&ndash;&ndash;&ndash;&ndash;&ndash;&ndash;&ndash;&ndash;</td></tr>
    ///               <tr><td align="center" style="text-align: center"><small>docFreq+1</small></td></tr>
    ///            </table>
    ///          </td>
    ///          <td valign="middle" align="center">
    ///            <big>)</big>
    ///          </td>
    ///        </tr>
    ///      </table>
    ///      <br>&nbsp;<br>
    ///    </li>
    ///
    ///    <li>
    ///      <A NAME="formula_coord"></A>
    ///      <b><i>coord(q,d)</i></b>
    ///      is a score factor based on how many of the query terms are found in the specified document.
    ///      Typically, a document that contains more of the query's terms will receive a higher score
    ///      than another document with fewer query terms.
    ///      this is a search time factor computed in
    ///      <seealso cref="#coord(int, int) coord(q,d)"/>
    ///      by the Similarity in effect at search time.
    ///      <br>&nbsp;<br>
    ///    </li>
    ///
    ///    <li><b>
    ///      <A NAME="formula_queryNorm"></A>
    ///      <i>queryNorm(q)</i>
    ///      </b>
    ///      is a normalizing factor used to make scores between queries comparable.
    ///      this factor does not affect document ranking (since all ranked documents are multiplied by the same factor),
    ///      but rather just attempts to make scores from different queries (or even different indexes) comparable.
    ///      this is a search time factor computed by the Similarity in effect at search time.
    ///
    ///      The default computation in
    ///      <seealso cref="Lucene.Net.Search.Similarities.DefaultSimilarity#queryNorm(float) DefaultSimilarity"/>
    ///      produces a <a href="http://en.wikipedia.org/wiki/Euclidean_norm#Euclidean_norm">Euclidean norm</a>:
    ///      <br>&nbsp;<br>
    ///      <table cellpadding="1" cellspacing="0" border="0" align="center" style="width:auto">
    ///        <tr>
    ///          <td valign="middle" align="right" rowspan="1">
    ///            queryNorm(q)  &nbsp; = &nbsp;
    ///            <seealso cref="Lucene.Net.Search.Similarities.DefaultSimilarity#queryNorm(float) queryNorm(sumOfSquaredWeights)"/>
    ///            &nbsp; = &nbsp;
    ///          </td>
    ///          <td valign="middle" align="center" rowspan="1">
    ///            <table>
    ///               <tr><td align="center" style="text-align: center"><big>1</big></td></tr>
    ///               <tr><td align="center" style="text-align: center"><big>
    ///                  &ndash;&ndash;&ndash;&ndash;&ndash;&ndash;&ndash;&ndash;&ndash;&ndash;&ndash;&ndash;&ndash;&ndash;
    ///               </big></td></tr>
    ///               <tr><td align="center" style="text-align: center">sumOfSquaredWeights<sup><big>&frac12;</big></sup></td></tr>
    ///            </table>
    ///          </td>
    ///        </tr>
    ///      </table>
    ///      <br>&nbsp;<br>
    ///
    ///      The sum of squared weights (of the query terms) is
    ///      computed by the query <seealso cref="Lucene.Net.Search.Weight"/> object.
    ///      For example, a <seealso cref="Lucene.Net.Search.BooleanQuery"/>
    ///      computes this value as:
    ///
    ///      <br>&nbsp;<br>
    ///      <table cellpadding="1" cellspacing="0" border="0" align="center" style="width:auto">
    ///        <tr>
    ///          <td valign="middle" align="right" rowspan="1">
    ///            <seealso cref="Lucene.Net.Search.Weight#getValueForNormalization() sumOfSquaredWeights"/> &nbsp; = &nbsp;
    ///            <seealso cref="Lucene.Net.Search.Query#getBoost() q.getBoost()"/> <sup><big>2</big></sup>
    ///            &nbsp;&middot;&nbsp;
    ///          </td>
    ///          <td valign="bottom" align="center" rowspan="1" style="text-align: center">
    ///            <big><big><big>&sum;</big></big></big>
    ///          </td>
    ///          <td valign="middle" align="right" rowspan="1">
    ///            <big><big>(</big></big>
    ///            <A HREF="#formula_idf">idf(t)</A> &nbsp;&middot;&nbsp;
    ///            <A HREF="#formula_termBoost">t.getBoost()</A>
    ///            <big><big>) <sup>2</sup> </big></big>
    ///          </td>
    ///        </tr>
    ///        <tr valigh="top">
    ///          <td></td>
    ///          <td align="center" style="text-align: center"><small>t in q</small></td>
    ///          <td></td>
    ///        </tr>
    ///      </table>
    ///      <br>&nbsp;<br>
    ///
    ///    </li>
    ///
    ///    <li>
    ///      <A NAME="formula_termBoost"></A>
    ///      <b><i>t.getBoost()</i></b>
    ///      is a search time boost of term <i>t</i> in the query <i>q</i> as
    ///      specified in the query text
    ///      (see <A HREF="{@docRoot}/../queryparser/org/apache/lucene/queryparser/classic/package-summary.html#Boosting_a_Term">query syntax</A>),
    ///      or as set by application calls to
    ///      <seealso cref="Lucene.Net.Search.Query#setBoost(float) setBoost()"/>.
    ///      Notice that there is really no direct API for accessing a boost of one term in a multi term query,
    ///      but rather multi terms are represented in a query as multi
    ///      <seealso cref="Lucene.Net.Search.TermQuery TermQuery"/> objects,
    ///      and so the boost of a term in the query is accessible by calling the sub-query
    ///      <seealso cref="Lucene.Net.Search.Query#getBoost() getBoost()"/>.
    ///      <br>&nbsp;<br>
    ///    </li>
    ///
    ///    <li>
    ///      <A NAME="formula_norm"></A>
    ///      <b><i>norm(t,d)</i></b> encapsulates a few (indexing time) boost and length factors:
    ///
    ///      <ul>
    ///        <li><b>Field boost</b> - set by calling
    ///        <seealso cref="Field#setBoost(float) field.setBoost()"/>
    ///        before adding the field to a document.
    ///        </li>
    ///        <li><b>lengthNorm</b> - computed
    ///        when the document is added to the index in accordance with the number of tokens
    ///        of this field in the document, so that shorter fields contribute more to the score.
    ///        LengthNorm is computed by the Similarity class in effect at indexing.
    ///        </li>
    ///      </ul>
    ///      The <seealso cref="#computeNorm"/> method is responsible for
    ///      combining all of these factors into a single float.
    ///
    ///      <p>
    ///      When a document is added to the index, all the above factors are multiplied.
    ///      If the document has multiple fields with the same name, all their boosts are multiplied together:
    ///
    ///      <br>&nbsp;<br>
    ///      <table cellpadding="1" cellspacing="0" border="0" align="center" style="width:auto">
    ///        <tr>
    ///          <td valign="middle" align="right" rowspan="1">
    ///            norm(t,d) &nbsp; = &nbsp;
    ///            lengthNorm
    ///            &nbsp;&middot;&nbsp;
    ///          </td>
    ///          <td valign="bottom" align="center" rowspan="1" style="text-align: center">
    ///            <big><big><big>&prod;</big></big></big>
    ///          </td>
    ///          <td valign="middle" align="right" rowspan="1">
    ///            <seealso cref="Lucene.Net.Index.IndexableField#boost() f.boost"/>()
    ///          </td>
    ///        </tr>
    ///        <tr valigh="top">
    ///          <td></td>
    ///          <td align="center" style="text-align: center"><small>field <i><b>f</b></i> in <i>d</i> named as <i><b>t</b></i></small></td>
    ///          <td></td>
    ///        </tr>
    ///      </table>
    ///      Note that search time is too late to modify this <i>norm</i> part of scoring,
    ///      e.g. by using a different <seealso cref="Similarity"/> for search.
    ///    </li>
    /// </ol>
    /// </summary>
    /// <seealso cref= Lucene.Net.Index.IndexWriterConfig#setSimilarity(Similarity) </seealso>
    /// <seealso cref= IndexSearcher#setSimilarity(Similarity) </seealso>
    public abstract class TFIDFSimilarity : Similarity
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        public TFIDFSimilarity()
        {
        }

        /// <summary>
        /// Computes a score factor based on the fraction of all query terms that a
        /// document contains.  this value is multiplied into scores.
        ///
        /// <p>The presence of a large portion of the query terms indicates a better
        /// match with the query, so implementations of this method usually return
        /// larger values when the ratio between these parameters is large and smaller
        /// values when the ratio between them is small.
        /// </summary>
        /// <param name="overlap"> the number of query terms matched in the document </param>
        /// <param name="maxOverlap"> the total number of terms in the query </param>
        /// <returns> a score factor based on term overlap with the query </returns>
        public override abstract float Coord(int overlap, int maxOverlap);

        /// <summary>
        /// Computes the normalization value for a query given the sum of the squared
        /// weights of each of the query terms.  this value is multiplied into the
        /// weight of each query term. While the classic query normalization factor is
        /// computed as 1/sqrt(sumOfSquaredWeights), other implementations might
        /// completely ignore sumOfSquaredWeights (ie return 1).
        ///
        /// <p>this does not affect ranking, but the default implementation does make scores
        /// from different queries more comparable than they would be by eliminating the
        /// magnitude of the Query vector as a factor in the score.
        /// </summary>
        /// <param name="sumOfSquaredWeights"> the sum of the squares of query term weights </param>
        /// <returns> a normalization factor for query weights </returns>
        public override abstract float QueryNorm(float sumOfSquaredWeights);

        /// <summary>
        /// Computes a score factor based on a term or phrase's frequency in a
        /// document.  this value is multiplied by the <seealso cref="#idf(long, long)"/>
        /// factor for each term in the query and these products are then summed to
        /// form the initial score for a document.
        ///
        /// <p>Terms and phrases repeated in a document indicate the topic of the
        /// document, so implementations of this method usually return larger values
        /// when <code>freq</code> is large, and smaller values when <code>freq</code>
        /// is small.
        /// </summary>
        /// <param name="freq"> the frequency of a term within a document </param>
        /// <returns> a score factor based on a term's within-document frequency </returns>
        public abstract float Tf(float freq);

        /// <summary>
        /// Computes a score factor for a simple term and returns an explanation
        /// for that score factor.
        ///
        /// <p>
        /// The default implementation uses:
        ///
        /// <pre class="prettyprint">
        /// idf(docFreq, searcher.maxDoc());
        /// </pre>
        ///
        /// Note that <seealso cref="CollectionStatistics#maxDoc()"/> is used instead of
        /// <seealso cref="Lucene.Net.Index.IndexReader#numDocs() IndexReader#numDocs()"/> because also
        /// <seealso cref="TermStatistics#docFreq()"/> is used, and when the latter
        /// is inaccurate, so is <seealso cref="CollectionStatistics#maxDoc()"/>, and in the same direction.
        /// In addition, <seealso cref="CollectionStatistics#maxDoc()"/> is more efficient to compute
        /// </summary>
        /// <param name="collectionStats"> collection-level statistics </param>
        /// <param name="termStats"> term-level statistics for the term </param>
        /// <returns> an Explain object that includes both an idf score factor
        ///           and an explanation for the term. </returns>
        public virtual Explanation IdfExplain(CollectionStatistics collectionStats, TermStatistics termStats)
        {
            long df = termStats.DocFreq();
            long max = collectionStats.MaxDoc();
            float idf = Idf(df, max);
            return new Explanation(idf, "idf(docFreq=" + df + ", maxDocs=" + max + ")");
        }

        /// <summary>
        /// Computes a score factor for a phrase.
        ///
        /// <p>
        /// The default implementation sums the idf factor for
        /// each term in the phrase.
        /// </summary>
        /// <param name="collectionStats"> collection-level statistics </param>
        /// <param name="termStats"> term-level statistics for the terms in the phrase </param>
        /// <returns> an Explain object that includes both an idf
        ///         score factor for the phrase and an explanation
        ///         for each term. </returns>
        public virtual Explanation IdfExplain(CollectionStatistics collectionStats, TermStatistics[] termStats)
        {
            long max = collectionStats.MaxDoc();
            float idf = 0.0f;
            Explanation exp = new Explanation();
            exp.Description = "idf(), sum of:";
            foreach (TermStatistics stat in termStats)
            {
                long df = stat.DocFreq();
                float termIdf = Idf(df, max);
                exp.AddDetail(new Explanation(termIdf, "idf(docFreq=" + df + ", maxDocs=" + max + ")"));
                idf += termIdf;
            }
            exp.Value = idf;
            return exp;
        }

        /// <summary>
        /// Computes a score factor based on a term's document frequency (the number
        /// of documents which contain the term).  this value is multiplied by the
        /// <seealso cref="#tf(float)"/> factor for each term in the query and these products are
        /// then summed to form the initial score for a document.
        ///
        /// <p>Terms that occur in fewer documents are better indicators of topic, so
        /// implementations of this method usually return larger values for rare terms,
        /// and smaller values for common terms.
        /// </summary>
        /// <param name="docFreq"> the number of documents which contain the term </param>
        /// <param name="numDocs"> the total number of documents in the collection </param>
        /// <returns> a score factor based on the term's document frequency </returns>
        public abstract float Idf(long docFreq, long numDocs);

        /// <summary>
        /// Compute an index-time normalization value for this field instance.
        /// <p>
        /// this value will be stored in a single byte lossy representation by
        /// <seealso cref="#encodeNormValue(float)"/>.
        /// </summary>
        /// <param name="state"> statistics of the current field (such as length, boost, etc) </param>
        /// <returns> an index-time normalization value </returns>
        public abstract float LengthNorm(FieldInvertState state);

        public override sealed long ComputeNorm(FieldInvertState state)
        {
            float normValue = LengthNorm(state);
            return EncodeNormValue(normValue);
        }

        /// <summary>
        /// Decodes a normalization factor stored in an index.
        /// </summary>
        /// <seealso cref= #encodeNormValue(float) </seealso>
        public abstract float DecodeNormValue(long norm);

        /// <summary>
        /// Encodes a normalization factor for storage in an index. </summary>
        public abstract long EncodeNormValue(float f);

        /// <summary>
        /// Computes the amount of a sloppy phrase match, based on an edit distance.
        /// this value is summed for each sloppy phrase match in a document to form
        /// the frequency to be used in scoring instead of the exact term count.
        ///
        /// <p>A phrase match with a small edit distance to a document passage more
        /// closely matches the document, so implementations of this method usually
        /// return larger values when the edit distance is small and smaller values
        /// when it is large.
        /// </summary>
        /// <seealso cref= PhraseQuery#setSlop(int) </seealso>
        /// <param name="distance"> the edit distance of this sloppy phrase match </param>
        /// <returns> the frequency increment for this match </returns>
        public abstract float SloppyFreq(int distance);

        /// <summary>
        /// Calculate a scoring factor based on the data in the payload.  Implementations
        /// are responsible for interpreting what is in the payload.  Lucene makes no assumptions about
        /// what is in the byte array.
        /// </summary>
        /// <param name="doc"> The docId currently being scored. </param>
        /// <param name="start"> The start position of the payload </param>
        /// <param name="end"> The end position of the payload </param>
        /// <param name="payload"> The payload byte array to be scored </param>
        /// <returns> An implementation dependent float to be used as a scoring factor </returns>
        public abstract float ScorePayload(int doc, int start, int end, BytesRef payload);

        public override sealed SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats, params TermStatistics[] termStats)
        {
            Explanation idf = termStats.Length == 1 ? IdfExplain(collectionStats, termStats[0]) : IdfExplain(collectionStats, termStats);
            return new IDFStats(collectionStats.Field(), idf, queryBoost);
        }

        public override sealed SimScorer DoSimScorer(SimWeight stats, AtomicReaderContext context)
        {
            IDFStats idfstats = (IDFStats)stats;
            return new TFIDFSimScorer(this, idfstats, context.AtomicReader.GetNormValues(idfstats.Field));
        }

        private sealed class TFIDFSimScorer : SimScorer
        {
            private readonly TFIDFSimilarity OuterInstance;

            internal readonly IDFStats Stats;
            internal readonly float WeightValue;
            internal readonly NumericDocValues Norms;

            internal TFIDFSimScorer(TFIDFSimilarity outerInstance, IDFStats stats, NumericDocValues norms)
            {
                this.OuterInstance = outerInstance;
                this.Stats = stats;
                this.WeightValue = stats.Value;
                this.Norms = norms;
            }

            public override float Score(int doc, float freq)
            {
                float raw = OuterInstance.Tf(freq) * WeightValue; // compute tf(f)*weight

                return Norms == null ? raw : raw * OuterInstance.DecodeNormValue(Norms.Get(doc)); // normalize for field
            }

            public override float ComputeSlopFactor(int distance)
            {
                return OuterInstance.SloppyFreq(distance);
            }

            public override float ComputePayloadFactor(int doc, int start, int end, BytesRef payload)
            {
                return OuterInstance.ScorePayload(doc, start, end, payload);
            }

            public override Explanation Explain(int doc, Explanation freq)
            {
                return OuterInstance.ExplainScore(doc, freq, Stats, Norms);
            }
        }

        /// <summary>
        /// Collection statistics for the TF-IDF model. The only statistic of interest
        /// to this model is idf.
        /// </summary>
        private class IDFStats : SimWeight
        {
            internal readonly string Field;

            /// <summary>
            /// The idf and its explanation </summary>
            internal readonly Explanation Idf;

            internal float QueryNorm;
            internal float QueryWeight;
            internal readonly float QueryBoost;
            internal float Value;

            public IDFStats(string field, Explanation idf, float queryBoost)
            {
                // TODO: Validate?
                this.Field = field;
                this.Idf = idf;
                this.QueryBoost = queryBoost;
                this.QueryWeight = idf.Value * queryBoost; // compute query weight
            }

            public override float ValueForNormalization
            {
                get
                {
                    // TODO: (sorta LUCENE-1907) make non-static class and expose this squaring via a nice method to subclasses?
                    return QueryWeight * QueryWeight; // sum of squared weights
                }
            }

            public override void Normalize(float queryNorm, float topLevelBoost)
            {
                this.QueryNorm = queryNorm * topLevelBoost;
                QueryWeight *= this.QueryNorm; // normalize query weight
                Value = QueryWeight * Idf.Value; // idf for document
            }
        }

        private Explanation ExplainScore(int doc, Explanation freq, IDFStats stats, NumericDocValues norms)
        {
            Explanation result = new Explanation();
            result.Description = "score(doc=" + doc + ",freq=" + freq + "), product of:";

            // explain query weight
            Explanation queryExpl = new Explanation();
            queryExpl.Description = "queryWeight, product of:";

            Explanation boostExpl = new Explanation(stats.QueryBoost, "boost");
            if (stats.QueryBoost != 1.0f)
            {
                queryExpl.AddDetail(boostExpl);
            }
            queryExpl.AddDetail(stats.Idf);

            Explanation queryNormExpl = new Explanation(stats.QueryNorm, "queryNorm");
            queryExpl.AddDetail(queryNormExpl);

            queryExpl.Value = boostExpl.Value * stats.Idf.Value * queryNormExpl.Value;

            result.AddDetail(queryExpl);

            // explain field weight
            Explanation fieldExpl = new Explanation();
            fieldExpl.Description = "fieldWeight in " + doc + ", product of:";

            Explanation tfExplanation = new Explanation();
            tfExplanation.Value = Tf(freq.Value);
            tfExplanation.Description = "tf(freq=" + freq.Value + "), with freq of:";
            tfExplanation.AddDetail(freq);
            fieldExpl.AddDetail(tfExplanation);
            fieldExpl.AddDetail(stats.Idf);

            Explanation fieldNormExpl = new Explanation();
            float fieldNorm = norms != null ? DecodeNormValue(norms.Get(doc)) : 1.0f;
            fieldNormExpl.Value = fieldNorm;
            fieldNormExpl.Description = "fieldNorm(doc=" + doc + ")";
            fieldExpl.AddDetail(fieldNormExpl);

            fieldExpl.Value = tfExplanation.Value * stats.Idf.Value * fieldNormExpl.Value;

            result.AddDetail(fieldExpl);

            // combine them
            result.Value = queryExpl.Value * fieldExpl.Value;

            if (queryExpl.Value == 1.0f)
            {
                return fieldExpl;
            }

            return result;
        }
    }
}