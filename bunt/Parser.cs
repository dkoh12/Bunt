using System.Data;
using System.Xml.Linq;

namespace bunt
{
    class ParseError : InvalidExpressionException { }

    /*
     * Single Token Lookahead Recursive Descent Parser 
     * 
     * Static Analysis / Syntax Analysis - tell whether it's grammatically correct
     */
    public class Parser
    {
        List<Token> tokens;
        int current = 0;

        public Parser(List<Token> tokens) { 
            this.tokens = tokens;
        }

        public List<Stmt> parse()
        {
            List<Stmt> statements = new List<Stmt>();

            while (!isAtEnd())
            {
                statements.Add(declaration());
            }

            return statements;
        }

        #region Statements

        // declaration -> classDecl | funDecl | varDecl | statement
        Stmt declaration()
        {
            try
            {
                if (match(TokenType.CLASS)) return classDeclaration();
                if (match(TokenType.FUN)) return function("function");
                if (match(TokenType.VAR)) return varDeclaration();
                return statement();
            } catch (ParseError error)
            {
                synchronize();
                return null;
            }
        }

        // classDecl -> "class" IDENTIFIER ( "<" IDENTIFIER )? "{" function* "}"
        Stmt classDeclaration()
        {
            Token name = consume(TokenType.IDENTIFIER, "Expect class name.");

            Expr.Variable superclass = null;
            if (match(TokenType.LESS))
            {
                consume(TokenType.IDENTIFIER, "Expect superclass name.");
                superclass = new Expr.Variable(previous());
            }
            
            consume(TokenType.LEFT_BRACE, "Expect '{' before class body.");

            List<Stmt.Function> methods = new List<Stmt.Function>();
            while (!check(TokenType.RIGHT_BRACE) && !isAtEnd())
            {
                methods.Add(function("method"));
            }

            consume(TokenType.RIGHT_BRACE, "Expect '}' after class body.");

            return new Stmt.Class(name, superclass, methods);
        }

        /// <summary>
        /// funDecl -> "fun" function
        /// function -> IDENTIFIER "(" parameters? ")" block
        /// parameters -> IDENTIFIER ( "," IDENTIFIER )*
        /// </summary>
        /// <param name="kind">what kind of function - regular function or class method</param>
        Stmt.Function function(string kind)
        {
            Token name = consume(TokenType.IDENTIFIER, "Expect " + kind + " name.");
            consume(TokenType.LEFT_PAREN, "Expect '(' after " + kind + " name.");

            List<Token> parameters = new List<Token>();

            if (!check(TokenType.RIGHT_PAREN))
            {
                do
                {
                    if (parameters.Count >= 255)
                    {
                        error(peek(), "Can't have more than 255 parameters.");
                    }

                    parameters.Add(consume(TokenType.IDENTIFIER, "Expect parameter name."));
                } while (match(TokenType.COMMA));
            }

            consume(TokenType.RIGHT_PAREN, "Expect ')' after parameters.");

            consume(TokenType.LEFT_BRACE, "Expect '{' before " + kind + " body.");
            List<Stmt> body = block();
            return new Stmt.Function(name, parameters, body);
        }

        // varDecl -> "var" IDENTIFIER ( "=" expression )?
        Stmt varDeclaration()
        {
            Token name = consume(TokenType.IDENTIFIER, "Expect variable name.");

            Expr initializer = null;
            if (match(TokenType.EQUAL))
            {
                initializer = expression();
            }

            consume(TokenType.SEMICOLON, "Expect ';' after variable declaration.");
            return new Stmt.Var(name, initializer);
        }

        Stmt statement()
        {
            if (match(TokenType.BREAK)) return breakStatement();
            if (match(TokenType.CONTINUE)) return continueStatement();
            if (match(TokenType.FOR)) return forStatement();
            if (match(TokenType.IF)) return ifStatement();
            if (match(TokenType.PRINT)) return printStatement();
            if (match(TokenType.RETURN)) return returnStatement();
            if (match(TokenType.WHILE)) return whileStatement();
            if (match(TokenType.LEFT_BRACE)) return new Stmt.Block(block());

            return expressionStatement();
        }

        Stmt breakStatement()
        {
            Token keyword = previous();
            consume(TokenType.SEMICOLON, "Expect ';' after return value.");
            return new Stmt.Break(keyword);
        }

        Stmt continueStatement()
        {
            Token keyword = previous();
            consume(TokenType.SEMICOLON, "Expect ';' after return value.");
            return new Stmt.Continue(keyword);
        }

