﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotNetWebToolkit.Attributes;

#pragma warning disable 0626

namespace DotNetWebToolkit.Web {

    [JsClass]
    public static class Document {

        public static extern HtmlElement GetElementById(string id);

    }

}