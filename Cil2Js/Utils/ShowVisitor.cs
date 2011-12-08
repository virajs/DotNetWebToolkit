﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cil2Js.Analysis;
using Mono.Cecil;
using Cil2Js.Ast;

namespace Cil2Js.Utils {
    public class ShowVisitor : AstVisitor {

        private static string GetStmtName(Stmt s) {
            if (s.StmtType == Stmt.NodeType.Cil) {
                return string.Format("IL_{0:x4}", ((StmtCil)s).Insts.First().Offset);
            } else {
                return string.Format("{0:x8}", s.GetHashCode());
            }
        }

        public static string V(MethodDefinition method, ICode c) {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0} {1}({2})",
                method.ReturnType.FullName, method.Name,
                string.Join(", ", method.Parameters.Select(x => string.Format("{0} {1}", x.ParameterType.FullName, x.Name))));
            var seen = new HashSet<ICode>() { c };
            var todo = new Queue<Stmt>();
            todo.Enqueue((Stmt)c);
            while (todo.Any()) {
                var cBlock = todo.Dequeue();
                var v = new ShowVisitor(method);
                v.Visit(cBlock);
                sb.AppendLine();
                sb.AppendLine();
                sb.Append(GetStmtName(cBlock) + ":");
                sb.Append(v.Code);
                foreach (var continuation in v.Continuations) {
                    if (seen.Add(continuation.To)) {
                        todo.Enqueue(continuation.To);
                    }
                }
            }

            sb.AppendLine();
            sb.Append("}");
            return sb.ToString();
        }

        private ShowVisitor(MethodDefinition method)
            : base(true) {
        }

        private StringBuilder code = new StringBuilder();
        private int indent = 1;
        private List<StmtContinuation> continuations = new List<StmtContinuation>();

        public string Code { get { return this.code.ToString(); } }
        public IEnumerable<StmtContinuation> Continuations { get { return this.continuations; } }


        private void NewLine() {
            this.code.Append(Environment.NewLine);
            this.code.Append(' ', this.indent * 4);
        }

        protected override ICode VisitCil(StmtCil s) {
            this.NewLine();
            this.code.Append("CIL: " + s.ToString());
            this.Visit(s.EndCil);
            return s;
        }

        protected override ICode VisitBlock(StmtBlock s) {
            foreach (var stmt in s.Statements) {
                this.Visit(stmt);
            }
            return s;
        }

        protected override ICode VisitIf(StmtIf s) {
            this.NewLine();
            this.code.Append("if (");
            this.Visit(s.Condition);
            this.code.Append(") {");
            this.indent++;
            this.Visit(s.Then);
            this.indent--;
            if (s.Else != null) {
                this.NewLine();
                this.code.Append("} else {");
                this.indent++;
                this.Visit(s.Else);
                this.indent--;
            }
            this.NewLine();
            this.code.Append("}");
            return s;
        }

        protected override ICode VisitContinuation(StmtContinuation s) {
            this.NewLine();
            this.code.AppendFormat("-> {0}", GetStmtName(s.To));
            this.continuations.Add(s);
            return s;
        }

        private static Dictionary<UnaryOp, string> unaryOps = new Dictionary<UnaryOp, string> {
            { UnaryOp.Not, "!" },
            { UnaryOp.Negate, "-" },
        };
        protected override ICode VisitUnary(ExprUnary e) {
            this.code.Append("(");
            this.code.Append(unaryOps[e.Op]);
            this.Visit(e.Expr);
            this.code.Append(")");
            return e;
        }

        protected override ICode VisitReturn(StmtReturn s) {
            this.NewLine();
            this.code.Append("return");
            if (s.Expr != null) {
                this.code.Append(" ");
                this.Visit(s.Expr);
            }
            return s;
        }

        private static Dictionary<BinaryOp, string> binaryOps = new Dictionary<BinaryOp, string> {
            { BinaryOp.Add, "+" },
            { BinaryOp.Sub, "-" },
            { BinaryOp.Mul, "*" },
            { BinaryOp.Div, "/" },
            { BinaryOp.And, "&&" },
            { BinaryOp.Equal, "==" },
            { BinaryOp.NotEqual, "!=" },
            { BinaryOp.LessThan, "<" },
            { BinaryOp.LessThanOrEqual, "<=" },
            { BinaryOp.GreaterThan, ">" },
            { BinaryOp.GreaterThanOrEqual, ">=" },
            { BinaryOp.Or, "||" },
        };
        protected override ICode VisitBinary(ExprBinary e) {
            this.code.Append("(");
            this.Visit(e.Left);
            this.code.AppendFormat(" {0} ", binaryOps[e.Op]);
            this.Visit(e.Right);
            this.code.Append(")");
            return e;
        }

        protected override ICode VisitTernary(ExprTernary e) {
            this.code.Append("(");
            this.Visit(e.Condition);
            this.code.Append(" ? ");
            this.Visit(e.IfTrue);
            this.code.Append(" : ");
            this.Visit(e.IfFalse);
            this.code.Append(")");
            return e;
        }

        protected override ICode VisitVarInstResult(ExprVarInstResult e) {
            this.code.Append(e.ToString());
            return e;
        }

        protected override ICode VisitLiteral(ExprLiteral e) {
            this.code.Append(e.Value);
            return e;
        }

        protected override ICode VisitAssignment(StmtAssignment s) {
            this.NewLine();
            this.Visit(s.Target);
            this.code.Append(" = ");
            this.Visit(s.Expr);
            return s;
        }

        protected override ICode VisitVarLocal(ExprVarLocal e) {
            this.code.Append(e.ToString());
            return e;
        }

        protected override ICode VisitVarParameter(ExprVarParameter e) {
            this.code.Append(e.ToString());
            return e;
        }

        private HashSet<ExprVarPhi> phiSeen = null;
        protected override ICode VisitVarPhi(ExprVarPhi e) {
            bool finalise = false;
            if (this.phiSeen == null) {
                this.phiSeen = new HashSet<ExprVarPhi>();
                finalise = true;
            }
            this.code.AppendFormat("phi<{0}>(", e.GetHashCode());
            if (this.phiSeen.Add(e)) {
                foreach (var v in e.Exprs) {
                    this.Visit(v);
                    this.code.Append(",");
                }
                this.code.Length--;
            } else {
                this.code.AppendFormat("<rec-{0}>", e.GetHashCode());
            }
            this.code.Append(")");
            if (finalise) {
                this.phiSeen = null;
            }
            return e;
        }

        protected override ICode VisitDoLoop(StmtDoLoop s) {
            this.NewLine();
            this.code.Append("do {");
            this.indent++;
            this.Visit(s.Body);
            this.indent--;
            this.NewLine();
            this.code.Append("} while (");
            this.Visit(s.While);
            this.code.Append(")");
            return s;
        }

    }
}