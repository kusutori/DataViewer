# Table Control Decision

DataViewer should not migrate to the archived/legacy Windows Community Toolkit `DataGrid` path for new WinUI 3 work.

## Decision

Use `WinUI.TableView` as the result-grid control.

## Rationale

- Windows Community Toolkit `DataGrid` is not part of the v8+ WinUI 3 control set.
- Community Toolkit Labs `DataTable` is still an incubation/labs control.
- `WinUI.TableView` is actively maintained, targets WinUI/Windows App SDK, and is designed for large item counts with table features such as filtering and export.

## Integration Approach

- Keep `QueryResult` as the table data boundary.
- Host `WinUI.TableView.TableView` through Reactor `XamlHostElement`.
- Map each `QueryResult` row into a dictionary shape accepted by `WinUI.TableView`.
- Generate one `TableViewTextColumn` per result column.
- Do not couple DuckDB query execution to any specific table control.

## Current Status

The previous lightweight Reactor grid has been removed. The active implementation lives in `Controls/ResultTableView.cs`.

The next validation pass should cover:

- dynamic columns from query results,
- horizontal and vertical virtualization,
- copy/export behavior,
- dark/light theme consistency.
