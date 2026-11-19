using DevOpsAI.Rag.Application.Ports;

namespace DevOpsAI.Rag.Application.Documents;

public sealed class ListRunbookDocuments
{
    private readonly IRunbookDocumentStore _documentStore;

    public ListRunbookDocuments(IRunbookDocumentStore documentStore)
    {
        ArgumentNullException.ThrowIfNull(documentStore);
        _documentStore = documentStore;
    }

    public async Task<ListRunbookDocumentsResponse> ExecuteAsync(CancellationToken cancellationToken)
    {
        var documents = await _documentStore.ListAsync(cancellationToken).ConfigureAwait(false);

        return new ListRunbookDocumentsResponse(Array.AsReadOnly(documents.ToArray()));
    }
}
