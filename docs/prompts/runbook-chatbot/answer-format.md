# Structured answer format

Return exactly one JSON object. Do not add Markdown fences, commentary, or properties not listed here.

For an answer supported by the retrieved passages:

```json
{
  "status": "answered",
  "answer": "A concise answer supported by the retrieved text.",
  "citedChunkIds": ["exact-retrieved-chunk-id"]
}
```

When retrieved passages do not provide enough information:

```json
{
  "status": "insufficient_evidence",
  "answer": "There isn't enough information in the retrieved runbooks to answer that.",
  "citedChunkIds": []
}
```

`status` must be `answered` or `insufficient_evidence`. An answered response requires a non-empty answer and one or more exact chunk IDs from the supplied evidence. An insufficient-evidence response requires a non-empty explanation and an empty `citedChunkIds` array.
