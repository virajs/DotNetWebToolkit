﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotNetWebToolkit.Cil2Js.Ast;
using Mono.Cecil;
using DotNetWebToolkit.Cil2Js.Utils;
using DotNetWebToolkit.Attributes;
using DotNetWebToolkit.Cil2Js.Output;
using System.Reflection;

namespace DotNetWebToolkit.Cil2Js.JsResolvers {

    using Cls = DotNetWebToolkit.Cil2Js.JsResolvers.Classes;

    public static partial class JsResolver {

        static JsResolver() {
            var thisModule = ModuleDefinition.ReadModule(Assembly.GetExecutingAssembly().Location);
            Action<TypeDefinition, TypeDefinition> addWithNested = null;
            addWithNested = (bclType, customType) => {
                cMap.Add(bclType, customType);
                var bclNestedTypes = bclType.NestedTypes;
                if (bclNestedTypes.Any()) {
                    var customNestedTypes = customType.Resolve().NestedTypes;
                    foreach (var bclNestedType in bclNestedTypes) {
                        var customNestedType = customNestedTypes.FirstOrDefault(x => x.Name == bclNestedType.Name);
                        if (customNestedType != null) {
                            addWithNested(bclNestedType, customNestedType);
                        }
                    }
                }
            };
            cMap = new Dictionary<TypeDefinition, TypeDefinition>();
            foreach (var m in map) {
                var bclType = thisModule.Import(m.Key).Resolve();
                var customType = thisModule.Import(m.Value).Resolve();
                addWithNested(bclType, customType);
            }
            cMapReverse = cMap.ToDictionary(x => x.Value, x => x.Key);

            //var nested = new List<KeyValuePair<Type, Type>>();
            //Action<Type, Type> addNested = null;
            //addNested = (bclType, customType) => {
            //    var customNesteds = customType.GetNestedTypes();
            //    var bclNesteds = bclType.GetNestedTypes();
            //    foreach (var bclNested in bclNesteds) {
            //        var customNested = customNesteds.FirstOrDefault(x => x.Name == bclNested.Name);
            //        if (customNested != null) {
            //            nested.Add(new KeyValuePair<Type, Type>(bclNested, customNested));
            //            addNested(bclNested, customNested);
            //        }
            //    }
            //};
            //foreach (var m in map) {
            //    addNested(m.Key, m.Value);
            //}
            //foreach (var n in nested) {
            //    map.Add(n.Key, n.Value);
            //}
            //reverseTypeMap = map.ToDictionary(x => x.Value, x => x.Key);
        }

        private static Type T(string typeName) {
            return Type.GetType(typeName);
        }

        //private static Dictionary<Type, Type> reverseTypeMap;

        private static Dictionary<TypeDefinition, TypeDefinition> cMap;
        private static Dictionary<TypeDefinition, TypeDefinition> cMapReverse;

        //private static readonly ModuleDefinition thisModule = ModuleDefinition.ReadModule(Assembly.GetExecutingAssembly().Location);

        private static string JsCase(string s) {
            return char.ToLowerInvariant(s[0]) + s.Substring(1);
        }

        private static bool DoesMatchMethod(MethodReference mInternal, MethodReference m) {
            var detailsAttr = mInternal.Resolve().GetCustomAttribute<JsDetailAttribute>(true);
            if (detailsAttr != null) {
                var signature = detailsAttr.Properties.FirstOrDefault(x => x.Name == "Signature");
                if (signature.Name != null) {
                    if (mInternal.Name != m.Name) {
                        return false;
                    }
                    var sigTypes = ((CustomAttributeArgument[])signature.Argument.Value)
                        .Select(x => ((TypeReference)x.Value).FullResolve(m))
                        .ToArray();
                    var mReturnType = m.ReturnType.FullResolve(m);
                    if (!mReturnType.IsSame(sigTypes[0])) {
                        return false;
                    }
                    for (int i = 0; i < m.Parameters.Count; i++) {
                        var mParameterType = m.Parameters[i].ParameterType.FullResolve(m);
                        if (!mParameterType.IsSame(sigTypes[i + 1])) {
                            return false;
                        }
                    }
                    return true;
                }
            }
            return mInternal.MatchMethodOnly(m);
        }

