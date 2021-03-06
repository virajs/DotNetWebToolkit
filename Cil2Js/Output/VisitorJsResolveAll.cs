﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using DotNetWebToolkit.Cil2Js.Analysis;
using DotNetWebToolkit.Cil2Js.Ast;
using DotNetWebToolkit.Cil2Js.JsResolvers;
using DotNetWebToolkit.Cil2Js.Utils;
using Mono.Cecil;

namespace DotNetWebToolkit.Cil2Js.Output {

    public class VisitorJsResolveAll : JsAstVisitor {

        public static ICode V(ICode ast) {
            var v = new VisitorJsResolveAll();
            return v.Visit(ast);
        }


        protected override ICode VisitCall(ExprCall e) {
            var expr = this.HandleCall(e, (obj, args) => new ExprCall(e.Ctx, e.CallMethod, obj, args, e.IsVirtualCall, e.ConstrainedType, e.Type));
            var res = JsResolver.ResolveCallSite(expr);
            if (res != null) {
                return this.Visit(res);
            }
            if (expr.ConstrainedType != null) {
                if (expr.ConstrainedType.IsValueType) {
                    // Map constrained virtual call to a method on a value-type, to a non-virtual call.
                    // This is important as it prevents having to box the value-type, which is expensive
                    var impl = expr.ConstrainedType.EnumResolvedMethods().FirstOrDefault(x => x.MatchMethodOnly(expr.CallMethod));
                    if (impl != null) {
                        var constrainedCall = new ExprCall(expr.Ctx, impl, expr.Obj, expr.Args, false, null, expr.Type);
                        return constrainedCall;
                    } else {
                        throw new Exception();
                    }
                }
            }
            if (expr.IsVirtualCall) {
                var ctx = expr.Ctx;
                var objIsVar = expr.Obj.IsVar();
                var temp = objIsVar ? null : new ExprVarLocal(ctx, expr.Obj.Type);
                var getTypeObj = objIsVar ? expr.Obj : new ExprAssignment(ctx, temp, expr.Obj);
                var getType = new ExprCall(ctx, typeof(object).GetMethod("GetType"), getTypeObj);
                var eJsVCall = new ExprJsVirtualCall(ctx, expr.CallMethod, getType, temp ?? expr.Obj, expr.Args);
                return eJsVCall;
            }
            return expr;
        }

        protected override ICode VisitNewObj(ExprNewObj e) {
            var expr = this.HandleCall(e, (obj, args) => new ExprNewObj(e.Ctx, e.CallMethod, args));
            var res = JsResolver.ResolveCallSite(expr);
            if (res == null) {
                return expr;
            } else {
                return this.Visit(res);
            }
        }

        protected override ICode VisitNewArray(ExprNewArray e) {
            var ctx = e.Ctx;
            MethodInfo caGenDef;
            if (e.ElementType.IsValueType) {
                caGenDef = ((Func<int, int[]>)InternalFunctions.CreateArrayValueType<int>).Method.GetGenericMethodDefinition();
            } else {
                caGenDef = ((Func<int, object[]>)InternalFunctions.CreateArrayRefType<object>).Method.GetGenericMethodDefinition();
            }
            var mCreateArray = ctx.Module.Import(caGenDef).MakeGeneric(e.ElementType);
            var expr = new ExprCall(ctx, mCreateArray, null, e.ExprNumElements);
            return expr;
        }

