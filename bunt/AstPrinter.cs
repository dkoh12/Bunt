using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bunt
{
    /// <summary>
    /// Prints the AST for debugging our parser and interpreter.
    /// Converts the tree into a string
    /// </summary>
    internal class AstPrinter : Expr.IVisitor<string>
    {
        string print(Expr expr)
        {
            return expr.accept(this);
        }

        public string visitAssignExpr(Expr.Assign expr)
        {
            throw new NotImplementedException();
        }

        public string visitBinaryExpr(Expr.Binary expr)
        {
            return parenthesize(expr.oprtor.lexeme, expr.left, expr.right);
        }

        public string visitCallExpr(Expr.Call expr)
        {
            throw new NotImplementedException();
        }

        public string visitGetExpr(Expr.Get expr)
        {
            throw new NotImplementedException();
        }

        public string visitGroupingExpr(Expr.Grouping expr)
        {
            return parenthesize("group", expr.expression);
        }

        public string visitLambdaExpr(Expr.Lambda expr)
        {
            throw new NotImplementedException();
        }

        public string visitListExpr(Expr.List expr)
        {
            throw new NotImplementedException();
        }

        public string visitLiteralExpr(Expr.Literal expr)
        {
            if (expr.value == null) return "nil";
            return expr.value.ToString();
        }

        public string visitLogicalExpr(Expr.Logical expr)
        {
            throw new NotImplementedException();
        }

        public string visitSetExpr(Expr.Set expr)
        {
            throw new NotImplementedException();
        }

        public string visitSubscriptExpr(Expr.Subscript expr)
        {
            throw new NotImplementedException();
        }

        public string visitSuperExpr(Expr.Super expr)
        {
            throw new NotImplementedException();
        }

        public string visitThisExpr(Expr.This expr)
        {
            throw new NotImplementedException();
        }

        public string visitUnaryExpr(Expr.Unary expr)
        {
            return parenthesize(expr.oprtor.lexeme, expr.right);
        }

        public string visitVariableExpr(Expr.Variable expr)
        {
            throw new NotImplementedException();
        }


        #region helper methods

        string parenthesize(string name, params Expr[] exprs)
        {
            StringBuilder s = new StringBuilder();

            s.Append("(").Append(name);
            foreach (Expr expr in exprs)
            {
                s.Append(" ");
                s.Append(expr.accept(this));
            }
            s.Append(")");

            return s.ToString();
        }

        #endregion

    }
}
