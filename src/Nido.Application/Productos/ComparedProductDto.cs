namespace Nido.Application.Productos;

public record ComparedProductDto(
    string Id,
    string Source,
    string Name,
    string Link,
    string Image,
    double Price,
    string Unit,
    double UnitPrice
);