        //private static MemberInfo FindJsMember(MethodReference mRef, Type type) {
        //    Func<IEnumerable<JsAttribute>, string, bool> isMatch = (attrs, mName) => {
        //        return attrs.Any(a => {
        //            if (a.IsStaticFull != null) {
        //                if (a.IsStaticFull.Value == mRef.HasThis) {
        //                    return false;
        //                }
        //            }
        //            if (a.MethodName == null && a.Parameters == null && mName == mRef.Name) {
        //                return true;
        //            }
        //            if (a.MethodName != null) {
        //                if (a.MethodName != mRef.Name) {
        //                    return false;
        //                }
        //            } else {
        //                if (mName != mRef.Name) {
        //                    return false;
        //                }
        //            }
        //            if (a.Parameters.Count() != mRef.Parameters.Count) {
        //                return false;
        //            }
        //            for (int i = 0; i < mRef.Parameters.Count; i++) {
        //                var parameter = mRef.Parameters[i];
        //                var mRefParameterResolved = parameter.ParameterType.FullResolve(mRef);
        //                var aParameterFullName = GenericParamPlaceholders.ResolveFullName(a.Parameters.ElementAt(i), mRef);
        //                if (aParameterFullName != mRefParameterResolved.FullName) {
        //                    return false;
        //                }
        //            }
        //            var mRefReturnTypeResolved = mRef.ReturnType.FullResolve(mRef);
        //            var aReturnTypeFullName = GenericParamPlaceholders.ResolveFullName(a.ReturnType, mRef);
        //            if (mRefReturnTypeResolved.FullName != aReturnTypeFullName) {
        //                return false;
        //            }
        //            return true;
        //        });
        //    };
        //    var members = type.GetMembers();
        //    var member = members.FirstOrDefault(m => {
        //        var attrs = m.GetCustomAttributes<JsAttribute>();
        //        return isMatch(attrs, m.Name);
        //    });
        //    if (member == null) {
        //        var jss = type.GetCustomAttributes<JsAttribute>();
        //        if (!isMatch(jss, null)) {
        //            throw new InvalidOperationException("Cannot find method: " + mRef.FullName);
        //        }
        //    }
        //    return member;
        //}

        //private static TypeReference ImportType(Type type, TypeReference declaringType) {
        //    var tRef = thisModule.Import(type);
        //    if (tRef.HasGenericParameters) {
        //        var args = ((GenericInstanceType)declaringType).GenericArguments.ToArray();
        //        tRef = tRef.MakeGeneric(args);
        //    }
        //    return tRef;
        //}

        public static string JsName(MemberReference m) {
            var mType = m.MetadataToken.TokenType;
            string name = JsCase(m.Name);
            CustomAttribute jsDetail = null;
            switch (mType) {
            case TokenType.Field:
                jsDetail = ((FieldDefinition)m).GetCustomAttribute<JsDetailAttribute>();
                break;
            case TokenType.Method:
                jsDetail = ((MethodReference)m).Resolve().GetCustomAttribute<JsDetailAttribute>();
                break;
            default:
                throw new NotImplementedException("Cannot handle: " + mType);
            }
            if (jsDetail != null) {
                var nameProp = jsDetail.Properties.FirstOrDefault(x => x.Name == "Name");
                if (nameProp.Name != null) {
                    name = (string)nameProp.Argument.Value;
                }
            }
            return name;
        }