        // forStmt -> "for" "(" ( varDecl | exprStmt | ";" ) expression? ";" expression? ")" statement;
        /// <summary>
        /// desugar for loop into a while loop. For loop is basically syntactic sugar that makes it more pleasant and productive to work with.
        /// 
        /// we can convert
        /// 
        /// for (var i = 0; i < 10; i = i + 1) print i;
        /// 
        /// into
        /// 
        /// var i = 0;
        /// while (i < 10) {
        ///     print i;
        ///     i = i + 1;
        /// }
        /// </summary>
        Stmt forStatement()
        {
            consume(TokenType.LEFT_PAREN, "Expect '(' after 'for'.");

            Stmt initializer;
            if (match(TokenType.SEMICOLON))
            {
                initializer = null;
            } 
            else if (match(TokenType.VAR))
            {
                initializer = varDeclaration();
            } 
            else
            {
                initializer = expressionStatement();
            }

            Expr condition = null;
            if (!check(TokenType.SEMICOLON))
            {
                condition = expression();
            }
            consume(TokenType.SEMICOLON, "Expect ';' after loop condition.");

            Expr increment = null;
            if (!check(TokenType.RIGHT_PAREN))
            {
                increment = expression();
            }
            consume(TokenType.RIGHT_PAREN, "Expect ')' after for clauses.");

            Stmt body = statement();

            if (increment != null)
            {
                body = new Stmt.Block(new List<Stmt>(new[] { body, new Stmt.Expression(increment) }));
            }

            if (condition == null) 
            {
                condition = new Expr.Literal(true); // if condition is null, make it an infinite loop
            }
            body = new Stmt.While(condition, body);

            if (initializer != null)
            {
                body = new Stmt.Block(new List<Stmt>(new[] { initializer, body }));
            }

            return body;
        }

        // whileStmt -> "while" "(" expression ")" statement
        Stmt whileStatement()
        {
            consume(TokenType.LEFT_PAREN, "Expect '(' after 'while'.");
            Expr condition = expression();
            consume(TokenType.RIGHT_PAREN, "Expect ')' after condition.");
            Stmt body = statement();

            return new Stmt.While(condition, body);
        }

        // ifStmt -> "if" "(" expression ")" statement ( "else" statement )?
        Stmt ifStatement()
        {
            consume(TokenType.LEFT_PAREN, "Expect '(' after 'if'.");
            Expr condition = expression();
            consume(TokenType.RIGHT_PAREN, "Expect ')' after if condition.");

            Stmt thenBranch = statement();
            Stmt elseBranch = null;
            if (match(TokenType.ELSE))
            {
                elseBranch = statement();
            }

            return new Stmt.If(condition, thenBranch, elseBranch);
        }

        Stmt printStatement()
        {
            Expr value = expression();
            consume(TokenType.SEMICOLON, "Expect ';' after value.");
            return new Stmt.Print(value);
        }

        // returnStmt -> "return" expression?
        Stmt returnStatement()
        {
            Token keyword = previous();
            Expr value = null;

            /*
             * Since Bunt is dynmically typed, there are no true void functions. Every function must return something.
             * Since many different tokens can potentially start an expression, it's hard to tell if a return value is present.
             * Instead we check if it's absent. Since a semicolon can't begin an expression, if the next token is that, we know
             * there must not be a value.
             */
            if (!check(TokenType.SEMICOLON))
            {
                value = expression();
            }

            consume(TokenType.SEMICOLON, "Expect ';' after return value.");
            return new Stmt.Return(keyword, value);
        }

        Stmt expressionStatement()
        {
            Expr expr = expression();
            consume(TokenType.SEMICOLON, "Expect ';' after expression.");
            return new Stmt.Expression(expr);
        }

        List<Stmt> block()
        {
            List<Stmt> statements = new List<Stmt>();

            while (!check(TokenType.RIGHT_BRACE) && !isAtEnd())
            {
                statements.Add(declaration());
            }

            consume(TokenType.RIGHT_BRACE, "Expect '}' after block.");
            return statements;
        }

        #endregion

        #region Expressions

        // expression -> assignment
        private Expr expression()
        {
            return assignment();
        }

