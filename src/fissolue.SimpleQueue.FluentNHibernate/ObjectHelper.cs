using System;
using System.Linq.Expressions;

namespace fissolue.SimpleQueue.FluentNHibernate
{
    internal static class ObjectHelper
    {
        public static string GetPropertyName<T>(Expression<Func<T, object>> exp)
        {
            var body = exp.Body as MemberExpression;

            if (body == null)
            {
                var ubody = (UnaryExpression) exp.Body;
                body = ubody.Operand as MemberExpression;
            }

            return body.Member.Name;
        }
    }
}