        protected override ICode VisitBinary(ExprBinary e) {
            var ctx = e.Ctx;
            // Special cases for == and != needed as an empty string is false, not true
            if (e.Op == BinaryOp.Equal) {
                if (e.Left.IsLiteralNull()) {
                    if (e.Right.IsVar() && e.Right.Type.IsValueType && !e.Right.Type.IsNullable()) {
                        return ctx.Literal(false); // Non-nullable Value-type can never be null
                    } else {
                        return new ExprJsExplicit(ctx, "(e == null)", ctx.Boolean, e.Right.Named("e"));
                    }
                }
                if (e.Right.IsLiteralNull()) {
                    if (e.Left.IsVar() && e.Left.Type.IsValueType && !e.Left.Type.IsNullable()) {
                        return ctx.Literal(false); // Non-nullable Value-type can never be null
                    } else {
                        return new ExprJsExplicit(ctx, "(e == null)", ctx.Boolean, e.Left.Named("e"));
                    }
                }
            }
            if (e.Op == BinaryOp.NotEqual || e.Op == BinaryOp.GreaterThan_Un) {
                if (e.Left.IsLiteralNull()) {
                    if (e.Right.IsVar() && e.Right.Type.IsValueType && !e.Right.Type.IsNullable()) {
                        return ctx.Literal(true); // Non-nullable Value-type can never be null
                    } else {
                        return new ExprJsExplicit(ctx, "(e != null)", ctx.Boolean, e.Right.Named("e"));
                    }
                }
                if (e.Right.IsLiteralNull()) {
                    if (e.Left.IsVar() && e.Left.Type.IsValueType && !e.Left.Type.IsNullable()) {
                        return ctx.Literal(true); // Non-nullable Value-type can never be null
                    } else {
                        return new ExprJsExplicit(ctx, "(e != null)", ctx.Boolean, e.Left.Named("e"));
                    }
                }
            }
            // Special case for eq and neq with unsigned integer types.
            // If either side of the eq/neq is unsigned, then ensure that both sides are unsigned
            if (e.Op == BinaryOp.Equal || e.Op == BinaryOp.NotEqual) {
                if (e.Left.Type.IsUnsignedInteger()) {
                    if (e.Right.Type.IsSignedInteger()) {
                        return new ExprBinary(ctx, e.Op, e.Type, e.Left, new ExprConv(ctx, e.Right, e.Left.Type, false));
                    }
                } else if (e.Right.Type.IsUnsignedInteger()) {
                    if (e.Left.Type.IsSignedInteger()) {
                        return new ExprBinary(ctx, e.Op, e.Type, new ExprConv(ctx, e.Left, e.Right.Type, false), e.Right);
                    }
                }
            }
            // Special case for unsigned <, <=, >, >=
            // Force conversions to unsigned if either side is signed
            if (e.Op == BinaryOp.LessThan_Un || e.Op == BinaryOp.LessThanOrEqual_Un ||
                e.Op == BinaryOp.GreaterThan_Un || e.Op == BinaryOp.GreaterThanOrEqual_Un) {
                Expr newLeft = null, newRight = null;
                if (e.Left.Type.IsSignedInteger()) {
                    newLeft = new ExprConv(ctx, e.Left, e.Left.Type.UnsignedEquivilent(ctx.TypeSystem), false);
                }
                if (e.Right.Type.IsSignedInteger()) {
                    newRight = new ExprConv(ctx, e.Right, e.Right.Type.UnsignedEquivilent(ctx.TypeSystem), false);
                }
                if (newLeft != null || newRight != null) {
                    return new ExprBinary(ctx, e.Op, e.Type, newLeft ?? e.Left, newRight ?? e.Right);
                }
            }
            return base.VisitBinary(e);
        }

        protected override ICode VisitBox(ExprBox e) {
            var ctx = e.Ctx;
            var expr = (Expr)this.Visit(e.Expr);
            if (e.Type.IsUnsignedInteger() && expr.Type.IsSignedInteger()) {
                expr = new ExprConv(ctx, expr, e.Type, false);
            }
            if (!e.Type.IsValueType) {
                // For ref-types 'box' does nothing
                return this.Visit(e.Expr);
            }
            var eType = new ExprJsTypeVarName(ctx, e.Type);
            if (e.Type.IsNullable()) {
                var exprIsVar = expr.IsVar();
                var innerType = e.Type.GetNullableInnerType();
                var temp = exprIsVar ? null : new ExprVarLocal(ctx, e.Type);
                var box = new ExprBox(ctx, temp ?? expr, innerType).Named("box");
                var nullableJs = exprIsVar ? "(expr !== null ? box : null)" : "((temp = expr) !== null ? box : null)";
                var nullableExpr = new ExprJsExplicit(ctx, nullableJs, innerType, temp.Named("temp"), expr.Named("expr"), box);
                return nullableExpr;
            } else {
                var deepCopyExpr = InternalFunctions.ValueTypeDeepCopyIfRequired(e.Type, () => (Expr)this.Visit(expr));
                var int2bool = e.Type.IsBoolean() && expr.Type.IsInteger() ? "!!" : "";
                var js = "{v:" + int2bool + "expr,_:type}";
                var ret = new ExprJsExplicit(ctx, js, e.Type, (deepCopyExpr ?? expr).Named("expr"), eType.Named("type"));
                return ret;
            }
        }

