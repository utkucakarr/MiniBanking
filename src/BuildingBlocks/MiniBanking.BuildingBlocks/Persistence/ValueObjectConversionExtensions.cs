using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MiniBanking.BuildingBlocks.Persistence;

public static class ValueObjectConversionExtensions
{
    /// <summary>
    /// Stores a single-value value object (e.g. NationalId → its string) in one column.
    /// Reading it back calls the value object's PRIVATE constructor, not Create(...): what is in our own
    /// database was valid when it was saved and must not be re-validated (ADR 0005). Otherwise tightening
    /// a rule later would make old rows impossible to load.
    /// </summary>
    /// <example><c>builder.Property(c => c.NationalId).HasValueObjectConversion(id => id.Value);</c></example>
    public static PropertyBuilder<TValueObject> HasValueObjectConversion<TValueObject, TProvider>(
        this PropertyBuilder<TValueObject> property,
        Expression<Func<TValueObject, TProvider>> toProvider) =>
        property.HasConversion(toProvider, FromPrivateConstructor<TValueObject, TProvider>());

    /// <summary>Builds the expression <c>value => new TValueObject(value)</c> for a non-public constructor.</summary>
    private static Expression<Func<TProvider, TValueObject>> FromPrivateConstructor<TValueObject, TProvider>()
    {
        var constructor = typeof(TValueObject).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                [typeof(TProvider)])
            ?? throw new InvalidOperationException(
                $"{typeof(TValueObject).Name} needs a private constructor taking a single {typeof(TProvider).Name}.");

        var value = Expression.Parameter(typeof(TProvider), "value");

        return Expression.Lambda<Func<TProvider, TValueObject>>(Expression.New(constructor, value), value);
    }
}
