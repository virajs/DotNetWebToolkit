﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotNetWebToolkit.Cil2Js.Ast;

namespace DotNetWebToolkit.Cil2Js.Analysis {
    public class VisitorReplace : AstRecursiveVisitor {

        public static ICode V(ICode ast, ICode find, ICode replace) {
            var v = new VisitorReplace(find, replace);
            return v.Visit(ast);
        }

        private VisitorReplace(ICode find, ICode replace) {
            this.find = find;
            this.replace = replace;
        }

        private ICode find, replace;

        protected override ICode VisitStmt(Stmt s) {
            if (s == this.find) {
                return this.replace;
            }
            return base.VisitStmt(s);
        }

        protected override ICode VisitExpr(Expr e) {
            if (e == this.find) {
                return this.replace;
            }
            return base.VisitExpr(e);
        }

    }
}
