using bunt.NativeFunctions;

namespace bunt
{
    // C# does not allow <Void> in Generics
    public class Interpreter : Expr.IVisitor<object>, Stmt.IVisitor<object>
    {
        private BuntEnvironment _globals;
        private BuntEnvironment environment;
        private readonly Dictionary<Expr, int> locals;

        public BuntEnvironment globals
        {
            get { return _globals; } 
        }

        public Interpreter() 
        { 
            _globals = new BuntEnvironment();
            environment = _globals;

            locals = new Dictionary<Expr, int>();

            defineNativeFunctions();
        }

        public void defineNativeFunctions()
        {
            IBuntCallable clock = new nf_clock();
            globals.define("clock", clock);
        }

        public void interpret(List<Stmt> statements)
        {
            try
            {
                foreach (Stmt statement in statements)
                {
                    execute(statement);
                }
            } 
            catch (Break b)
            {
                // do nothing. We should exit out of block statement.
            }
            catch (RuntimeError e)
            {
                Bunt.runtimeError(e);
            }
        }

        #region Expressions

        public object visitAssignExpr(Expr.Assign expr)
        {
            object value = evaluate(expr.value);
                            
            int distance;
            if (locals.TryGetValue(expr, out distance))
            {
                environment.assignAt(distance, expr.name, value); // assign the local variable
            }
            else
            {
                globals.assign(expr.name, value); // assign the global variable
            }

            return value;
        }

        public object visitBinaryExpr(Expr.Binary expr)
        {
            object left = evaluate(expr.left);
            object right = evaluate(expr.right);

            switch (expr.oprtor.type)
            {
                case TokenType.GREATER:
                    {
                        checkNumberOperands(expr.oprtor, left, right);
                        return (float)left > (float)right;
                    }
                case TokenType.GREATER_EQUAL:
                    {
                        checkNumberOperands(expr.oprtor, left, right);
                        return (float)left >= (float)right;
                    }
                case TokenType.LESS:
                    {
                        checkNumberOperands(expr.oprtor, left, right);
                        return (float)left < (float)right;
                    }
                case TokenType.LESS_EQUAL:
                    {
                        checkNumberOperands(expr.oprtor, left, right);
                        return (float)left <= (float)right;
                    }
                case TokenType.MINUS:
                    {
                        
                        checkNumberOperands(expr.oprtor, left, right);
                        return (float)left - (float)right;
                    }
                case TokenType.PLUS:
                    {
                        if (left is float && right is float)
                            return (float)left + (float)right;

                        if (left is string && right is string)
                            return (string)left + (string)right;

                        if (left is string)
                        {
                            return (string)left + Convert.ToString(right);
                        }
                        else if (right is string)
                        {
                            return Convert.ToString(left) + (string)right;
                        }

                        throw new RuntimeError(expr.oprtor, "Operands must be two numbers or two strings.");
                    }
                case TokenType.SLASH:
                    {
                        checkNumberOperands(expr.oprtor, left, right);

                        if ((float)right == 0)
                        {
                            throw new RuntimeError(expr.oprtor, "Cannot divide by zero.");
                        }

                        return (float)left / (float)right;
                    }
                case TokenType.STAR:
                    {
                        checkNumberOperands(expr.oprtor, left, right);
                        return (float)left * (float)right;
                    }
                case TokenType.BANG_EQUAL:
                    return !isEqual(left, right);
                case TokenType.EQUAL_EQUAL:
                    return isEqual(left, right);
                
            }

            // unreachable
            return null;
        }

        public object visitCallExpr(Expr.Call expr)
        {
            object callee = evaluate(expr.callee);

            List<object> arguments = new List<object>();
            foreach (Expr argument in expr.arguments)
            {
                arguments.Add(evaluate(argument));
            }
            
            if (!(callee is IBuntCallable))
            {
                throw new RuntimeError(expr.paren, "Can only call functions and classes.");
            }
            

            IBuntCallable function = (IBuntCallable)callee;
            if (arguments.Count != function.arity)
            {
                throw new RuntimeError(expr.paren, "Expected " + function.arity + " arguments but got " + arguments.Count + ".");
            }

