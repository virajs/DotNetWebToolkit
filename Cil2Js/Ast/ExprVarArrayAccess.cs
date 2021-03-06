﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace DotNetWebToolkit.Cil2Js.Ast {
    public class ExprVarArrayAccess : ExprVar {

        public ExprVarArrayAccess(Ctx ctx, Expr array, Expr index)
            : base(ctx) {
            this.Array = array;
            this.Index = index;
            this.elementType = ((ArrayType)array.Type).ElementType;
        }

        public Expr Array { get; private set; }
        public Expr Index { get; private set; }
        public TypeReference elementType;

        public override Expr.NodeType ExprType {
            get { return NodeType.ArrayAccess; }
        }

        public override TypeReference Type {
            get { return this.elementType; }
        }

    }
}
