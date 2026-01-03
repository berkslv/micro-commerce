using BuildingBlocks.Common.Domain;
using BuildingBlocks.Common.Exceptions;

namespace Catalog.Domain.Entities;

/// <summary>
/// Category entity for organizing products.
/// </summary>
public sealed class Category : BaseAuditableEntity
{
    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    private readonly List<Product> _products = new();

    public IReadOnlyList<Product> Products
    {
        get
        {
            return _products.AsReadOnly();
        }
    }

    private Category()
    {
    }

    public static Category Create(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Category name is required");
        }

        return new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Category name is required");
        }

        Name = name;
        Description = description ?? string.Empty;
        ModifiedAt = DateTime.UtcNow;
    }
}
