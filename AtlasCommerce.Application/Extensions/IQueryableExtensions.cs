using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.Extensions
{
    public static class IQueryableExtensions
    {
        public static IOrderedQueryable<T> OrderByDynamic<T>(this IQueryable<T> source, string propertyName, bool ascending)
        {
            return ApplyOrder(source, propertyName, ascending, false);
        }

        public static IOrderedQueryable<T> ThenByDynamic<T>(this IOrderedQueryable<T> source, string propertyName, bool ascending)
        {
            return ApplyOrder(source, propertyName, ascending, true);
        }

        private static IOrderedQueryable<T> ApplyOrder<T>(IQueryable<T> source, string propertyName, bool ascending, bool isThenBy)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentException("Property name cannot be null or empty.", nameof(propertyName));

            var parameter = Expression.Parameter(typeof(T), "x");
            Expression property = parameter;

            foreach (var member in propertyName.Split('.'))
            {
                property = Expression.PropertyOrField(property, member);
            }

            var lambda = Expression.Lambda(property, parameter);

            string methodName;

            if (!isThenBy)
                methodName = ascending ? "OrderBy" : "OrderByDescending";
            else
                methodName = ascending ? "ThenBy" : "ThenByDescending";

            var method = typeof(Queryable).GetMethods()
                .First(m => m.Name == methodName
                         && m.IsGenericMethodDefinition
                         && m.GetGenericArguments().Length == 2
                         && m.GetParameters().Length == 2);

            var genericMethod = method.MakeGenericMethod(typeof(T), property.Type);

            var result = (IOrderedQueryable<T>)genericMethod.Invoke(null, new object[] { source, lambda });

            return result;
        }
    }
}
