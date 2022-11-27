using NUnit.Framework;
using UnityEngine;
using Phezu.Util;
using System.Management.Instrumentation;

public class TokenizerTester
{
    [Test]
    public void TokenizerConstruction()
    {
        Tokenizer tokenizer = new(new string[] { ";" }, ' ');
    }

    [Test]
    public void TokenizeNullString() {
        Tokenizer tokenizer = new(new string[] { ";" }, ' ');

        tokenizer.TokenizeString(null);
    }

    [Test]
    public void TokenizeEmptyString() {
        Tokenizer tokenizer = new(new string[] { ";" }, ' ');

        tokenizer.TokenizeString("");
    }

    [TestCase(new string[] { }, ' ', "this;", new string[] {"this;"})]
    [TestCase(new string[] { }, ' ', "this ; ", new string[] {"this", ";"})]
    [TestCase(new string[] { }, ' ', " this ;; ", new string[] {"this", ";;"})]
    [TestCase(new string[] { }, ' ', " th;is is A", new string[] {"th;is", "is", "A"})]

    [TestCase(new string[] { "," }, ' ', " this ;; ", new string[] { "this", ";;" })]
    [TestCase(new string[] { "," }, ' ', " th;is is A", new string[] { "th;is", "is", "A" })]

    [TestCase(new string[] { ";" }, '_', " th;is is A", new string[] { " th;is is A" })]

    [TestCase(new string[] { "add" }, ' ', "add 1 2", new string[] { "add", "1", "2" })]
    [TestCase(new string[] { "add" }, ' ', " add 1 2", new string[] { "add", "1", "2" })]
    [TestCase(new string[] { "add" }, ' ', "1 add 2 ", new string[] { "1", "add", "2" })]
    [TestCase(new string[] { "add" }, ' ', "1add 2 ", new string[] { "1add", "2" })]
    public void RunTokenizerWordTestCases(string[] specialChars, char seperator, string lineToTokenize, string[] outputTokens) {
        Tokenizer tokenizer = new(specialChars, seperator);

        var tokens = tokenizer.TokenizeString(lineToTokenize);

        for (int i = 0; i < tokens.Count; i++)
            Debug.Log("|" + tokens[i].Word + "|");

        Assert.AreEqual(outputTokens.Length, tokens.Count);

        for (int i = 0; i < outputTokens.Length; i++)
            Assert.IsTrue(outputTokens[i].CompareTo(tokens[i].Word) == 0);
    }

    [TestCase(new string[] { "sub" }, ' ', "sub 1 2", new bool[] { true, false, false })]
    [TestCase(new string[] { "sub" }, ' ', "sub1 2", new bool[] { false, false })]
    [TestCase(new string[] { "add" }, ' ', "sub 1 2 add", new bool[] { false, false, false, true })]
    [TestCase(new string[] { "add", "sub" }, ' ', "sub 1 2 add", new bool[] { true, false, false, true })]
    public void RunTokenizerSpecialTokenTestCases(string[] specialChars, char seperator, string lineToTokenize, bool[] isSpecials) {
        Tokenizer tokenizer = new(specialChars, seperator);

        var tokens = tokenizer.TokenizeString(lineToTokenize);

        for (int i = 0; i < tokens.Count; i++)
            Debug.Log("|" + tokens[i].Word + "|" + ": " + tokens[i].IsSpecial);

        Assert.AreEqual(isSpecials.Length, tokens.Count);

        for (int i = 0; i < isSpecials.Length; i++)
            Assert.AreEqual(isSpecials[i], tokens[i].IsSpecial);
    }
}
