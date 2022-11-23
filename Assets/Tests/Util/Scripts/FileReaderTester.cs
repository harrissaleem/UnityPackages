using System;
using System.IO;
using NUnit.Framework;
using Phezu.Util;

public class FileReaderTester
{
    [Test]
    public void FileReaderConstruction()
    {
         new FileReader();
    }

    [Test]
    public void FileReaderConstructionWithNullFilePath() {
        Assert.Throws<ArgumentNullException>(() => new FileReader(null));
    }

    [Test]
    public void FileReaderConstructionWithEmptyFilePath() {
        Assert.Throws<ArgumentException>(() => new FileReader(""));
    }

    [Test]
    public void FileReaderConstructionWithInvalidFilePath() {
        Assert.Throws<FileNotFoundException>(() => new FileReader("2@ "));
    }

    [Test]
    public void OpenFile() {
        FileReader fileReader = new();
        fileReader.OpenFile("Assets/Tests/Util/TestData/TestCase1.txt");
    }

    [Test]
    public void ReadEmptyFile() {
        FileReader fileReader = new();
        fileReader.OpenFile("Assets/Tests/Util/TestData/TestCase1.txt");

        Assert.IsFalse(fileReader.HasNextLine());
    }

    [Test]
    public void ReadSingleLineFile() {
        FileReader fileReader = new();
        fileReader.OpenFile("Assets/Tests/Util/TestData/TestCase2.txt");

        Assert.IsTrue(fileReader.HasNextLine());
        Assert.IsTrue(fileReader.GetCurrentLine().CompareTo("1 2") == 0);

        fileReader.NextLine();

        Assert.IsFalse(fileReader.HasNextLine());
    }

    [Test]
    public void FileReaderResets() {
        FileReader fileReader = new();
        fileReader.OpenFile("Assets/Tests/Util/TestData/TestCase2.txt");

        fileReader.ResetLineIndex();

        Assert.IsTrue(fileReader.HasNextLine());
        Assert.IsTrue(fileReader.GetCurrentLine().CompareTo("1 2") == 0);
        fileReader.NextLine();
        Assert.IsFalse(fileReader.HasNextLine());

        fileReader.ResetLineIndex();

        Assert.IsTrue(fileReader.HasNextLine());
        Assert.IsTrue(fileReader.GetCurrentLine().CompareTo("1 2") == 0);
        fileReader.NextLine();
        Assert.IsFalse(fileReader.HasNextLine());
    }
}
