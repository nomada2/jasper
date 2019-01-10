using System;
using LamarCompiler.Frames;
using Microsoft.AspNetCore.Mvc;

namespace Jasper.MvcExtender
{
    public class CallActionResultFrame : MethodCall
    {
        public CallActionResultFrame(Type handlerType) : base(handlerType, nameof(IActionResult.ExecuteResultAsync))
        {
        }
    }
}