        /// <summary>
        /// If a call/newobj requires translating to an Expr that is not a call/newobj, then it is done here.
        /// </summary>
        /// <param name="call"></param>
        /// <returns></returns>
        public static Expr ResolveCallSite(ICall call) {
            var ctx = call.Ctx;
            var mRef = call.CallMethod;
            var mDef = mRef.Resolve();
            // A call to a method in a "JsClass" class - all external methods/properties require translating to JS
            var tDefDecl = mDef.DeclaringType;
            var jsClassAttr = tDefDecl.GetCustomAttribute<JsClassAttribute>() ?? tDefDecl.GetCustomAttribute<JsAbstractClassAttribute>();
            if (jsClassAttr != null) {
                if (mDef.IsExternal()) {
                    var jsDetail = mDef.GetCustomAttribute<JsDetailAttribute>(true);
                    if (mDef.IsGetter || mDef.IsSetter) {
                        throw new Exception();
                        //var isDomEventProp = 
                        //if (jsDetail != null && 
                    } else if (mDef.IsConstructor && !mDef.IsStatic) {
                        throw new Exception();
                    } else {
                        throw new Exception();
                    }
                } else {
                    return null;
                }
            }
            var jsRedirectAttr = mDef.GetCustomAttribute<JsRedirectAttribute>(true);
            if (jsRedirectAttr != null) {
                var redirectToTRef = ((TypeReference)jsRedirectAttr.ConstructorArguments[0].Value).FullResolve(mRef);
                var redirectToMRef = redirectToTRef.EnumResolvedMethods(mRef).First(x => x.MatchMethodOnly(mRef));
                switch (call.ExprType) {
                case Expr.NodeType.NewObj:
                    return new ExprNewObj(ctx, redirectToMRef, call.Args);
                case Expr.NodeType.Call:
                    return new ExprCall(ctx, redirectToMRef, call.Obj, call.Args, call.IsVirtualCall, null, call.Type);
                default:
                    throw new NotImplementedException("Cannot handle: " + call.ExprType);
                }
                throw new Exception();
                // Must carry result through to FindExprReturn() call
            }
            var expr = FindExprReturn(call);
            if (expr != null) {
                return expr;
            }
            return null;
        }

        /// <summary>
        /// Translate the ctx for transcoding here.
        /// Used to translate methods in the BCL to custom methods.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public static Ctx TranslateCtx(Ctx ctx) {
            var mappedMethod = FindMappedMethod(ctx.MRef);
            if (mappedMethod != null) {
                return new Ctx(mappedMethod.DeclaringType, mappedMethod, ctx);
            }
            return null;
            //var mappedTRef = TypeMap(ctx.TRef);
            //if (mappedTRef == null) {
            //    return null;
            //}
            //var mRef = ctx.MRef;
            //var mappedMethods = mappedTRef.EnumResolvedMethods(mRef);
            //var mappedMethod = mappedMethods.First(x => {
            //    // TODO: include signature checking
            //    var sigAttr = x.Resolve().GetCustomAttribute<JsDetailAttribute>();
            //    if (sigAttr != null) {
            //        var sigArg = sigAttr.Properties.FirstOrDefault(y => y.Name == "Signature");
            //        if (sigArg.Name != null) {
            //            var sigTypes = ((IEnumerable<CustomAttributeArgument>)sigArg.Argument.Value)
            //                .Select(y => ((TypeReference)y.Value).FullResolve(ctx))
            //                .ToArray();
            //            if (mRef.Name != x.Name) {
            //                return false;
            //            }
            //            if (!mRef.ReturnType.FullResolve(ctx).IsSame(sigTypes[0])) {
            //                return false;
            //            }
            //            var sameParams = mRef.Parameters.Zip(sigTypes.Skip(1), (a, b) => a.ParameterType.FullResolve(ctx).IsSame(b)).All(y => y);
            //            if (!sameParams) {
            //                return false;
            //            }
            //            return true;
            //        }
            //    }
            //    return x.MatchMethodOnly(ctx.MRef);
            //});
            //return new Ctx(mappedTRef, mappedMethod);
        }

