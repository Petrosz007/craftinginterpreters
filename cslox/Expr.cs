// Auto generated with Tools/AstGenerator

using System.Collections.Generic;

namespace cslox {
	public abstract class Expr {
		public interface Visitor<R> {
			R visitAssignExpr(Assign expr);
			R visitBinaryExpr(Binary expr);
			R visitGroupingExpr(Grouping expr);
			R visitLiteralExpr(Literal expr);
			R visitUnaryExpr(Unary expr);
			R visitVariableExpr(Variable expr);
		}
		public class Assign : Expr {
			public Token Name { get; }
			public Expr Value { get; }
			public Assign(Token name, Expr value) {
				Name = name;
				Value = value;
			}

			public override R accept<R>(Visitor<R> visitor) =>
				visitor.visitAssignExpr(this);
			
		}
		public class Binary : Expr {
			public Expr Left { get; }
			public Token Binop { get; }
			public Expr Right { get; }
			public Binary(Expr left, Token binop, Expr right) {
				Left = left;
				Binop = binop;
				Right = right;
			}

			public override R accept<R>(Visitor<R> visitor) =>
				visitor.visitBinaryExpr(this);
			
		}
		public class Grouping : Expr {
			public Expr Expression { get; }
			public Grouping(Expr expression) {
				Expression = expression;
			}

			public override R accept<R>(Visitor<R> visitor) =>
				visitor.visitGroupingExpr(this);
			
		}
		public class Literal : Expr {
			public object Value { get; }
			public Literal(object value) {
				Value = value;
			}

			public override R accept<R>(Visitor<R> visitor) =>
				visitor.visitLiteralExpr(this);
			
		}
		public class Unary : Expr {
			public Token Op { get; }
			public Expr Right { get; }
			public Unary(Token op, Expr right) {
				Op = op;
				Right = right;
			}

			public override R accept<R>(Visitor<R> visitor) =>
				visitor.visitUnaryExpr(this);
			
		}
		public class Variable : Expr {
			public Token Name { get; }
			public Variable(Token name) {
				Name = name;
			}

			public override R accept<R>(Visitor<R> visitor) =>
				visitor.visitVariableExpr(this);
			
		}

		public abstract R accept<R>(Visitor<R> visitor);
	}
}
