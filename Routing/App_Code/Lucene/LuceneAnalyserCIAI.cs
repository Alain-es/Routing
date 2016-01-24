using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;


// Taken from: https://our.umbraco.org/forum/developers/extending-umbraco/16396-Examine-and-accents-for-portuguese-language

namespace Routing.LuceneAnalyser
{
    public class LuceneAnalyserCIAI : Analyzer
    {
        public override TokenStream TokenStream(string fieldName, System.IO.TextReader reader)
        {
            StandardTokenizer tokenizer = new StandardTokenizer(Lucene.Net.Util.Version.LUCENE_29, reader);
            tokenizer.SetMaxTokenLength(255);
            TokenStream stream = new StandardFilter(tokenizer);
            stream = new LowerCaseFilter(stream);
            return new ASCIIFoldingFilter(stream);
        }
    }
}