        private static MethodReference GetRedirect(MethodReference mRef) {
            var mDef = mRef.Resolve();
            var redirectAttr = mDef.GetCustomAttribute<JsRedirectAttribute>();
            if (redirectAttr == null) {
                return null;
            }
            var redirectType = (TypeReference)redirectAttr.ConstructorArguments[0].Value;
            if (redirectType.HasGenericParameters) {
                var genType = new GenericInstanceType(redirectType);
                foreach (var genArg in ((GenericInstanceType)mRef.DeclaringType).GenericArguments) {
                    genType.GenericArguments.Add(genArg);
                }
                redirectType = genType;
            }
            var redirectMethod = redirectType.EnumResolvedMethods(mRef).First(x => x.MatchMethodOnly(mRef));
            return redirectMethod;
        }

        private static Stmt FindStmtReturn(Ctx ctx) {
            var mRef = ctx.MRef;
            var mRefRedirected = GetRedirect(mRef);
            if (mRefRedirected != null) {
                mRef = mRefRedirected;
            }
            var mDef = mRef.Resolve();
            var jsAttr = mDef.GetCustomAttribute<JsAttribute>();
            if (jsAttr != null) {
                var ctorArgs = jsAttr.ConstructorArguments;
                if (ctorArgs.Count == 2) {
                    if (ctorArgs[0].Type.IsType() && ((Array)ctorArgs[1].Value).Length == 0) {
                        // IJsImpl class provides the implementation
                        var iJsImpltype = ((TypeReference)ctorArgs[0].Value).LoadType();
                        var iJsImpl = (IJsImpl)Activator.CreateInstance(iJsImpltype);
                        var stmt = iJsImpl.GetImpl(ctx);
                        return stmt;
                    }
                } else if (ctorArgs.Count == 1) {
                    if (ctorArgs[0].Type.IsString()) {
                        // Explicit JS string provides implementation
                        var js = (string)ctorArgs[0].Value;
                        var parameters = mRef.Parameters.Select((x, i) => ctx.MethodParameter(i, ((char)('a' + i)).ToString())).ToArray();
                        var stmt = new StmtJsExplicit(ctx, js, parameters.Concat(ctx.ThisNamed));
                        return stmt;
                    }
                }
            }
            var mappedType = TypeMap(ctx.TRef);
            if (mappedType != null) {
                var mappedMRef = FindJsMember(mRef, mappedType);
                if (mappedMRef != null) {
                    if (mappedMRef.ReturnType.FullName == typeof(Stmt).FullName) {
                        var m = mappedMRef.LoadMethod();
                        var stmt = (Stmt)m.Invoke(null, new object[] { ctx });
                        return stmt;
                    }
                }
            }
            return null;
        }

        private static Expr FindExprReturn(ICall call) {
            var mRef = call.CallMethod;
            var tRef = mRef.DeclaringType;
            var mappedType = TypeMap(tRef);
            if (mappedType != null) {
                var mappedMRef = FindJsMember(mRef, mappedType);
                if (mappedMRef != null) {
                    if (mappedMRef.ReturnType.FullName == typeof(Expr).FullName) {
                        var m = mappedMRef.LoadMethod();
                        var expr = (Expr)m.Invoke(null, new object[] { call });
                        return expr;
                    }
                }
            }
            return null;
        }

        private static MethodReference FindMappedMethod(MethodReference mRef) {
            var mappedType = TypeMap(mRef.DeclaringType);
            if (mappedType != null) {
                var methods = mappedType.EnumResolvedMethods(mRef);
                var method = methods.FirstOrDefault(x => DoesMatchMethod(x, mRef));// x.MatchMethodOnly(mRef));
                if (method == null) {
                    throw new NotImplementedException("Mapped method not implemented: " + mRef);
                }
                return method;
            }
            return null;
        }

