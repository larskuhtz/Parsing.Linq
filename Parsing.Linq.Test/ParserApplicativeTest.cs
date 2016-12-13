using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable InconsistentNaming
namespace System.Parsing.Linq.Test
{
    [TestClass]
    public partial class ParserApplicativeTest
    {
        public bool CanParse<T>(Parser<T> parser, string text)
        {
            var result = parser.Parse(text);
            return !result.IsMissing;
        }

        [TestMethod]
        public void Select_PureAppTest()
        {
            var p = Parser.FromChar('a');
            Func<char, char> f = x => x;
            var p1 = Parser.Lift<char, char>(f).App(p);
            var p2 = p.Select(f);

            var r1 = p1.ParseAll("a");
            var r2 = p2.ParseAll("a");
            Assert.AreEqual(r1, r2);
        }

        [TestMethod]
        public void AppTest()
        {
            /* Monadic notation
            var p =
                from t1 in Parser.FromChar('a')
                from t2 in Parser.FromChar('b')
                from t3 in Parser.FromChar('c')
                select Tuple.Create(t1, t2, t3);
            */

            // Applciative notation
            var p = Parser.Lift<char,char,char, Tuple<char,char,char>>(t1 => t2 => t3 => Tuple.Create(t1, t2, t3))
                .App(Parser.FromChar('a'))
                .App(Parser.FromChar('b'))
                .App(Parser.FromChar('c'));

            var r = p.ParseAll("abc");
            Assert.AreEqual('a', r.Item1);
            Assert.AreEqual('b', r.Item2);
            Assert.AreEqual('c', r.Item3);
        }

        [TestMethod]
        public void App_Parse_Many_Zero()
        {
            var parser = Parser.FromChar('a').Many();
            var result = parser.Parse("b");
            Assert.IsTrue(!result.IsMissing);
            Assert.AreEqual(0, result.Value.Count());
        }

        [TestMethod]
        public void App_Parse_Many_Zero2()
        {
            var parser = Parser.FromChar('a').Many();
            var result = parser.Parse("");
            Assert.IsTrue(!result.IsMissing);
            Assert.AreEqual(0, result.Value.Count());
        }

        [TestMethod]
        public void App_Parse_Many_One()
        {
            var parser = Parser.FromChar('a').Many();
            var result = parser.Parse("ab");
            Assert.IsTrue(!result.IsMissing);
            Assert.AreEqual(1, result.Value.Count());
        }

        [TestMethod]
        public void App_Parse_Many()
        {
            var parser = Parser.FromChar('a').Many();
            var result = parser.Parse("aaaaab");
            Assert.IsTrue(!result.IsMissing);
            Assert.AreEqual(5, result.Value.Count());
        }

        private void App_ParseN_(int i, int j, int k)
        {
            var parser = Parser.FromChar('a').ParseN(k);
            var result = parser.Parse(new string('a', i) + new string('b', j));
            Assert.IsTrue(!result.IsMissing);
            Assert.AreEqual(k, result.Value.Count());
        }

        [TestMethod]
        public void App_ParseN0() => App_ParseN_(2, 1, 0);

        [TestMethod]
        public void App_ParseN1() => App_ParseN_(3, 1, 1);

        [TestMethod]
        public void App_ParseN2() => App_ParseN_(4, 1, 2);

        [TestMethod]
        public void App_ParseNTwice()
        {
            var parser = Parser.FromChar('a').ParseN(5).Proj2(Parser.FromChar('a').ParseN(5));
            var result = parser.Parse("aaaaaaaaaa");
            Assert.IsTrue(!result.IsMissing);
            Assert.AreEqual(5, result.Value.Count());
        }

        [TestMethod]
        public void App_Parse_ParseN_Many()
        {
            var parser = Parser.FromChar('a').ParseN(5).Proj2(Parser.FromChar('a').Many());
            var result = parser.Parse("aaaaaaaaaa");
            Assert.IsTrue(!result.IsMissing);
            Assert.AreEqual(5, result.Value.Count());
        }

        [TestMethod]
        public void App_Parse_Many_Many()
        {
            var parser = Parser.FromChar('a').Many().Proj2(Parser.FromChar('a').Many());
            var result = parser.Parse("aaaaaaaaaa");
            Assert.IsTrue(!result.IsMissing);
            Assert.AreEqual(0, result.Value.Count());
        }

