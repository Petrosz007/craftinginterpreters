using System.Collection.Generic;

namespace cslox {
	public abstract class Expr {
		public static class Binary : Expr {
			public static readonly Expr Left { get; }
			public static readonly Token Binop { get; }
			public static readonly Expr Right { get; }
			public static Binary(Expr left, Token binop, Expr right) {
				Left = left;
				Binop = binop;
				Right = right;
			}
		}
		public static class Grouping : Expr {
			public static readonly Expr Expression { get; }
			public static Grouping(Expr expression) {
				Expression = expression;
			}
		}
		public static class Literal : Expr {
			public static readonly Object Value { get; }
			public static Literal(Object value) {
				Value = value;
			}
		}
		public static class Unary : Expr {
			public static readonly Token Op { get; }
			public static readonly Expr Right { get; }
			public static Unary(Token op, Expr right) {
				Op = op;
				Right = right;
			}
		}
	}
}
