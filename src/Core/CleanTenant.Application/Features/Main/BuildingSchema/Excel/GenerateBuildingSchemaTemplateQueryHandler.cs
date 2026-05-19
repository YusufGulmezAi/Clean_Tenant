using MediatR;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Excel;

/// <summary>
/// <see cref="GenerateBuildingSchemaTemplateQuery"/> handler.
/// </summary>
public sealed class GenerateBuildingSchemaTemplateQueryHandler
    : IRequestHandler<GenerateBuildingSchemaTemplateQuery, Result<byte[]>>
{
    private readonly IBuildingSchemaExcelService _excelService;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GenerateBuildingSchemaTemplateQueryHandler(IBuildingSchemaExcelService excelService)
        => _excelService = excelService;

    /// <inheritdoc />
    public Task<Result<byte[]>> Handle(
        GenerateBuildingSchemaTemplateQuery query,
        CancellationToken cancellationToken)
    {
        var bytes = _excelService.GenerateTemplate();
        return Task.FromResult(Result<byte[]>.Success(bytes));
    }
}
