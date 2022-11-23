using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Phezu.Util {

    public class FileReader {
        private List<string> m_Lines;
        private int m_LineIndex;

        public string[] Lines => m_Lines.ToArray();

        public FileReader() {
            Initialize();
        }

        public FileReader(string filePath) {
            Initialize();
            OpenFile(filePath);
        }

        private void Initialize() {
            m_Lines = new();
        }

        public void Clear() {
            m_Lines = new();
            m_LineIndex = 0;
        }

        public bool HasNextLine() {
            return m_LineIndex < m_Lines.Count;
        }

        public void NextLine() {
            if (HasNextLine())
                m_LineIndex++;
        }

        public string GetCurrentLine() {
            if (m_LineIndex < m_Lines.Count)
                return m_Lines[m_LineIndex];
            else
                return null;
        }

        public void ResetLineIndex() {
            m_LineIndex = 0;
        }

        public void OpenFile(string filePath) {
            ReadFile(GetFileReader(filePath));
        }

        private StreamReader GetFileReader(string filePath) {
            try {
                StreamReader fileReader = new(filePath);
                return fileReader;
            }
            catch (Exception e) {
                throw e;
            }
        }

        private void ReadFile(StreamReader fileReader) {
            string line;
            int counter = 0;

            while ((line = fileReader.ReadLine()) != null) {
                m_Lines.Add(new(line));
                counter++;
            }

            fileReader.Close();
        }
    }
}