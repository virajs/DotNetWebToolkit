﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Mono.Cecil;
using DotNetWebToolkit.Cil2Js.Analysis;
using DotNetWebToolkit.Cil2Js.Ast;
using DotNetWebToolkit.Cil2Js.Utils;
using DotNetWebToolkit.Cil2Js.Output;
using FsCheck.Fluent;
using FsCheck;
using Test.ExecutionTests;
using System.Linq.Expressions;
using Test.Utils;
using DotNetWebToolkit;
using DotNetWebToolkit.Attributes;
using DotNetWebToolkit.Web;
using DotNetWebToolkit.Cil2Js;
using Test.BrowserTests;
using Test.BrowserTests.Canvas;
using Test.BrowserTests.TypedArrays;
using Test.BrowserTests.Window_;
using DotNetWebToolkit.Cil2Js.JsResolvers.Classes;

namespace Test {

    class Program {

        static void RunAllTests<T>(bool verbose = false) where T : new() {
            dynamic t = new T();
            t.Verbose = verbose;
            var methods = typeof(T)
                .GetMethods()
                .Where(x => x.GetCustomAttributes(false).Any(y => y.GetType().Name == "TestAttribute"))
                .ToArray();
            foreach (var method in methods) {
                var call = Expression.Call(Expression.Constant(t), method);
                var fn = Expression.Lambda<Action>(call).Compile();
                Console.WriteLine("Calling: {0}();", method.Name);
                fn();
            }
        }

        public static void Canvas2DDemo() {
            var canvas = (CanvasElement)Window.Document.GetElementById("canvasId");
            var ctx = canvas.GetContext2D();
            string fill1 = "#ff0000";
            string fill2 = "#0000ff";
            bool f1 = true;
            Window.SetInterval(() => {
                ctx.FillStyle = f1 ? fill1 : fill2;
                f1 = !f1;
                ctx.FillRect(50, 50, 100, 50);
            }, 5000);
        }

        public static int Factorial(int i) {
            return (i == 0) ? 1 : i * Factorial(i - 1);
        }

        public static int Fibonacci(int i) {
            return (i <= 1) ? 1 : Fibonacci(i - 1) + Fibonacci(i - 2);
        }

        public static int FactorialOrFibonacci(int i, bool factorial) {
            if (factorial) {
                return Factorial(i);
            } else {
                return Fibonacci(i);
            }
        }

        public static void WS() {
            bool open = false;
            var ws = new WebSocket("testing");
            ws.OnOpen = () => {
                open = true;
            };
            ws.OnMessage = m => {
                if (open) {
                    Window.Alert(m.Data);
                    ws.Send("Testing ws");
                }
            };
        }

        static void Main(string[] args) {
            //var js = Transcoder.ToJs(typeof(Program).Assembly.Location, true);
            //Console.WriteLine(js);

            //var mi = typeof(Program).GetMethod("WS");
            //var js = Transcoder.ToJs(mi, true);
            //Console.WriteLine(js);

            var t = new TestEqualityIEquatable() { Verbose = true };
            t.TestBooleanEqualsObject();

            return;

        }

    }

    [JsExport]
    public class Exported {
        private Exported() { }
        public static int Get1() {
            var a = new int[1];
            a[0] = 8;
            return a.Length;
        }
    }

}
