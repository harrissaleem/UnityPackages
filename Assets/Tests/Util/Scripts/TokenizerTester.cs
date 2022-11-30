using NUnit.Framework;
using UnityEngine;
using Phezu.Util;

public class TokenizerTester {
    [Test]
    public void TokenizerConstruction() {
        new Tokenizer(new char[] { ';' }, ' ');
    }

    [Test]
    public void TokenizeNullString() {
        Tokenizer tokenizer = new(new char[] { ';' }, ' ');

        tokenizer.TokenizeString(null);
    }

    [Test]
    public void TokenizeEmptyString() {
        Tokenizer tokenizer = new(new char[] { ';' }, ' ');

        tokenizer.TokenizeString("");
    }

    [TestCase(new char[] { ';' }, ' ', "this;", new string[] { "this", ";" })]
    [TestCase(new char[] { ';' }, ' ', "this ; ", new string[] { "this", ";" })]
    [TestCase(new char[] { ';' }, ' ', " this ;; ", new string[] { "this", ";", ";" })]
    [TestCase(new char[] { ';' }, ' ', " th;is is A", new string[] { "th", ";", "is", "is", "A" })]

    [TestCase(new char[] { ',' }, ' ', " this ;; ", new string[] { "this", ";;" })]
    [TestCase(new char[] { ',' }, ' ', " th;is is A", new string[] { "th;is", "is", "A" })]

    [TestCase(new char[] { ',' }, '_', " th;is is A", new string[] { " th;is is A" })]
    public void RunTokenizerWordTestCases(char[] specialChars, char seperator, string lineToTokenize, string[] outputTokens) {
        Tokenizer tokenizer = new(specialChars, seperator);

        var tokens = tokenizer.TokenizeString(lineToTokenize);

        for (int i = 0; i < tokens.Count; i++)
            Debug.Log("|" + tokens[i].Word + "|");

        Assert.AreEqual(outputTokens.Length, tokens.Count);

        for (int i = 0; i < outputTokens.Length; i++)
            Assert.IsTrue(outputTokens[i].CompareTo(tokens[i].Word) == 0);
    }
}