            return function.call(this, arguments);
        }

        public object visitGetExpr(Expr.Get expr)
        {
            object obj = evaluate(expr.obj);
            if (obj is BuntInstance)
            {
                return ((BuntInstance)obj).get(expr.name);
            }

            throw new RuntimeError(expr.name, "Only instances have properties.");
        }

        public object visitGroupingExpr(Expr.Grouping expr)
        {
            return evaluate(expr.expression);
        }

        public object visitLiteralExpr(Expr.Literal expr)
        {
            return expr.value;
        }

        /// <summary>
        /// The short circuit here means that we will see the following
        /// 
        /// print "hi" or 2; // "hi"
        /// print nil or "yes" // "yes"
        /// </summary>
        public object visitLogicalExpr(Expr.Logical expr)
        {
            object left = evaluate(expr.left);

            if (expr.oprtor.type == TokenType.OR)
            {
                if (isTruthy(left)) return left;
            } 
            else // AND
            {
                if (!isTruthy(left)) return left;
            }

            return evaluate(expr.right);
        }

        public object visitSetExpr(Expr.Set expr)
        {
            object obj = evaluate(expr.obj);

            if (!(obj is BuntInstance))
            {
                throw new RuntimeError(expr.name, "Only instances have fields.");
            }

            object value = evaluate(expr.value);
            ((BuntInstance)obj).set(expr.name, value);
            return value;
        }

        public object visitSuperExpr(Expr.Super expr)
        {
            int distance = locals[expr];
            BuntClass superclass = (BuntClass)environment.getAt(distance, "super");

            BuntInstance obj = (BuntInstance)environment.getAt(distance - 1, "this");

            BuntFunction method = superclass.findMethod(expr.method.lexeme);

            if (method == null)
            {
                throw new RuntimeError(expr.method, "Undefined property '" + expr.method.lexeme + "'.");
            }

            return method.bind(obj);
        }

        public object visitThisExpr(Expr.This expr)
        {
            return lookupVariable(expr.keyword, expr);
        }

        public object visitUnaryExpr(Expr.Unary expr)
        {
            object right = evaluate(expr.right);

            switch (expr.oprtor.type)
            {
                case TokenType.BANG:
                    return !isTruthy(right);
                case TokenType.MINUS:
                    {
                        checkNumberOperand(expr.oprtor, right);
                        return -(float)right;
                    }
            }

            // unreachable
            return null;
        }

        public object visitVariableExpr(Expr.Variable expr)
        {
            return lookupVariable(expr.name, expr);
        }

        private object lookupVariable(Token name, Expr expr)
        {
            int distance;
            if (locals.TryGetValue(expr, out distance))
            {
                return environment.getAt(distance, name.lexeme); // we only resolved local variables
            }
            else
            {
                return globals.get(name); // global variable
            }
        }

        #endregion

        #region Statements

        public object visitBlockStmt(Stmt.Block stmt)
        {
            executeBlock(stmt.statements, new BuntEnvironment(environment));
            return null;
        }

        public object visitBreakStmt(Stmt.Break stmt)
        {
            // exit out of loop - but only the loop. still want to proceed with other statements
            throw new Break();
        }

        public object visitClassStmt(Stmt.Class stmt)
        {
            object superclass = null;
            if (stmt.superclass != null)
            {
                superclass = evaluate(stmt.superclass);
                if (!(superclass is BuntClass))
                {
                    throw new RuntimeError(stmt.superclass.name, "Superclass must be a class.");
                }
            }

            environment.define(stmt.name.lexeme, null);

            if (stmt.superclass != null)
            {
                environment = new BuntEnvironment(environment);
                environment.define("super", superclass);
            }

            Dictionary<string, BuntFunction> methods = new Dictionary<string, BuntFunction>();
            foreach (Stmt.Function method in stmt.methods)
            {
                BuntFunction function = new BuntFunction(method, environment, method.name.lexeme.Equals("init"));
                methods.Add(method.name.lexeme, function);
            }

            BuntClass klass = new BuntClass(stmt.name.lexeme, (BuntClass)superclass, methods);