        // assignment -> ( call "." )? IDENTIFIER "=" assignment | logic_or
        /// <summary>
        /// '=' can refer to both equality and variable assignment and our single token lookahead recursive descent parser
        /// can't see far enough to tell that it's parsing an assignment until after it has gone through the LHS.
        /// 
        /// The LHS isn't an expression that evaluates to a value.
        /// 
        /// Consider:
        /// var a = "before";
        /// a = "after";
        /// 
        /// On the second line, we don't evaluate "a" (which would return string "before"). We figure out what the variable
        /// 'a' refers to so we know where to store the RHS expression's value.
        /// 
        /// r-values produce expressions.
        /// l-values "evaluates" to a storage location that we can assign into.
        /// 
        /// Since assignment is right-associative, we recursively call assignment() to parse the RHS
        /// 
        /// var a = var b = var c = 5
        /// </summary>
        private Expr assignment()
        {
            Expr expr = or();

            if (match(TokenType.EQUAL))
            {
                Token equals = previous();
                Expr value = assignment();

                if (expr is Expr.Variable)
                {
                    Token name = ((Expr.Variable)expr).name;
                    return new Expr.Assign(name, value);
                }
                else if (expr is Expr.Get) // properties
                {
                    Expr.Get get = (Expr.Get)expr;
                    return new Expr.Set(get.obj, get.name, value);
                }
                else if (expr is Expr.Subscript) // array index
                {

                }

                error(equals, "Invalid assignment target.");
            }

            return expr;
        }

        // logic_or -> logic_and ( "or" logic_and )*
        private Expr or()
        {
            Expr expr = and();

            while (match(TokenType.OR))
            {
                Token oprtor = previous();
                Expr right = and();
                expr = new Expr.Logical(expr, oprtor, right);
            }

            return expr;
        }

        // logic_and -> equality ( "and" equality )*
        private Expr and()
        {
            Expr expr = equality();

            while (match(TokenType.AND))
            {
                Token oprtor = previous();
                Expr right = equality();
                expr = new Expr.Logical(expr, oprtor, right);
            }

            return expr;
        }

        // equality -> comparison (("!=" | "==") comparison)*
        private Expr equality()
        {
            Expr expr = comparison();
            while (match(TokenType.BANG_EQUAL, TokenType.EQUAL_EQUAL))
            {
                Token oprtor = previous();
                Expr right = comparison();
                expr = new Expr.Binary(expr, oprtor, right);
            }

            return expr;
        }

        // comparison -> term ((">" | ">=" | "<" | "<=") term)*
        private Expr comparison()
        {
            Expr expr = term();

            while (match(TokenType.GREATER, TokenType.GREATER_EQUAL, TokenType.LESS, TokenType.LESS_EQUAL))
            {
                Token oprtor = previous();
                Expr right = term();
                expr = new Expr.Binary(expr, oprtor, right);
            }

            return expr;
        }

        // term -> factor(("-"|"+") factor)*
        private Expr term()
        {
            Expr expr = factor();

            while (match(TokenType.MINUS, TokenType.PLUS))
            {
                Token oprtor = previous();
                Expr right = factor();
                expr = new Expr.Binary(expr, oprtor, right);
            }

            return expr;
        }

        // factory -> unary (("/"|"*") unary)*
        private Expr factor()
        {
            Expr expr = unary();

            while (match(TokenType.SLASH, TokenType.STAR))
            {
                Token oprtor = previous();
                Expr right = unary();
                expr = new Expr.Binary(expr, oprtor, right);
            }

            return expr;
        }

        // unary -> ("!"|"-") unary | call
        private Expr unary()
        {
            if (match(TokenType.BANG, TokenType.MINUS))
            {
                Token oprtor = previous();
                Expr right = unary();
                return new Expr.Unary(oprtor, right);
            }

            return call();
        }

        // call -> subscript ( "(" arguments? ")" | "." IDENTIFIER )*
        private Expr call()
        {
            Expr expr = subscript();

            while (true)
            {
                if (match(TokenType.LEFT_PAREN)) // functions
                {
                    expr = finishCall(expr);
                }
                else if (match(TokenType.DOT)) // properties on instances
                {
                    Token name = consume(TokenType.IDENTIFIER, "Expect property name after '.'.");
                    expr = new Expr.Get(expr, name);
                }
                else
                {
                    break;
                }
            }

            return expr;
        }

        // subscript -> primary ( "[" logic_or "]" )*
        private Expr subscript()
        {
            Expr expr = primary();

            while (true)
            {
                if (match(TokenType.LEFT_BRACKET))
                {
                    expr = finishSubscript(expr); // array index.
                }
                else
                {
                    break;
                }
            }

            return expr;
        }

