namespace bunt
{
    /*
     * Static Analysis for scopes / Semantic Analysis - figure out the meaning
     * 
     * Resolve each variable once. Find every variable mentioned and figure out which declaration each refer to.
     * 
     * {
     *     var a; // 1
     *     var b; // 2
     * }
     * 
     * at the first line, only 'a' is in scope. At the second line both a and b are. 
     * Each var statement splits the block into two separate scopes, the scope before the variable is declared
     * and one after which includes the new variable.
     * 
     * instead of making the data more statically structured, we'll bake the static resolution into the access operation itself.
     */
    internal class Resolver : Expr.IVisitor<object>, Stmt.IVisitor<object>
    {
        readonly Interpreter interpreter;
        readonly Stack<Dictionary<string, bool>> scopes = new Stack<Dictionary<string, bool>>(); // only for local block scopes. not for variables in global scope
        FunctionType currentFunction = FunctionType.NONE;
        ClassType currentClass = ClassType.NONE;
        LoopType currentLoop = LoopType.NONE;

        public Resolver(Interpreter interpreter) 
        { 
            this.interpreter = interpreter;
        }

        enum FunctionType
        {
            NONE,
            FUNCTION,
            INITIALIZER,
            METHOD
        }

        enum ClassType
        {
            NONE,
            CLASS,
            SUBCLASS
        }

        enum LoopType
        {
            NONE,
            LOOP
        }

        public void resolve(List<Stmt> statements)
        {
            foreach (Stmt statement in statements)
            {
                resolve(statement);
            }
        }

        void resolve(Stmt stmt)
        {
            stmt.accept(this);
        }

        void resolve(Expr expr)
        {
            expr.accept(this);
        }

        void beginScope()
        {
            scopes.Push(new Dictionary<string, bool>());
        }

        void endScope()
        {
            scopes.Pop();
        }

        /// <summary>
        /// Declaration adds the variable to the innermost scope so that it shadows any outer one and so that we know the variable exists.
        /// </summary>
        /// <param name="name"></param>
        void declare(Token name)
        {
            if (scopes.Count == 0) return;

            Dictionary<string, bool> scope = scopes.Peek();

            if (scope.ContainsKey(name.lexeme))
            {
                // we allow declaring multiple variables with the same name in the global scope but not in local scope.
                // global scope makes sense, since after all, we are building an interpreter.
                Bunt.error(name, "Already a variable with this name in this scope.");
            }

            scope.Add(name.lexeme, false); // false = have not finished resolving that variable's initializer
        }

        /// <summary>
        /// After declaring the variable, we resolve its initializer expression in the same scope.
        /// </summary>
        /// <param name="name"></param>
        void define(Token name)
        {
            if (scopes.Count == 0) return;
            scopes.Peek().Remove(name.lexeme); // due to C#'s limited functionality of Dictionary.
            scopes.Peek().Add(name.lexeme, true); // finished resolving the variable.
        }

        void resolveLocal(Expr expr, Token name)
        {
            for (int i = scopes.Count - 1; i >= 0; i--)
            {
                /* !!! NOTE !!!
                 * Linq C# stack does very different things than Java stack.
                 * 
                 * In Java stack, every time an element is pushed to the stack, it's on a later index, kind of like a list.
                 * In C# linq stack, every time an element is pushed to the stack, it's on index 0 and everything else gets pushed down.
                 */
                if (scopes.ElementAt(scopes.Count - 1 - i).ContainsKey(name.lexeme)) //in Java, change it from scopes.Count - 1 - i to i.
                {
                    interpreter.resolve(expr, scopes.Count - 1 - i);
                    return;
                }
            }
        }

        /// <summary>
        /// At runtime, declaring a function doesn't do anything with the function's body. It doesn't get touched until
        /// the function gets called.
        /// In static analysis we immediately traverse the body.
        /// </summary>
        void resolveFunction(Stmt.Function function, FunctionType type)
        {
            FunctionType enclosingFunction = currentFunction;
            currentFunction = type;

            beginScope();
            foreach (Token param in function.parameters)
            {
                declare(param);
                define(param);
            }
            resolve(function.body);
            endScope();

            currentFunction = enclosingFunction;
        }

        #region Expressions

        // need to have variables resolved
        public object visitAssignExpr(Expr.Assign expr)
        {
            resolve(expr.value);
            resolveLocal(expr, expr.name);
            return null;
        }

        public object visitBinaryExpr(Expr.Binary expr)
        {
            resolve(expr.left);
            resolve(expr.right);
            return null;
        }

        public object visitCallExpr(Expr.Call expr)
        {
            resolve(expr.callee);

            foreach (Expr argument in expr.arguments)
            {
                resolve(argument);
            }

            return null;
        }

        // since properties are looked up dynamically, they don't get resolved. 
        public object visitGetExpr(Expr.Get expr)
        {
            resolve(expr.obj);
            return null;
        }

        public object visitGroupingExpr(Expr.Grouping expr)
        {
            resolve(expr.expression);
            return null;
        }

        public object visitLiteralExpr(Expr.Literal expr)
        {
            return null;
        }

        public object visitLogicalExpr(Expr.Logical expr)
        {
            resolve(expr.left);
            resolve(expr.right);
            return null;
        }

        // since properties are looked up dynamically, they don't get resolved. 
        public object visitSetExpr(Expr.Set expr)
        {
            resolve(expr.value);
            resolve(expr.obj);
            return null;
        }

