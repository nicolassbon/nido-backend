using System;
using System.Collections.Generic;

namespace Nido.Application.Productos;

public record ComparePricesResult(
    List<ComparedProductDto> Products,
    List<string> FailedScrapers,
    DateTime Timestamp
);