        // arguments -> expression ( "," expression )*
        private Expr finishCall(Expr callee)
        {
            List<Expr> arguments = new List<Expr>();

            if (!check(TokenType.RIGHT_PAREN))
            {
                do
                {
                    /*
                     *  C standard says conforming implementation has to support at least 127 arguments to a function but doesn't say there's an upper limit.
                     *  Java specification says a method can accept no more than 255 arguments.
                     *  For C#, we use the same limit as Java specification for simplicity.
                     *  
                     *  The limit is 254 arguments if the method is an instance method. That's because 'this' works like an argument.
                     */
                    if (arguments.Count >= 255)
                    {
                        error(peek(), "Can't have more than 255 arguments.");
                    }
                    arguments.Add(expression());
                } while (match(TokenType.COMMA));
            }

            Token paren = consume(TokenType.RIGHT_PAREN, "Expect ')' after arguments.");

            return new Expr.Call(callee, paren, arguments);
        }

        private Expr finishSubscript(Expr name)
        {
            Expr index = or();
            consume(TokenType.RIGHT_BRACKET, "Expect ']' after arguments."); // i could return Token paren here for error reporting.
            return new Expr.Subscript(name, index, null);
        }

        // primary -> NUMBER | STRING | "true" | "false" | "nil" | "this" | "(" expression ")" | "[" list "]" | IDENTIFIER | "super" "." IDENTIFIER
        // list -> expression ( "," expression )*
        private Expr primary()
        {
            if (match(TokenType.FALSE)) return new Expr.Literal(false);
            if (match(TokenType.TRUE)) return new Expr.Literal(true);
            if (match(TokenType.NIL)) return new Expr.Literal(null);

            if (match(TokenType.NUMBER, TokenType.STRING))
            {
                return new Expr.Literal(previous().literal);
            }

            if (match(TokenType.SUPER))
            {
                Token keyword = previous();
                consume(TokenType.DOT, "Expect '.' after 'super'.");
                Token method = consume(TokenType.IDENTIFIER, "Expect superclass method name.");
                return new Expr.Super(keyword, method);
            }

            if (match(TokenType.THIS)) return new Expr.This(previous());

            if (match(TokenType.IDENTIFIER))
            {
                return new Expr.Variable(previous());
            }

            if (match(TokenType.LEFT_PAREN))
            {
                Expr expr = expression();
                consume(TokenType.RIGHT_PAREN, "Expect ')' after expression.");
                return new Expr.Grouping(expr);
            }

            if (match(TokenType.LEFT_BRACKET))
            {
                List<Expr> array = new List<Expr>();

                do
                {
                    array.Add(expression());
                } while (match(TokenType.COMMA));

                consume(TokenType.RIGHT_BRACKET, "Expect ']' after elements.");

                return new Expr.List(array);
            }

            throw error(peek(), "Expect expression.");
        }

        #endregion

        #region helper methods

        Token consume(TokenType type, string message)
        {
            if (check(type)) return advance();

            throw error(peek(), message);
        }

        ParseError error(Token token, string message)
        {
            Bunt.error(token, message);
            return new ParseError();
        }

        /*
         * synchronization point for error recovery.
         * 
         * additional syntax errors hiding in the discarded tokens aren't reported but that also means 
         * mistaken cascaded errors that are side effects of initiall error aren't falsely reported either.
         */
        void synchronize()
        {
            advance();
            while (!isAtEnd())
            {
                if (previous().type == TokenType.SEMICOLON) return;

                // synchronize on statements
                // throw error to get out of call frame.
                switch (peek().type)
                {
                    case TokenType.CLASS:
                    case TokenType.FOR:
                    case TokenType.FUN:
                    case TokenType.IF:
                    case TokenType.PRINT:
                    case TokenType.RETURN:
                    case TokenType.VAR:
                    case TokenType.WHILE:
                        return;
                }

                advance();
            }
        }

        bool match(params TokenType[] types)
        {
            foreach (TokenType type in types)
            {
                if (check(type))
                {
                    advance();
                    return true;
                }
            }
            return false;
        }

        bool check(TokenType type)
        {
            if (isAtEnd()) return false;
            return peek().type == type;
        }

        Token advance()
        {
            if (!isAtEnd()) current++;
            return previous();
        }

        bool isAtEnd()
        {
            return peek().type == TokenType.EOF;
        }

        Token peek()
        {
            return tokens[current];
        }

        Token previous()
        {
            return tokens[current - 1];
        }

        #endregion
    }
}
