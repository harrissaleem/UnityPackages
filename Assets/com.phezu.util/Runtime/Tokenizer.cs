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

        private List<string> m_SpecialTokens;
        private char m_Seperator;

        private List<Token> m_LineTokens;
        private StringBuilder m_CurrWord;
        private bool m_IsBuildingToken = false;

        public Tokenizer(string[] specialCharacters, char seperator) {
            if (specialCharacters.Contains(seperator + ""))
                throw new System.Exception("specialCharacters cannot contain seperator");

            m_SpecialTokens = new(specialCharacters);
            m_Seperator = seperator;

            m_LineTokens = new();
            m_CurrWord = new();
        }

        private void ClearData() {
            m_LineTokens.Clear();
            m_CurrWord.Clear();
        }

        public List<Token> TokenizeString(string text) {
            if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text))
                return new();

            ClearData();

            for (int i = 0; i < text.Length; i++) {
                bool isLastCharacter = i == text.Length - 1;
                ProcessCharacter(text[i], isLastCharacter);
            }

            return m_LineTokens;
        }

        private void ProcessCharacter(char character, bool isLastCharacter) {
            if (character != m_Seperator)
                AppendCurrWord(character);
            if (ReachedTokenEnd(character, isLastCharacter))
                ExtractTokenFromCurrWord(isLastCharacter);
        }

        private bool ReachedTokenEnd(char character, bool isLastCharacter) {
            bool isSeperator = character == m_Seperator;

            return (isSeperator && m_IsBuildingToken) || isLastCharacter;
        }

        private bool IsSpecialToken(string tokenWord) {
            return m_SpecialTokens.Contains(tokenWord);
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
            string currWord = m_CurrWord.ToString();
            bool isSpecialToken = IsSpecialToken(currWord);

            m_LineTokens.Add(new(currWord, isSpecialToken, isEndOfLineToken));
        }
    }
}