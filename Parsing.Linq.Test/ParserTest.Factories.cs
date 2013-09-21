﻿using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable InconsistentNaming
namespace System.Parsing.Linq.Test
{
    public partial class ParserTest
    {
        [TestMethod]
        public void FromRegexTest()
        {
            var parser = Parser.FromRegex("[abc]");
            var result = parser.Parse("cat");

            Assert.IsFalse(result.IsMissing);
            Assert.AreEqual("c", result.Value);
            Assert.AreEqual(0, result.Position);
            Assert.AreEqual(1, result.Length);
        }

        [TestMethod]
        public void OffsetTest()
        {
            var parser = Parser.FromRegex("word");
            var line = @"word abcdefgh";

            var result = parser.Parse(line, 5);
            Assert.IsTrue(result.IsMissing);
        }

        [TestMethod]
        public void SequencingTest()
        {
            var parser =
                from hello in Parser.FromText("Hello")
                from space in CharParsers.WhiteSpace
                from world in Parser.FromText("world")
                from period in Parser.FromChar('.')
                select hello + world;

            Assert.IsTrue(CanParse(parser, "Hello world."));
        }

        [TestMethod]
        public void SequencingRegexTest()
        {
            var parser =
                from hello in Parser.FromRegex("Hello")
                from space in CharParsers.WhiteSpace
                from world in Parser.FromText("world")
                from period in Parser.FromChar('.')
                select hello + world;

            Assert.IsTrue(CanParse(parser, "Hello world."));
        }

        [TestMethod]
        public void SequencingCorrectOffsetIsSetTest()
        {
            var parser =
                from a in Parser.FromRegex("aaa")
                from b in Parser.FromText("bbb")
                select a + b;

            Assert.IsTrue(CanParse(parser, "aaabbb...suffix"));
            Assert.IsFalse(CanParse(parser, "123bbbaaa"));
        }
    }
}
