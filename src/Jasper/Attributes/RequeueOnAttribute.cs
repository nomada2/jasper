﻿using System;
using Jasper.ErrorHandling;
using Jasper.Runtime.Handlers;
using LamarCodeGeneration;
using LamarCodeGeneration.Util;

namespace Jasper.Attributes
{
    /// <summary>
    ///     Applies an error handling polity to requeue a message if it
    ///     encounters an exception of the designated type
    /// </summary>
    public class RequeueOnAttribute : ModifyHandlerChainAttribute
    {
        private readonly Type _exceptionType;
        private readonly int _attempts;

        public RequeueOnAttribute(Type exceptionType, int attempts = 3)
        {
            _exceptionType = exceptionType;
            _attempts = attempts;
        }

        public override void Modify(HandlerChain chain, GenerationRules rules)
        {
            chain.OnExceptionOfType(_exceptionType)
                .Requeue(_attempts);
        }
    }
}