            if (superclass != null)
            {
                environment = environment.enclosing;
            }

            environment.assign(stmt.name, klass);
            return null;
        }

        public object visitContinueStmt(Stmt.Continue stmt)
        {
            throw new Continue();
        }

        public object visitExpressionStmt(Stmt.Expression stmt)
        {
            evaluate(stmt.expression);
            return null;
        }

        public object visitFunctionStmt(Stmt.Function stmt)
        {
            // this is the environment that is active when the function is declared not when the function is called.
            BuntFunction function = new BuntFunction(stmt, environment, false);
            environment.define(stmt.name.lexeme, function);
            return null;
        }

        public object visitIfStmt(Stmt.If stmt)
        {
            if (isTruthy(evaluate(stmt.condition)))
            {
                execute(stmt.thenBranch);
            } 
            else if (stmt.elseBranch != null)
            {
                execute(stmt.elseBranch);
            }
            return null;
        }

        public object visitVarStmt(Stmt.Var stmt)
        {
            object value = null;
            if (stmt.initializer != null)
            {
                value = evaluate(stmt.initializer);
            }

            environment.define(stmt.name.lexeme, value);
            return null;
        }

        public object visitPrintStmt(Stmt.Print stmt)
        {
            object value = evaluate(stmt.expression);
            Console.WriteLine(stringify(value));
            return null;
        }

        /// <summary>
        /// If we have a return value, we evaluate it, otherwise we use nil.
        /// </summary>
        /// <exception cref="Return">we throw an exception so that it can unwind the call stack.</exception>
        public object visitReturnStmt(Stmt.Return stmt)
        {
            object value = null;
            if (stmt.value != null) value = evaluate(stmt.value);

            throw new Return(value);
        }

        public object visitWhileStmt(Stmt.While stmt)
        {
            try
            {
                while (isTruthy(evaluate(stmt.condition)))
                {
                    try
                    {
                        execute(stmt.body);
                    }
                    catch (Continue)
                    {
                        // do nothing.
                    }
                }
            } catch (Break)
            {
                // do nothing.
            }

            return null;
        }

        #endregion

        #region helper methods

        object evaluate(Expr expr)
        {
            return expr.accept(this);
        }

        void execute(Stmt stmt)
        {
            stmt.accept(this);
        }

        /// <summary>
        /// Everytime we visit a variable, it tells us how many scopes there are between the current scope
        /// and the scope where the variable is defined.
        /// </summary>
        /// <param name="expr"></param>
        /// <param name="depth"></param>
        public void resolve(Expr expr, int depth)
        {
            if (locals.ContainsKey(expr))
            {
                locals.Remove(expr); // due to limitations in C#
            }

            locals.Add(expr, depth);
        }

        public void executeBlock(List<Stmt> statements, BuntEnvironment environment)
        {
            BuntEnvironment previous = this.environment;
            try
            {
                this.environment = environment;

                // if a 'continue' flag is passed, this needs to throw an error.
                foreach (Stmt statement in statements)
                {
                    execute(statement);
                }
            }             
            finally
            {
                this.environment = previous;
            }
        }

        // false and nil are falsey and everything else is truthy
        bool isTruthy(object obj)
        {
            if (obj == null) return false;
            if (obj is bool) return (bool)obj;
            return true;
        }

        bool isEqual(object a, object b)
        {
            if (a == null && b == null) return true;
            if (a == null) return false;

            return a.Equals(b);
        }

        string stringify(object obj)
        {
            if (obj == null) return "nil";

            if (obj is float)
            {
                string text = obj.ToString();
                if (text.EndsWith(".0"))
                {
                    text = text.Substring(text.Length - 2);
                }
                return text;
            }

            return obj.ToString();
        }


        #region handle runtime errors
        void checkNumberOperand(Token oprtor, object operand)
        {
            if (operand is float) return;
            throw new RuntimeError(oprtor, "Operand must be a number.");
        }

        void checkNumberOperands(Token oprtor, object left, object right)
        {
            if (left is float && right is float) return;

            throw new RuntimeError(oprtor, "Operands must be numbers.");
        }

        #endregion

        #endregion
    }
}