        private static MethodReference FindJsMember(MethodReference mRef, TypeReference mappedTRef) {
            Func<IEnumerable<CustomAttribute>, string, bool> isMatch = (attrs, mName) => {
                return attrs.Any(a => {
                    string aMethodName = null;
                    TypeReference aReturnType = null;
                    IEnumerable<TypeReference> aParameterTypes = null;
                    bool? isStatic = null;
                    var numArgs = a.ConstructorArguments.Count;
                    var ctorArgs = a.ConstructorArguments;
                    if (numArgs == 0 || (numArgs == 1 && ctorArgs[0].Type.IsString())) {
                        // Do nothing
                    } else if (numArgs == 2) {
                        aReturnType = (TypeReference)ctorArgs[0].Value;
                        aParameterTypes = ((IEnumerable<CustomAttributeArgument>)ctorArgs[1].Value).Select(x => (TypeReference)x.Value).ToArray();
                    } else if (numArgs == 3) {
                        aMethodName = (string)ctorArgs[0].Value;
                        aReturnType = (TypeReference)ctorArgs[1].Value;
                        aParameterTypes = ((IEnumerable<CustomAttributeArgument>)ctorArgs[2].Value).Select(x => (TypeReference)x.Value).ToArray();
                    } else {
                        throw new InvalidOperationException("Unrecognised JsAttribute constructor");
                    }
                    if (isStatic != null) {
                        if (isStatic.Value == mRef.HasThis) {
                            return false;
                        }
                    }
                    if (aMethodName == null && aParameterTypes == null && mName == mRef.Name) {
                        return true;
                    }
                    if ((aMethodName ?? mName) != mRef.Name) {
                        return false;
                    }
                    if (aParameterTypes.Count() != mRef.Parameters.Count) {
                        return false;
                    }
                    if (aParameterTypes.Zip(mRef.Parameters, (x, y) => new { aType = x, mRefType = y.ParameterType }).Any(x => {
                        var aTypeResolved = x.aType.FullResolve(mRef);
                        var mRefTypeResolved = x.mRefType.FullResolve(mRef);
                        return !aTypeResolved.IsSame(mRefTypeResolved);
                    })) {
                        return false;
                    }
                    var aReturnTypeResolved = aReturnType.FullResolve(mRef);
                    var mRefReturnTypeResolved = mRef.ReturnType.FullResolve(mRef);
                    if (!aReturnTypeResolved.IsSame(mRefReturnTypeResolved)) {
                        return false;
                    }
                    return true;
                });
            };
            var members = mappedTRef.EnumResolvedMethods(mRef);
            var member = members.FirstOrDefault(m => {
                var mr = m.Resolve();
                var attrs = mr.GetCustomAttributes<JsAttribute>();
                var match = isMatch(attrs, m.Name);
                return match;
            });
            return member;
        }

