# Copilot Instructions

## Project Guidelines
- Use English consistently for code comments and user-facing output strings; avoid mixed-language text.
- UI component choice is flexible as long as the stack is free/open-source for everyone; MudBlazor is only an initial suggestion, not a strict requirement.
- Blazor.Extensions.Canvas is approved as an optional free component for custom visualizations if needed.

## Testing Guidelines
- For xUnit in this project, long calculation tests should be skipped by default and only run in an explicit long-running test mode.