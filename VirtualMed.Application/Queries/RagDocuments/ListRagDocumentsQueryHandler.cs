using MediatR;
using Microsoft.EntityFrameworkCore;
using VirtualMed.Application.Interfaces;
using VirtualMed.Application.RagDocuments;
using VirtualMed.Domain.Entities;

namespace VirtualMed.Application.Queries.RagDocuments;

public class ListRagDocumentsQueryHandler : IRequestHandler<ListRagDocumentsQuery, IReadOnlyList<RagDocumentDto>>
{
    private readonly IApplicationDbContext _context;

    public ListRagDocumentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<RagDocumentDto>> Handle(
        ListRagDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Set<RagDocument>()
            .AsNoTracking()
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new RagDocumentDto
            {
                Id = d.Id,
                FileName = d.FileName,
                Status = d.Status.ToString(),
                FileSizeBytes = d.FileSizeBytes,
                IndexedNodeCount = d.IndexedNodeCount,
                ErrorMessage = d.ErrorMessage,
                CreatedAt = d.CreatedAt,
                IndexedAt = d.IndexedAt
            })
            .ToListAsync(cancellationToken);
    }
}
