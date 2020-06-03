// Auto generated with Tools/AstGenerator

using System.Collections.Generic;

namespace cslox {
	public abstract class Stmt {
		public interface Visitor<R> {
			R visitBlockStmt(Block stmt);
			R visitIfStmt(If stmt);
			R visitExpressionStmt(Expression stmt);
			R visitPrintStmt(Print stmt);
			R visitVarStmt(Var stmt);
			R visitWhileStmt(While stmt);
		}
		public class Block : Stmt {
			public List<Stmt> Statements { get; }
			public Block(List<Stmt> statements) {
				Statements = statements;
			}

			public override R accept<R>(Visitor<R> visitor) =>
				visitor.visitBlockStmt(this);
			
		}
		public class If : Stmt {
			public Expr Condition { get; }
			public Stmt ThenBranch { get; }
			public Stmt ElseBranch { get; }
			public If(Expr condition, Stmt thenBranch, Stmt elseBranch) {
				Condition = condition;
				ThenBranch = thenBranch;
				ElseBranch = elseBranch;
			}

			public override R accept<R>(Visitor<R> visitor) =>
				visitor.visitIfStmt(this);
			
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
		public class While : Stmt {
			public Expr Condition { get; }
			public Stmt Body { get; }
			public While(Expr condition, Stmt body) {
				Condition = condition;
				Body = body;
			}

			public override R accept<R>(Visitor<R> visitor) =>
				visitor.visitWhileStmt(this);
			
		}

		public abstract R accept<R>(Visitor<R> visitor);
	}
}