        public object visitSuperExpr(Expr.Super expr)
        {
            if (currentClass == ClassType.NONE)
            {
                Bunt.error(expr.keyword, "Can't use 'super' outside of a class.");
            }
            else if (currentClass != ClassType.SUBCLASS)
            {
                Bunt.error(expr.keyword, "Can't use 'super' in a class with no superclass.");
            }

            resolveLocal(expr, expr.keyword);
            return null;
        }

        // we resolve like any other local variable using "this" as the name.
        public object visitThisExpr(Expr.This expr)
        {
            if (currentClass == ClassType.NONE)
            {
                Bunt.error(expr.keyword, "Can't use 'this' outside of a class.");
                return null;
            }

            resolveLocal(expr, expr.keyword);
            return null;
        }

        public object visitUnaryExpr(Expr.Unary expr)
        {
            resolve(expr.right);
            return null;
        }

        // need to have variables resolved
        public object visitVariableExpr(Expr.Variable expr)
        {
            /* This prevents cases like
             * 
             * var a = "outer";
             * {
             *     var a = a;
             * }
             */
            if (scopes.Count != 0)
            {
                bool value;
                bool foundValue = scopes.Peek().TryGetValue(expr.name.lexeme, out value);

                if (foundValue && value == false)
                {
                    Bunt.error(expr.name, "Can't read local variable in its own initializer.");
                }
            }
            
            resolveLocal(expr, expr.name);
            return null;
        }

        #endregion

        #region Statements

        // introduces a new scope for the statements it contains
        public object visitBlockStmt(Stmt.Block stmt)
        {
            beginScope();
            resolve(stmt.statements);
            endScope();
            return null;
        }

        public object visitBreakStmt(Stmt.Break stmt)
        {
            if (currentLoop == LoopType.NONE)
            {
                Bunt.error(stmt.keyword, "Can't break from outside a loop.");
            }

            return null;
        }

        public object visitClassStmt(Stmt.Class stmt)
        {
            ClassType enclosingClass = currentClass;
            currentClass = ClassType.CLASS;

            declare(stmt.name);
            define(stmt.name);

            // this prevents cases like 'class A < A {}'
            if (stmt.superclass != null && stmt.name.lexeme.Equals(stmt.superclass.name.lexeme))
            {
                Bunt.error(stmt.superclass.name, "A class can't inherit from itself.");
            }

            if (stmt.superclass != null)
            {
                currentClass = ClassType.SUBCLASS;
                resolve(stmt.superclass);
            }

            if (stmt.superclass != null)
            {
                beginScope();
                scopes.Peek().Add("super", true);
            }

            beginScope();
            scopes.Peek().Add("this", true); // resolve has a new scope for 'this'. 

            foreach (Stmt.Function method in stmt.methods)
            {
                FunctionType declaration = FunctionType.METHOD;
                if (method.name.lexeme.Equals("init"))
                {
                    declaration = FunctionType.INITIALIZER;
                }

                resolveFunction(method, declaration);
            }

            endScope();

            if (stmt.superclass != null) endScope();

            currentClass = enclosingClass;

            return null;
        }

        public object visitContinueStmt(Stmt.Continue stmt)
        {
            if (currentLoop == LoopType.NONE)
            {
                Bunt.error(stmt.keyword, "Can't continue from outside a loop.");
            }

            return null;
        }

        public object visitExpressionStmt(Stmt.Expression stmt)
        {
            resolve(stmt.expression);
            return null;
        }

        // introduces new scope for its body and binds its parameters in that scope
        //
        // function name is bound in the surrounding scope. We bind the function parameters into the inner scope.
        public object visitFunctionStmt(Stmt.Function stmt)
        {
            declare(stmt.name);
            define(stmt.name);

            resolveFunction(stmt, FunctionType.FUNCTION);
            return null;
        }

        // there is no control flow in variable resolution
        public object visitIfStmt(Stmt.If stmt)
        {
            resolve(stmt.condition);
            resolve(stmt.thenBranch);
            if (stmt.elseBranch != null) resolve(stmt.elseBranch);
            return null;
        }

        public object visitPrintStmt(Stmt.Print stmt)
        {
            resolve(stmt.expression);
            return null;
        }

        public object visitReturnStmt(Stmt.Return stmt)
        {
            if (currentFunction == FunctionType.NONE)
            {
                Bunt.error(stmt.keyword, "Can't return from top-level code.");
            }

            if (stmt.value != null)
            {
                if (currentFunction == FunctionType.INITIALIZER)
                {
                    Bunt.error(stmt.keyword, "Can't return a value from an initializer.");
                }

                resolve(stmt.value);
            }

            return null;
        }
        
        // adds a new variable to the current scope
        public object visitVarStmt(Stmt.Var stmt)
        {
            declare(stmt.name);
            if (stmt.initializer != null)
            {
                resolve(stmt.initializer);
            }
            define(stmt.name);
            return null;
        }

        public object visitWhileStmt(Stmt.While stmt)
        {
            LoopType enclosingLoop = currentLoop;
            currentLoop = LoopType.LOOP;

            // this is a placeholder for 'continue' to exit the while loop.
            // we need to handle the case where 'continue' statement is inside multiple nested if statements.
            // scopes.Peek().Add("while", true); // does this make sense? scopes is not the same as Environment.

            resolve(stmt.condition);
            resolve(stmt.body);

            currentLoop = enclosingLoop;

            return null;
        }

        #endregion

    }
}