        // The Many parser is greedy and doesn't backtrack
        [TestMethod]
        public void App_Parse_Many_NoBacktrack()
        {
            var parser = Parser.FromChar('a').Many().Proj2(Parser.FromChar('a').ParseN(5));
            var result = parser.Parse("aaaaaaaaaa");
            Assert.IsTrue(result.IsMissing);
        }

        [TestMethod]
        public void App_Parse_Until()
        {
            var parser = Parser.FromChar('a')
                .Until(Parser.FromChar('b'))
                .Select(t => t.Item1);
            var result = parser.Parse("ab");
            Assert.IsTrue(! result.IsMissing);
            Assert.AreEqual(1, result.Value.Count());
        }

        [TestMethod]
        public void App_Parse_Until0()
        {
            var parser = Parser.FromChar('a')
                .Until(Parser.FromChar('b'))
                .Select(t => t.Item1);
            var result = parser.Parse("b");
            Assert.IsTrue(! result.IsMissing);
            Assert.AreEqual(0, result.Value.Count());
        }

        [TestMethod]
        public void App_Parse_Until1()
        {
            var parser = Parser.FromChar('a')
                .Until(Parser.FromChar('b'))
                .Select(t => t.Item1);
            var result = parser.Parse("aab");
            Assert.IsTrue(! result.IsMissing);
            Assert.AreEqual(2, result.Value.Count());
        }

        [TestMethod]
        public void App_Parse_Until2()
        {
            var parser = Parser.FromChar('a')
                .Until(Parser.FromChar('b').Some())
                .Select(t => t.Item1.Concat(t.Item2));
            var result = parser.Parse("aaabb");
            Assert.IsTrue(! result.IsMissing);
            Assert.AreEqual(5, result.Value.Count());
        }

        [TestMethod]
        public void App_Parse_Until3()
        {
            var parser = Parser.FromChar('a')
                .Until(Parser.FromChar('a').Some())
                .Select(t => t.Item1.Concat(t.Item2));
            var result = parser.Parse("aaaaa");
            Assert.IsTrue(! result.IsMissing);
            Assert.AreEqual(5, result.Value.Count());
        }

        [TestMethod]
        public void App_Parse_Until4()
        {
            var parser = Parser.FromChar('a')
                .Until(Parser.FromChar('b').ParseN(2))
                .Select(t => t.Item1.Concat(t.Item2));
            var result = parser.Parse("aaabb");
            Assert.IsTrue(! result.IsMissing);
            Assert.AreEqual(5, result.Value.Count());
        }

        [TestMethod]
        public void App_Parse_Until5()
        {
            var parser = Parser.FromChar('a')
                .Until(Parser.FromChar('a').ParseN(2))
                .Select(t => t.Item1.Concat(t.Item2));
            var result = parser.Parse("aaaaa");
            Assert.IsTrue(! result.IsMissing);
            Assert.AreEqual(2, result.Value.Count());
        }

        [TestMethod]
        public void App_Parse_Many_All()
        {
            var parser = Parser.FromChar('a').Many();
            var result = parser.Parse("aaaaaa");
            Assert.IsTrue(!result.IsMissing);
            Assert.AreEqual(6, result.Value.Count());
        }

        [TestMethod]
        public void App_Parse_Some_Zero()
        {
            var parser = Parser.FromChar('a').Some();
            var result = parser.Parse("b");
            Assert.IsTrue(result.IsMissing);
        }

        [TestMethod]
        public void App_Parse_Some_Zero2()
        {
            var parser = Parser.FromChar('a').Some();
            var result = parser.Parse("");
            Assert.IsTrue(result.IsMissing);
        }

        [TestMethod]
        public void App_Parse_Some_One()
        {
            var parser = Parser.FromChar('a').Some();
            var result = parser.Parse("ab");
            Assert.IsTrue(!result.IsMissing);
            Assert.AreEqual(1, result.Value.Count());
        }

        [TestMethod]
        public void App_Parse_Some()
        {
            var parser = Parser.FromChar('a').Some();
            var result = parser.Parse("aaaaab");
            Assert.IsTrue(!result.IsMissing);
            Assert.AreEqual(5, result.Value.Count());
        }

