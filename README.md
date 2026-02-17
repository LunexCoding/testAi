# OrderApprovalSystem

A WPF-based order approval system for managing technological orders with multi-level approval workflows.

## Recent Updates

### Nested Approval History UI Refactoring (February 2026)

The approval history tree view has been refactored to improve usability and support complex multi-level approval workflows:

**Key Improvements:**
- **Fixed column text overlap**: Column widths adjusted to prevent text from overlapping when rows are nested
- **Reordered columns**: Now displays in business-logical order: Executor, Deadline, State, Result, Receipt Date, Completion Date, Comment
- **Smart completion dates**: Parent nodes automatically show the latest completion date from all nested rework cycles
- **Multi-level nesting**: Full support for arbitrary nesting depth (approval → rework → rework → ... → completion)
- **Better text handling**: Ellipsis for long names/statuses, wrapping for comments, tooltips on hover

**Technical Details:**
See [ARCHITECTURE_DECISIONS.md](ARCHITECTURE_DECISIONS.md) for complete architectural documentation.

## Features

- **Order Management**: Create and manage technological orders
- **Approval Workflows**: Multi-stage approval process with role-based routing
- **Rework Handling**: Support for returning orders for revision with full history tracking
- **History Visualization**: Hierarchical tree view showing complete approval chain including all rework cycles
- **Role-Based Access**: Different views for Technologists, Managers, Guests, etc.

## Project Structure

```
OrderApprovalSystem/
├── Components/       # Reusable UI components
├── Views/           # Main application views
├── ViewModels/      # MVVM view models
├── Models/          # Data models and business entities
├── Core/
│   ├── Helpers/     # Business logic utilities
│   └── UI/          # UI infrastructure (converters, etc.)
├── Data/            # Entity Framework models and database context
└── Services/        # Application services
```

## Requirements

- .NET Framework 4.8
- Visual Studio 2017 or later
- Windows (WPF application)

## Build

Open `OrderApprovalSystem.sln` in Visual Studio and build the solution.

## License

[Add license information here]
