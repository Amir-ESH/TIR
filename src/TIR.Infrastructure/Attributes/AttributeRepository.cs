using TIR.Application.Contracts;
using TIR.Infrastructure.Data;

namespace TIR.Infrastructure.Attributes;

internal sealed class AttributeRepository(StoredProcedureExecutor executor) : IAttributeRepository
{
    public Task<AttributeDefinitionDto> CreateAsync(AttributeCreateRequest request, CancellationToken cancellationToken) =>
        executor.SingleAsync<AttributeDefinitionDto>("dbo.SP_AttributeDefinition_Create", request, cancellationToken);

    public Task<AttributeDefinitionDto?> GetAsync(long id, CancellationToken cancellationToken) =>
        executor.SingleOrDefaultAsync<AttributeDefinitionDto>("dbo.SP_AttributeDefinition_Get",
            new { Id = id }, cancellationToken);

    public Task<PageResult<AttributeDefinitionDto>> ListAsync(AttributeListRequest request, CancellationToken cancellationToken) =>
        executor.PageAsync<AttributeDefinitionDto>("dbo.SP_AttributeDefinition_List", request, cancellationToken);

    public Task<AttributeDefinitionDto> UpdateAsync(AttributeUpdateRequest request, CancellationToken cancellationToken) =>
        executor.SingleAsync<AttributeDefinitionDto>("dbo.SP_AttributeDefinition_Update", request, cancellationToken);

    public Task<ChangeStateDto> SoftDeleteAsync(StateChangeRequest request, CancellationToken cancellationToken) =>
        executor.SingleAsync<ChangeStateDto>("dbo.SP_AttributeDefinition_SoftDelete", request, cancellationToken);

    public Task<ChangeStateDto> RestoreAsync(StateChangeRequest request, CancellationToken cancellationToken) =>
        executor.SingleAsync<ChangeStateDto>("dbo.SP_AttributeDefinition_Restore", request, cancellationToken);

    public Task<AttributeOptionDto> CreateOptionAsync(AttributeOptionCreateRequest request, CancellationToken cancellationToken) =>
        executor.SingleAsync<AttributeOptionDto>("dbo.SP_AttributeOption_Create", request, cancellationToken);

    public Task<IReadOnlyList<AttributeOptionDto>> ListOptionsAsync(long attributeId, bool includeDisabled, CancellationToken cancellationToken) =>
        executor.ListAsync<AttributeOptionDto>("dbo.SP_AttributeOption_List",
            new { AttributeId = attributeId, IncludeDisabled = includeDisabled }, cancellationToken);
}
