// namespace cslox
// {
//     public class AstPrinter : Expr.Visitor<string>
//     {
//         public string Print(Expr expr) =>
//             expr.accept(this);

//         public string visitBinaryExpr(Expr.Binary expr) =>
//             Parenthasize(expr.Binop.Lexeme, expr.Left, expr.Right);

//         public string visitGroupingExpr(Expr.Grouping expr) =>
//             Parenthasize("group", expr.Expression);

//         public string visitLiteralExpr(Expr.Literal expr) =>
//             (expr.Value == null) ? "nil" : expr.Value.ToString();

//         public string visitUnaryExpr(Expr.Unary expr) =>
//             Parenthasize(expr.Op.Lexeme, expr.Right);

//         public string visitVariableExpr(Expr.Variable expr) =>
//             Parenthasize("var", expr);

//         private string Parenthasize(string name, params Expr[] exprs)
//         {
//             string str = $"({name}";
//             foreach(var expr in exprs)
//             {
//                 str += " ";
//                 str += expr.accept(this);
//             }
//             str += ")";

//             return str;
//         }
//     }
// }