        protected override ICode VisitUnboxAny(ExprUnboxAny e) {
            if (!e.Type.IsValueType) {
                // On ref-type, unbox-any becomes a castclass
                var expr = (Expr)this.Visit(e.Expr);
                var cast = new ExprCast(e.Ctx, expr, e.Type);
                return cast;
            }
            var ctx = e.Ctx;
            if (e.Type.IsNullable()) {
                // If obj==null create Nullable with hasValue=false
                // If obj.Type not assignable to e.InnerType throw InvalidCastEx
                var innerType = e.Type.GetNullableInnerType();
                var unboxMethod = ((Func<object, int?>)InternalFunctions.UnboxAnyNullable<int>).Method.GetGenericMethodDefinition();
                var mUnbox = ctx.Module.Import(unboxMethod).MakeGeneric(innerType);
                var unboxAnyCall = new ExprCall(ctx, mUnbox, null, e.Expr);
                return unboxAnyCall;
            } else {
                // If obj==null throw NullRefEx
                // If obj.Type not assignable to e.Type throw InvalidCastEx
                // otherwise unbox
                var unboxMethod = ((Func<object, int>)InternalFunctions.UnboxAnyNonNullable<int>).Method.GetGenericMethodDefinition();
                var mUnbox = ctx.Module.Import(unboxMethod).MakeGeneric(e.Type);
                var unboxAnyCall = new ExprCall(ctx, mUnbox, null, e.Expr);
                return unboxAnyCall;
            }
        }

        protected override ICode VisitCast(ExprCast e) {
            var ctx = e.Ctx;
            var mCast = ctx.Module.Import(((Func<object, Type, object>)InternalFunctions.Cast).Method);
            var eType = new ExprJsTypeVarName(ctx, e.Type);
            var expr = new ExprCall(ctx, e.Type, mCast, null, e.Expr, eType);
            return expr;
        }

        protected override ICode VisitIsInst(ExprIsInst e) {
            var ctx = e.Ctx;
            var mIsInst = ctx.Module.Import(((Func<object, Type, object>)InternalFunctions.IsInst).Method);
            var eType = new ExprJsTypeVarName(ctx, e.Type);
            var expr = new ExprCall(ctx, e.Type, mIsInst, null, e.Expr, eType);
            return expr;
        }

        protected override ICode VisitAssignment(StmtAssignment s) {
            var ctx = s.Ctx;
            var expr = (Expr)this.Visit(s.Expr);
            var target = (ExprVar)this.Visit(s.Target);
            if (target.Type.IsBoolean() && expr.Type.IsInteger()) {
                expr = new ExprJsExplicit(ctx, "!!expr", ctx.Boolean, expr.Named("expr"));
            } else if (target.Type.IsUnsignedInteger() && expr.Type.IsSignedInteger()) {
                expr = new ExprConv(ctx, expr, expr.Type.UnsignedEquivilent(ctx.TypeSystem), false);
            }
            if (expr != s.Expr || target != s.Target) {
                return new StmtAssignment(ctx, target, expr);
            } else {
                return s;
            }
        }

        protected override ICode VisitAssignment(ExprAssignment e) {
            var ctx = e.Ctx;
            var expr = (Expr)this.Visit(e.Expr);
            var target = (ExprVar)this.Visit(e.Target);
            if (target.Type.IsBoolean() && expr.Type.IsInteger()) {
                expr = new ExprJsExplicit(ctx, "!!expr", ctx.Boolean, expr.Named("expr"));
            } else if (target.Type.IsUnsignedInteger() && expr.Type.IsSignedInteger()) {
                expr = new ExprConv(ctx, expr, expr.Type.UnsignedEquivilent(ctx.TypeSystem), false);
            }
            if (expr != e.Expr || target != e.Target) {
                return new ExprAssignment(ctx, target, expr);
            } else {
                return e;
            }
        }

        protected override ICode VisitReturn(StmtReturn s) {
            var ctx = s.Ctx;
            var expr = (Expr)this.Visit(s.Expr);
            if (expr != null && expr.Type.IsInteger() && ctx.MRef.ReturnType.FullResolve(ctx).IsBoolean()) {
                expr = new ExprJsExplicit(ctx, "!!expr", ctx.Boolean, expr.Named("expr"));
            }
            if (expr != s.Expr) {
                return new StmtReturn(ctx, expr);
            } else {
                return s;
            }
        }

        protected override ICode VisitVariableAddress(ExprVariableAddress e) {
            if (e.Type.IsBoolean(true) && !e.Variable.Type.IsBoolean()) {
                var ctx = e.Ctx;
                return new ExprJsExplicit(ctx, "!!expr", ctx.Boolean, e.Variable.Named("expr")); // HACK
            }
            return base.VisitVariableAddress(e);
        }

    }
}
