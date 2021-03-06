﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetWebToolkit.Attributes;

#pragma warning disable 0626, 0824

namespace DotNetWebToolkit.Web {

    [JsClass("XMLHttpRequest")]
    public class XMLHttpRequest {

        public extern XMLHttpRequest();

        public extern void Abort();
        public extern string GetAllResponseHeaders();
        public extern string GetResponseHeader(string header);
        public extern void Open(string method, string url);
        public extern void Open(string method, string url, bool async);
        public extern void Open(string method, string url, bool async, string user);
        public extern void Open(string method, string url, bool async, string user, string password);
        public extern void OverrideMimeType(string mimeType);
        public extern void Send();
        public extern void Send(ArrayBuffer data);
        //public extern void Send(Blob data);
        public extern void Send(Document data);
        public extern void Send(string data);
        //public extern void Send(FormData data);
        public extern void SetRequestHeader(string header, string value);

        [JsDetail(IsDomEvent = true)]
        public extern Action OnReadyStateChange { set; }
        public extern XMLHttpRequestReadyState ReadyState { get; }
        public extern object Response { get; }
        public extern string ResponseText { get; }
        public extern XMLHttpRequestResponseType ResponseType { get; set; }
        public extern Document ResponseXML { get; }
        public extern ushort Status { get; }
        public extern string StatusText { get; }
        public extern uint Timeout { get; set; }
        //public extern XMLHttpRequestUpload Upload { set; }
        public extern bool WithCredentials { get; set; }

    }

    public enum XMLHttpRequestReadyState {
        Unsent = 0,
        Opened = 1,
        HeadersReceived = 2,
        Loading = 3,
        Done = 4,
    }

    [JsStringEnum]
    public enum XMLHttpRequestResponseType {
        [JsDetail(Name = "")]
        Default,
        [JsDetail(Name = "arraybuffer")]
        ArrayBuffer,
        [JsDetail(Name = "blob")]
        Blob,
        [JsDetail(Name = "document")]
        Document,
        [JsDetail(Name = "json")]
        Json,
        [JsDetail(Name = "text")]
        Text,
    }

    public static class XMLHttpRequestExtensions {

        public static void SendJson(this XMLHttpRequest xmlHttpRequest, object obj) {
            var data = Window.Json.Stringify(XMLHttpRequestHelper.Encode(obj));
            xmlHttpRequest.SetRequestHeader("Content-type", "application/x-www-form-urlencoded");
            xmlHttpRequest.Send(data);
        }

        [JsReturnTypeDeepUse]
        public static T RecvJson<T>(this XMLHttpRequest xmlHttpRequest) {
            return (T)XMLHttpRequestHelper.Decode(Window.Json.Parse(xmlHttpRequest.ResponseText));
        }

        public static void SetTimeout(this XMLHttpRequest xmlHttpRequest, TimeSpan timeout) {
            xmlHttpRequest.Timeout = (uint)timeout.TotalMilliseconds;
        }

        public static TimeSpan GetTimeout(this XMLHttpRequest xmlHttpRequest) {
            return TimeSpan.FromMilliseconds(xmlHttpRequest.Timeout);
        }

    }

    public static class XMLHttpRequestHelper {

        public static object Decode(object json) {
            // Implemented by _JsonHelper override class
            throw new Exception();
        }

        public static object Encode(object obj) {
            // Implemented by _JsonHelper override class
            throw new Exception();
        }

    }


}