        [TestMethod]
        public void App_Parse_Some_All()
        {
            var parser = Parser.FromChar('a').Some();
            var result = parser.Parse("aaaaaa");
            Assert.IsTrue(!result.IsMissing);
            Assert.AreEqual(6, result.Value.Count());
        }

        [TestMethod]
        public void App_Parse_Between()
        {
            var parser = Parser.FromChar('a');
            var before = Parser.FromChar('b');
            var after = Parser.FromChar('c');
            var result = parser.Between(before,after).Parse("bac");
            Assert.IsTrue(!result.IsMissing);
            Assert.AreEqual('a', result.Value);
        }

        [TestMethod]
        public void App_Parse_BetweenMany()
        {
            var parser = Parser.FromChar('a');
            var before = Parser.FromChar('b').Many();
            var after = Parser.FromChar('c').Many();
            var result = parser.Between(before,after).Parse("bbbbbaccc");
            Assert.IsTrue(!result.IsMissing);
            Assert.AreEqual('a', result.Value);
        }

        [TestMethod]
        public void App_Parse_BetweenMany_Zero()
        {
            var parser = Parser.FromChar('a');
            var before = Parser.FromChar('b').Many();
            var after = Parser.FromChar('c').Many();
            var result = parser.Between(before,after).Parse("a");
            Assert.IsTrue(!result.IsMissing);
            Assert.AreEqual('a', result.Value);
        }

        [TestMethod]
        public void App_Parse_ManyBetweenMany()
        {
            var parser = Parser.FromChar('a').Many();
            var before = Parser.FromChar('b').Many();
            var after = Parser.FromChar('c').Many();
            var result = parser.Between(before,after).Parse("bbbbbaaaaaccc");
            Assert.IsTrue(!result.IsMissing);
            Assert.AreEqual("aaaaa", string.Concat(result.Value));
        }

        [TestMethod]
        public void App_Parse_ManyBetweenMany_Zero()
        {
            var parser = Parser.FromChar('a').Many();
            var before = Parser.FromChar('b').Many();
            var after = Parser.FromChar('c').Many();
            var result = parser.Between(before,after).Parse("bbbbbccc");
            Assert.IsTrue(!result.IsMissing);
            Assert.AreEqual("", string.Concat(result.Value));
        }

        [TestMethod]
        public void App_Parse_SeparatedByN()
        {
            var parser = Parser.FromChar('a');
            var sep = Parser.FromChar(';');
            var result = parser.separatedByN(sep, 6).Parse("a;a;a;a;a;a");
            Assert.IsTrue(!result.IsMissing);
            Assert.AreEqual(6, result.Value.Count());
        }

        [TestMethod]
        public void App_Parse_SeparatedByN1()
        {
            var parser = Parser.FromChar('a');
            var sep = Parser.FromChar(';');
            var result = parser.separatedByN(sep, 1).Parse("a");
            Assert.IsTrue(!result.IsMissing);
            Assert.AreEqual(1, result.Value.Count());
        }

        [TestMethod]
        public void App_Parse_SeparatedByN_()
        {
            var parser = Parser.FromChar('a');
            var sep = Parser.FromChar(';');
            var result = parser.separatedByN(sep, 6).Parse("a;a;a;a;a;a;a;a;a");
            Assert.IsTrue(!result.IsMissing);
            Assert.AreEqual(6, result.Value.Count());
        }

        [TestMethod]
        public void App_Parse_SeparatedByN_0()
        {
            var parser = Parser.FromChar('a');
            var sep = Parser.FromChar(';');
            var result = parser.separatedByN(sep, 0).Parse("a;a;a;a;a;a;a;a;a");
            Assert.IsTrue(!result.IsMissing);
            Assert.AreEqual(0, result.Value.Count());
        }

        [TestMethod]
        public void App_Parse_SeparatedByN_1()
        {
            var parser = Parser.FromChar('a');
            var sep = Parser.FromChar(';');
            var result = parser.separatedByN(sep, 1).Parse("a;a;a;a;a;a;a;a;a");
            Assert.IsTrue(!result.IsMissing);
            Assert.AreEqual(1, result.Value.Count());
        }
    }
}
