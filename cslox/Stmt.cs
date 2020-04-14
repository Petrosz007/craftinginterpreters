// Auto generated with Tools/AstGenerator

using System.Collections.Generic;

namespace cslox {
	public abstract class Stmt {
		public interface Visitor<R> {
			R visitBlockStmt(Block stmt);
			R visitExpressionStmt(Expression stmt);
			R visitPrintStmt(Print stmt);
			R visitVarStmt(Var stmt);
		}
		public class Block : Stmt {
			public List<Stmt> Statements { get; }
			public Block(List<Stmt> statements) {
				Statements = statements;
			}

			public override R accept<R>(Visitor<R> visitor) =>
				visitor.visitBlockStmt(this);
			
		}
		public class Expression : Stmt {
			public Expr Expr { get; }
			public Expression(Expr expr) {
				Expr = expr;
			}

			public override R accept<R>(Visitor<R> visitor) =>
				visitor.visitExpressionStmt(this);
			
		}
		public class Print : Stmt {
			public Expr Expr { get; }
			public Print(Expr expr) {
				Expr = expr;
			}

			public override R accept<R>(Visitor<R> visitor) =>
				visitor.visitPrintStmt(this);
			
		}
		public class Var : Stmt {
			public Token Name { get; }
			public Expr Initializer { get; }
			public Var(Token name, Expr initializer) {
				Name = name;
				Initializer = initializer;
			}

			public override R accept<R>(Visitor<R> visitor) =>
				visitor.visitVarStmt(this);
			
		}

		public abstract R accept<R>(Visitor<R> visitor);
	}
}
