﻿// this file is autogenerated using T4
// https://learn.microsoft.com/en-us/visualstudio/modeling/design-time-code-generation-by-using-t4-text-templates?source=recommendations&view=vs-2022&tabs=csharp
// https://stackoverflow.com/questions/42353536/how-to-generate-a-simple-class-with-t4

namespace bunt
{
	public abstract class Expr
	{
		public interface IVisitor<T> {
		T visitAssignExpr(Assign expr);
		T visitBinaryExpr(Binary expr);
		T visitCallExpr(Call expr);
		T visitGetExpr(Get expr);
		T visitGroupingExpr(Grouping expr);
		T visitListExpr(List expr);
		T visitLiteralExpr(Literal expr);
		T visitLogicalExpr(Logical expr);
		T visitSubscriptExpr(Subscript expr);
		T visitSetExpr(Set expr);
		T visitSuperExpr(Super expr);
		T visitThisExpr(This expr);
		T visitUnaryExpr(Unary expr);
		T visitVariableExpr(Variable expr);
		}

		public class Assign : Expr
		{
			public readonly Token name;
			public readonly Expr value;

			public Assign(Token name, Expr value) {
				this.name = name;
				this.value = value;
			}

			public override T accept<T>(IVisitor<T> visitor) {
				return visitor.visitAssignExpr(this);
			}
		}
		public class Binary : Expr
		{
			public readonly Expr left;
			public readonly Token oprtor;
			public readonly Expr right;

			public Binary(Expr left, Token oprtor, Expr right) {
				this.left = left;
				this.oprtor = oprtor;
				this.right = right;
			}

			public override T accept<T>(IVisitor<T> visitor) {
				return visitor.visitBinaryExpr(this);
			}
		}
		public class Call : Expr
		{
			public readonly Expr callee;
			public readonly Token paren;
			public readonly List<Expr> arguments;

			public Call(Expr callee, Token paren, List<Expr> arguments) {
				this.callee = callee;
				this.paren = paren;
				this.arguments = arguments;
			}

			public override T accept<T>(IVisitor<T> visitor) {
				return visitor.visitCallExpr(this);
			}
		}
		public class Get : Expr
		{
			public readonly Expr obj;
			public readonly Token name;

			public Get(Expr obj, Token name) {
				this.obj = obj;
				this.name = name;
			}

			public override T accept<T>(IVisitor<T> visitor) {
				return visitor.visitGetExpr(this);
			}
		}
		public class Grouping : Expr
		{
			public readonly Expr expression;

			public Grouping(Expr expression) {
				this.expression = expression;
			}

			public override T accept<T>(IVisitor<T> visitor) {
				return visitor.visitGroupingExpr(this);
			}
		}
		public class List : Expr
		{
			public readonly List<Expr> values;

			public List(List<Expr> values) {
				this.values = values;
			}

			public override T accept<T>(IVisitor<T> visitor) {
				return visitor.visitListExpr(this);
			}
		}
		public class Literal : Expr
		{
			public readonly Object value;

			public Literal(Object value) {
				this.value = value;
			}

			public override T accept<T>(IVisitor<T> visitor) {
				return visitor.visitLiteralExpr(this);
			}
		}
		public class Logical : Expr
		{
			public readonly Expr left;
			public readonly Token oprtor;
			public readonly Expr right;

			public Logical(Expr left, Token oprtor, Expr right) {
				this.left = left;
				this.oprtor = oprtor;
				this.right = right;
			}

			public override T accept<T>(IVisitor<T> visitor) {
				return visitor.visitLogicalExpr(this);
			}
		}
		public class Subscript : Expr
		{
			public readonly Expr obj;
			public readonly Expr index;
			public readonly Expr value;

			public Subscript(Expr obj, Expr index, Expr value) {
				this.obj = obj;
				this.index = index;
				this.value = value;
			}

			public override T accept<T>(IVisitor<T> visitor) {
				return visitor.visitSubscriptExpr(this);
			}
		}
		public class Set : Expr
		{
			public readonly Expr obj;
			public readonly Token name;
			public readonly Expr value;

			public Set(Expr obj, Token name, Expr value) {
				this.obj = obj;
				this.name = name;
				this.value = value;
			}

			public override T accept<T>(IVisitor<T> visitor) {
				return visitor.visitSetExpr(this);
			}
		}
		public class Super : Expr
		{
			public readonly Token keyword;
			public readonly Token method;

			public Super(Token keyword, Token method) {
				this.keyword = keyword;
				this.method = method;
			}

			public override T accept<T>(IVisitor<T> visitor) {
				return visitor.visitSuperExpr(this);
			}
		}
		public class This : Expr
		{
			public readonly Token keyword;

			public This(Token keyword) {
				this.keyword = keyword;
			}

			public override T accept<T>(IVisitor<T> visitor) {
				return visitor.visitThisExpr(this);
			}
		}
		public class Unary : Expr
		{
			public readonly Token oprtor;
			public readonly Expr right;

			public Unary(Token oprtor, Expr right) {
				this.oprtor = oprtor;
				this.right = right;
			}

			public override T accept<T>(IVisitor<T> visitor) {
				return visitor.visitUnaryExpr(this);
			}
		}
		public class Variable : Expr
		{
			public readonly Token name;

			public Variable(Token name) {
				this.name = name;
			}

			public override T accept<T>(IVisitor<T> visitor) {
				return visitor.visitVariableExpr(this);
			}
		}

		public abstract T accept<T>(IVisitor<T> visitor);

	}
}