        /// <summary>
        /// If a method to directly provide an AST for a method is available, then find and use it here.
        /// The incoming ctx will contain the BCL method, not the custom method.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public static Stmt ResolveMethod(Ctx ctx) {
            var stmt = FindStmtReturn(ctx);
            if (stmt != null) {
                return stmt;
            }
            return null;
            //var mappedTRef = TypeMap(ctx.TRef);
            //if (mappedTRef == null) {
            //    return null;//TODO IJsImpl can occur with unmapped type (and possibly other situations)
            //}
            //var jsMember = FindJsMember(ctx.MRef, mappedTRef);
            //if (jsMember == null) {
            //    return null;
            //}
            //if (jsMember.ReturnType.FullName == typeof(Stmt).FullName) {
            //    // Direct AST provided for member, so call method
            //    var type = jsMember.DeclaringType.LoadType();
            //    var stmt = (Stmt)type.InvokeMember(jsMember.Name,
            //        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod,
            //        null, null, new object[] { ctx });
            //    return stmt;
            //}
            //var jsAttr = (jsMember ?? ctx.MDef).Resolve().GetCustomAttribute<JsAttribute>();
            //if (jsAttr != null) {
            //    if (jsAttr.ConstructorArguments.Count == 1) {
            //        var ctorArg0 = jsAttr.ConstructorArguments[0];
            //        if (ctorArg0.Type.IsString()) {
            //            var jsStr = (string)ctorArg0.Value;
            //            var args = Enumerable.Range(0, ctx.MRef.Parameters.Count).Select(i => ctx[i]).Concat(ctx.MRef.HasThis ? ctx.ThisNamed : null).ToArray();
            //            var stmt = new StmtJsExplicit(ctx, jsStr, args);
            //            return stmt;
            //        } else {
            //            var implType = (TypeDefinition)ctorArg0.Value;
            //            var type = implType.LoadType();
            //            var impl = (IJsImpl)Activator.CreateInstance(type);
            //            var stmt = impl.GetImpl(ctx);
            //            return stmt;
            //        }
            //    }
            //}
            //return null;
        }

        //public static Expr ResolveCall(ICall call) {
        //    var ctx = call.Ctx;
        //    var mRef = call.CallMethod;
        //    var mDef = mRef.Resolve();
        //    // A call defined on a class requiring external methods/properties translating to native JS
        //    var type = mDef.DeclaringType;
        //    var jsClass = type.GetCustomAttribute<JsClassAttribute>() ?? type.GetCustomAttribute<JsAbstractClassAttribute>();
        //    if (jsClass != null && mDef.IsExternal()) {
        //        var jsDetail = mDef.GetCustomAttribute<JsDetailAttribute>();
        //        if (mDef.IsSetter || mDef.IsGetter) {
        //            // TODO: Use JsName()
        //            var property = mDef.DeclaringType.Properties.First(x => {
        //                if (x.GetMethod != null && TypeExtensions.MethodRefEqComparerInstance.Equals(x.GetMethod, mDef)) {
        //                    return true;
        //                }
        //                if (x.SetMethod != null && TypeExtensions.MethodRefEqComparerInstance.Equals(x.SetMethod, mDef)) {
        //                    return true;
        //                }
        //                return false;
        //            });
        //            jsDetail = property.GetCustomAttribute<JsDetailAttribute>();
        //            string propertyName = null;
        //            if (jsDetail != null) {
        //                var isDomEventProp = jsDetail.Properties.FirstOrDefault(x => x.Name == "IsDomEvent");
        //                if (isDomEventProp.Name != null && (bool)isDomEventProp.Argument.Value) {
        //                    // Special handling of DOM events
        //                    if (!mDef.Name.StartsWith("set_On")) {
        //                        throw new InvalidOperationException("DOM event name must start with 'On'");
        //                    }
        //                    if (!mDef.IsSetter) {
        //                        throw new InvalidOperationException("Only setters supported on DOM events");
        //                    }
        //                    if (call.Args.Count() != 1) {
        //                        throw new InvalidOperationException("DOM event setter should have one argument");
        //                    }
        //                    var eventName = mDef.Name.Substring(6).ToLowerInvariant();
        //                    var eventNameExpr = ctx.Literal(eventName, ctx.String);
        //                    var safeCall = new ExprCall(ctx, (Action<object, string, Delegate>)InternalFunctions.SafeAddEventListener, null, call.Obj, eventNameExpr, call.Args.First());
        //                    return safeCall;
        //                }
        //                var nameProp = jsDetail.Properties.FirstOrDefault(x => x.Name == "Name");
        //                if (nameProp.Name != null) {
        //                    propertyName = (string)nameProp.Argument.Value;
        //                }
        //            }
        //            if (propertyName == null) {
        //                propertyName = JsCase(mDef.Name.Substring(4));
        //            }
        //            if (mDef.Name.Substring(4) == "Item") {
        //                propertyName = null;
        //            }
        //            if (mDef.IsStatic) {
        //                propertyName = JsCase(mDef.DeclaringType.Name) + "." + propertyName;
        //            }
        //            var jsProperty = new ExprJsResolvedProperty(ctx, call, propertyName);
        //            return jsProperty;
        //        } else if (mDef.IsConstructor) {
        //            string typeName = null;
        //            if (jsDetail != null) {
        //                var nameProp = jsDetail.Properties.FirstOrDefault(x => x.Name == "Name");
        //                if (nameProp.Name != null) {
        //                    typeName = (string)nameProp.Argument.Value;
        //                }
        //            }
        //            if (typeName == null) {
        //                typeName = (string)jsClass.ConstructorArguments[0].Value;
        //            }
        //            var expr = new ExprJsResolvedCtor(ctx, typeName, mRef.DeclaringType, call.Args);
        //            return expr;
        //        } else {
        //            string methodName = JsName(mDef);
        //            if (mDef.IsStatic) {
        //                methodName = JsCase(mDef.DeclaringType.Name) + "." + methodName;
        //            }
        //            return new ExprJsResolvedMethod(ctx, call.Type, call.Obj, methodName, call.Args);
        //        }
        //    }
        //    if (jsClass != null) {
        //        return null;
        //    }
        //    var redirect = mDef.GetCustomAttribute<JsRedirectAttribute>(true);
        //    if (redirect != null) {
        //        var tRedirect = (TypeReference)redirect.ConstructorArguments[0].Value;
        //        if (tRedirect.HasGenericParameters) {
        //            var args = ((GenericInstanceType)mRef.DeclaringType).GenericArguments.ToArray();
        //            tRedirect = tRedirect.MakeGeneric(args);
        //        }
        //        var mRedirect = tRedirect.EnumResolvedMethods(mRef).First(x => x.MatchMethodOnly(mRef));
        //        var expr = new ExprCall(ctx, mRedirect, call.Obj, call.Args, call.IsVirtualCall, null, call.Type);
        //        return expr;
        //    }
        //    var mDeclTypeDef = mRef.DeclaringType.Resolve();
        //    var callType = mDeclTypeDef.LoadType();
        //    if (callType == null) {
        //        // Method is outside this module or its references
        //        return null;
        //    }
        //    var altType = map.ValueOrDefault(callType);
        //    if (altType != null) {
        //        var tRef = thisModule.Import(altType);
        //        if (tRef.HasGenericParameters) {
        //            var args = ((GenericInstanceType)mRef.DeclaringType).GenericArguments.ToArray();
        //            tRef = tRef.MakeGeneric(args);
        //        }
        //        var mappedMethod = tRef.EnumResolvedMethods().FirstOrDefault(x => {
        //            var xResolved = x.FullResolve(mRef.DeclaringType, mRef, true);
        //            var res = xResolved.Resolve();
        //            var visible = res.IsPublic || (res.Name.Contains(".") && !res.IsConstructor); // to handle explicit interface methods
        //            return visible && res.GetCustomAttribute<JsRedirectAttribute>(true) == null && DoesMatchMethod(xResolved, mRef);
        //        });
        //        if (mappedMethod != null) {
        //            Expr expr;
        //            if (call.ExprType == Expr.NodeType.NewObj) {
        //                //if (mDef.IsConstructor) {
        //                expr = new ExprNewObj(ctx, mappedMethod, call.Args);
        //            } else {
        //                mappedMethod = mappedMethod.FullResolve(mRef.DeclaringType, mRef);
        //                expr = new ExprCall(ctx, mappedMethod, call.Obj, call.Args, call.IsVirtualCall);
        //            }
        //            return expr;
        //        }
        //        // Look for methods that generate AST
        //        var method = FindJsMember(call.CallMethod, altType);
        //        if (method != null && method.ReturnType() == typeof(Expr)) {
        //            if (method.DeclaringType.ContainsGenericParameters) {
        //                var tArgs = ((GenericInstanceType)call.CallMethod.DeclaringType).GenericArguments;
        //                var typeArgs = tArgs.Select(x => x.LoadType()).ToArray();
        //                var t = method.DeclaringType.MakeGenericType(typeArgs);
        //                method = t.GetMethods().First(x => x.Name == method.Name && x.ReturnType == typeof(Expr));
        //            }
        //            var expr = (Expr)((MethodInfo)method).Invoke(null, new object[] { call });
        //            return expr;
        //        }
        //    }
        //    return null;
        //}

        //public static Stmt ResolveMethod(Ctx ctx) {
        //    // Attribute for internal function
        //    var jsAttr = ctx.MDef.GetCustomAttribute<JsAttribute>();
        //    if (jsAttr != null) {
        //        var arg0 = jsAttr.ConstructorArguments[0];
        //        switch (arg0.Type.MetadataType) {
        //        case MetadataType.String: {
        //                var js = (string)arg0.Value;
        //                var args = Enumerable.Range(0, ctx.MRef.Parameters.Count).Select(i => ctx[i]).Concat(ctx.MRef.HasThis ? ctx.ThisNamed : null).ToArray();
        //                var stmt = new StmtJsExplicit(ctx, js, args);
        //                return stmt;
        //            }
        //        default: {
        //                var implType = (TypeDefinition)arg0.Value;
        //                var t = typeof(JsResolver).Module.ResolveType(implType.MetadataToken.ToInt32());
        //                var impl = (IJsImpl)Activator.CreateInstance(t);
        //                var stmt = impl.GetImpl(ctx);
        //                return stmt;
        //            }
        //        }
        //    }
        //    // Type map
        //    if (ctx.TDef.Methods.Any(x => x.IsExternal())) {
        //        // Type contains external methods, which cannot be loaded
        //        return null;
        //    }
        //    var typeFullName = ctx.TDef.AssemblyQualifiedName();
        //    var methodType = Type.GetType(typeFullName);
        //    if (methodType == null) {
        //        // Method is outside this module or its references
        //        return null;
        //    }
        //    var altType = map.ValueOrDefault(methodType);
        //    if (altType != null) {
        //        if (altType.IsGenericTypeDefinition) {
        //            var args = ((GenericInstanceType)ctx.TRef).GenericArguments.Select(x => x.LoadType()).ToArray();
        //            altType = altType.MakeGenericType(args);
        //        }
        //        // Look for methods that generate AST
        //        var method = FindJsMember(ctx.MRef, altType);
        //        if (method != null && method.ReturnType() == typeof(Stmt)) {
        //            var stmt = (Stmt)((MethodInfo)method).Invoke(null, new object[] { ctx });
        //            return stmt;
        //        }
        //    }
        //    return null;
        //}

        //public static MethodReference ResolveMethod(MethodReference mRef) {
        //    var tRef = mRef.DeclaringType;
        //    var type = tRef.LoadType();
        //    if (type == null) {
        //        return mRef;
        //    }
        //    var altType = map.ValueOrDefault(type);
        //    if (altType == null) {
        //        return mRef;
        //    }
        //    if (altType.IsGenericTypeDefinition) {
        //        var args = ((GenericInstanceType)tRef).GenericArguments.Select(x => x.LoadType()).ToArray();
        //        altType = altType.MakeGenericType(args);
        //    }
        //    var altTRef = thisModule.Import(altType);
        //    var mappedMethod = altTRef.EnumResolvedMethods().FirstOrDefault(x => {
        //        var xResolved = x.FullResolve(tRef, mRef, true);
        //        var res = xResolved.Resolve();
        //        var visible = res.IsPublic || (res.Name.Contains(".") && !res.IsConstructor); // to handle explicit interface methods
        //        return visible && res.GetCustomAttribute<JsRedirectAttribute>(true) == null && xResolved.MatchMethodOnly(mRef);
        //    });
        //    if (mappedMethod == null) {
        //        return mRef;
        //    }
        //    return mappedMethod;
        //}

        private static TypeReference PerformMapping(Dictionary<TypeDefinition, TypeDefinition> map, TypeReference tRef) {
            if (tRef.IsGenericParameter) {
                return null;
            }
            var tDef = tRef.Resolve();
            var altTDef = map.ValueOrDefault(tDef);
            if (altTDef == null) {
                return null;
            }
            if (altTDef.HasGenericParameters) {
                var tRefGenInst = (GenericInstanceType)tRef;
                var ret = altTDef.MakeGeneric(tRefGenInst.GenericArguments.ToArray());
                return ret;
            }
            return altTDef;
        }

        public static TypeReference TypeMap(TypeReference tRef) {
            return PerformMapping(cMap, tRef);
        }

        public static TypeReference ReverseTypeMap(TypeReference tRef) {
            return PerformMapping(cMapReverse, tRef);
        }

    }
}
