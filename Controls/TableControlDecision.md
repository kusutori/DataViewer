# Table Control Decision

DataViewer should not migrate to the archived/legacy Windows Community Toolkit `DataGrid` path for new WinUI 3 work.

## Decision

Use `WinUI.TableView` as the preferred migration target for the next table-control iteration.

## Rationale

- Windows Community Toolkit `DataGrid` is not part of the v8+ WinUI 3 control set.
- Community Toolkit Labs `DataTable` is still an incubation/labs control.
- `WinUI.TableView` is actively maintained, targets WinUI/Windows App SDK, and is designed for large item counts with table features such as filtering and export.

## Integration Approach

- Keep `QueryResult` as the table data boundary.
- Add a `TableViewAdapter` layer before replacing the current lightweight Reactor table.
- The adapter should map each `QueryResult` row into a dictionary/object shape accepted by `WinUI.TableView`.
- Do not couple DuckDB query execution to any specific table control.

## Current Status

The current Reactor table remains in place as a stable fallback. The next implementation step is a small spike that hosts `WinUI.TableView` from Reactor without XAML and validates:

- dynamic columns from query results,
- horizontal and vertical virtualization,
- copy/export behavior,
- dark/light theme consistency.
