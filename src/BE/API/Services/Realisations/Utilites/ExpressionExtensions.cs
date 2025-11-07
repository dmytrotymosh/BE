using System.Linq.Expressions;

namespace API.Services.Realisations.Utilites;

public static class ExpressionExtensions
{
    public static Expression<Func<T, bool>> And<T>(
        this Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        var param = Expression.Parameter(typeof(T));
        var body = Expression.AndAlso(
            Expression.Invoke(expr1, param),
            Expression.Invoke(expr2, param));
        return Expression.Lambda<Func<T, bool>>(body, param);
    }
}
