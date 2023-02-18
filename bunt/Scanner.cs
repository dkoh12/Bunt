namespace bunt
{
    // lexical analysis
    internal class Scanner
    {
        static Dictionary<String, TokenType> keywords = new Dictionary<string, TokenType>()
        {
            { "and",    TokenType.AND },
            { "class",  TokenType.CLASS },
            { "else",   TokenType.ELSE },
            { "false",  TokenType.FALSE },
            { "for",    TokenType.FOR },
            { "fun",    TokenType.FUN },
            { "if",     TokenType.IF },
            { "nil",    TokenType.NIL },
            { "or",     TokenType.OR },
            { "print",  TokenType.PRINT },
            { "return", TokenType.RETURN },
            { "super",  TokenType.SUPER },
            { "this",   TokenType.THIS },
            { "true",   TokenType.TRUE },
            { "var",    TokenType.VAR },
            { "while",  TokenType.WHILE }
        };

        string source;
        List<Token> tokens = new List<Token>();
        int start = 0;
        int current = 0;
        int line = 1;

        public Scanner(String source) { 
            this.source = source;
        }

        public List<Token> scanTokens()
        {
            while (!isAtEnd())
            {
                start = current;
                scanToken();
            }

            tokens.Add(new Token(TokenType.EOF, "", null, line));
            return tokens;
        }

        void scanToken()
        {
            char c = advance();
            switch (c)
            {
                case '(': addToken(TokenType.LEFT_PAREN); break;
                case ')': addToken(TokenType.RIGHT_PAREN); break;
                case '{': addToken(TokenType.LEFT_BRACE); break;
                case '}': addToken(TokenType.RIGHT_BRACE); break;
                case ',': addToken(TokenType.COMMA); break;
                case '.': addToken(TokenType.DOT); break;
                case '-': addToken(TokenType.MINUS); break;
                case '+': addToken(TokenType.PLUS); break;
                case ';': addToken(TokenType.SEMICOLON); break;
                case '*': addToken(TokenType.STAR); break;

                // two char token
                case '!': addToken(match('=') ? TokenType.BANG_EQUAL : TokenType.BANG); break;
                case '=': addToken(match('=') ? TokenType.EQUAL_EQUAL: TokenType.EQUAL); break;
                case '<': addToken(match('=') ? TokenType.LESS_EQUAL: TokenType.LESS); break;
                case '>': addToken(match('=') ? TokenType.GREATER_EQUAL: TokenType.GREATER); break;
                case '/':
                    if (match('/'))
                    {
                        while (peek() != '\n' && !isAtEnd()) advance();
                    } 
                    else
                    {
                        addToken(TokenType.SLASH);
                    }
                    break;

                // whitespace
                case ' ':
                case '\r':
                case '\t':
                    break;
                case '\n':
                    line++;
                    break;

                // string start
                case '"': stringseq(); break;
                default:
                    if (char.IsDigit(c))
                    {
                        number();
                    }
                    else if (isAlpha(c))
                    {
                        identifier();
                    }
                    else
                    {
                        // throw error
                    }
                    break;

            }
        }

        #region helper methods

        void identifier()
        {
            while (char.IsLetterOrDigit(peek())) advance(); // todo this doesn't include '_'

            string text = Substring(start, current);
            TokenType type;
            bool found = keywords.TryGetValue(text, out type);

            if (!found) type = TokenType.IDENTIFIER;

            addToken(type);
        }

        void number()
        {
            while (char.IsDigit(peek())) advance();

            // look for fractional part
            if (peek() == '.' && char.IsDigit(peekNext()))
            {
                // consume the "."
                advance();

                while (char.IsDigit(peek())) advance();
            }

            addToken(TokenType.NUMBER, float.Parse(Substring(start, current)));
        }

        // 'string' is a reserved keyword in C#
        void stringseq()
        {
            while (peek() != '"' && !isAtEnd())
            {
                if (peek() == '\n') line++;
                advance();
            }

            if (isAtEnd())
            {
                // error
                return;
            }

            // the closing '"'
            advance();

            // trim the quotes
            string value = Substring(start + 1, current - 1);
            addToken(TokenType.STRING, value);
        }

        bool isAlpha(char c)
        {
            return char.IsLetter(c) || c == '_';
        }

        // return if current token matches expected value and if so, advances pointer
        bool match(char expected)
        {
            if (isAtEnd()) return false;
            if (source[current] != expected) return false;

            current++;
            return true;
        }

        // return the current char without advancing the pointer
        char peek()
        {
            if (isAtEnd()) return '\0';
            return source[current];
        }

        // returns next char without advancing the pointer
        // used for identifying big numbers, fractional parts, strings
        char peekNext()
        {
            if (current + 1 >= source.Length) return '\0';
            return source[current+ 1];
        }

        // return current character and advance the pointer
        char advance()
        {
            return source[current++];
        }

        bool isAtEnd()
        {
            return current >= source.Length;
        }

        void addToken(TokenType type)
        {
            addToken(type, null);
        }

        void addToken(TokenType type, object literal)
        {
            string text = Substring(start, current);
            tokens.Add(new Token(type, text, literal, line));
        }

        string Substring(int start, int end)
        {
            int length = end - start;
            return source.Substring(start, length);
        }

        #endregion
    }
}
