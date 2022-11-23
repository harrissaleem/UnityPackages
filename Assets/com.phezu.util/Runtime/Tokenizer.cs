using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Phezu.Util {
    public class Tokenizer {

        public class Token {
            public string Word { get; private set; }
            public bool IsSpecial { get; private set; }
            public bool AtEndOfLine { get; private set; }

            public Token(string word, bool isSpecial, bool atEndOfLine) {
                Word = word;
                IsSpecial = isSpecial;
                AtEndOfLine = atEndOfLine;
            }
        }

        private List<char> m_SpecialCharacters;
        private char m_Seperator;

        private List<Token> m_LineTokens;
        private StringBuilder m_CurrWord;
        private bool m_IsBuildingToken = false;

        public Tokenizer(char[] specialCharacters, char seperator) {
            if (specialCharacters.Contains(seperator))
                throw new System.Exception("specialCharacters cannot contain seperator");

            m_SpecialCharacters = new(specialCharacters);
            m_Seperator = seperator;
        }

        private void Reset() {
            m_LineTokens = new();
            m_CurrWord = new();
        }

        public List<Token> TokenizeString(string text) {
            if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text))
                return new();

            Reset();

            for (int i = 0; i < text.Length; i++) {
                bool isLastCharacter = i == text.Length - 1;
                ProcessCharacter(text[i], isLastCharacter);
            }
            ProcessCharacter(m_Seperator, true);

            return m_LineTokens;
        }

        private void ProcessCharacter(char character, bool isLastCharacter) {
            if (IsSpecialCharacter(character))
                ProcessSpecialCharacter(character, isLastCharacter);
            else if (ReachedTokenEnd(character))
                ExtractTokenFromCurrWord(isLastCharacter);
            else if (character != m_Seperator)
                AppendCurrWord(character);
        }

        private bool ReachedTokenEnd(char character) {
            bool isSeperator = character == m_Seperator;

            return isSeperator && m_IsBuildingToken;
        }
        private bool IsSpecialCharacter(char character) {
            return m_SpecialCharacters.Contains(character);
        }
        private void AppendCurrWord(char character) {
            m_CurrWord.Append(character);
            m_IsBuildingToken = true;
        }

        private void ExtractTokenFromCurrWord(bool isEndOfLineToken) {
            CreateTokenFromCurrWord(isEndOfLineToken);

            m_IsBuildingToken = false;
            m_CurrWord.Clear();
        }

        private void CreateTokenFromCurrWord(bool isEndOfLineToken) {
            bool isSpecialToken = false;
            m_LineTokens.Add(new(m_CurrWord.ToString(), isSpecialToken, isEndOfLineToken));
        }
        private void ProcessSpecialCharacter(char character, bool isEndOfLineToken) {
            if (m_IsBuildingToken)
                ExtractTokenFromCurrWord(isEndOfLineToken);

            bool isSpecialToken = true;
            m_LineTokens.Add(new(character + "", isSpecialToken, isEndOfLineToken));
        }
    }
}