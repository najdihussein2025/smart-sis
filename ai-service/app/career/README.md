Career module (PB-17 / T66). `POST /career/explain` (service-key protected): the .NET API sends the student's top 3
careers with their scores, trait scores and passed courses (no personal data); the LLM (`LLM_MODEL`, default
`claude-opus-5-5`, key `LLM_API_KEY`) writes a short explanation. Without a key or on any LLM error the response is a
built-in text with `source: "fallback"`, and the .NET API then uses its own fallback. See PROJECT.md §9.
