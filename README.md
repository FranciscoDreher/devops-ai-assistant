# devops-ai-assistant

## AI request timeout

AI generation requests time out after 30 seconds by default. Configure `Ai:TimeoutSeconds`
in application settings or set the `Ai__TimeoutSeconds` environment variable. Values must
be between 1 and 86400 seconds.