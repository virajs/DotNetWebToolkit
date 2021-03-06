﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using DotNetWebToolkit.Cil2Js.Utils;

namespace DotNetWebToolkit.Cil2Js.Ast {
    public class ExprVariableAddress : Expr {

        public ExprVariableAddress(Ctx ctx, Expr variable, TypeReference type)
            : base(ctx) {
            //this.Index = index;
            this.Variable = variable;
            this.ElementType = type;
            this.type = type.MakePointer();
        }

        //public int Index { get; private set; }
        public Expr Variable { get; private set; }
        public TypeReference ElementType { get; private set; }
        private TypeReference type;

        public override Expr.NodeType ExprType {
            get { return NodeType.VariableAddress; }
        }

        public override TypeReference Type {
            get { return this.type; }
        }

    }
}
