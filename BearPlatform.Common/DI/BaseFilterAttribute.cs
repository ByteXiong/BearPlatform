using System;
using Castle.DynamicProxy;

namespace BearPlatform.Common.DI;

public abstract class BaseFilterAttribute : Attribute, IFilter
{
    public abstract void OnActionExecuted(IInvocation invocation);
    public abstract void OnActionExecuting(IInvocation invocation);
}
