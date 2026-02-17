# Architecture Decisions for Approval History UI Refactoring

## Overview
This document outlines the architectural decisions made during the refactoring of the nested approval history table/list UI.

## Problem Statement
The original implementation had several issues:
1. Column text overlapped into adjacent columns when rows were nested/indented
2. Column order did not match business requirements
3. Parent nodes did not show computed completion dates (max from children)
4. Limited support for multi-level nesting (only 2 levels: parent/child)

## Component Structure

### Current Organization
```
OrderApprovalSystem/
├── Components/
│   └── ApprovalHistoryTreeView.xaml     # Reusable tree view component
├── Views/
│   └── ApprovalHistory.xaml             # Main view container
├── ViewModels/
│   └── vmApprovalHistory.cs             # View model
├── Models/
│   ├── mApprovalHistory.cs              # Data model
│   └── ApprovalHistoryNode.cs           # Tree node wrapper
└── Core/
    ├── Helpers/
    │   └── ApprovalHistoryTreeBuilder.cs # Tree building logic
    └── UI/
        └── Converters/
            └── LevelToIndentConverter.cs # Indentation converter
```

### Rationale for Component Placement

#### Components Folder
**Decision**: Keep `ApprovalHistoryTreeView.xaml` in the `Components` folder.

**Reasoning**:
- This is a **reusable UI component** that can be used in multiple views
- It has no view-specific logic - it's a pure presentation component
- It accepts data through data binding (DataContext)
- Following WPF best practices for component reusability

#### Core/Helpers Folder
**Decision**: Keep `ApprovalHistoryTreeBuilder.cs` in `Core/Helpers`.

**Reasoning**:
- This is **business logic**, not UI logic
- It's a pure utility class with no UI dependencies
- Can be unit tested independently
- Might be reused in other contexts (export, search, etc.)
- Belongs in Core as it's part of the application's core functionality

#### Core/UI/Converters Folder
**Decision**: Keep `LevelToIndentConverter.cs` in `Core/UI/Converters`.

**Reasoning**:
- This is **UI infrastructure**, not component-specific
- Converters are framework-level utilities in WPF
- Can be reused across different tree views or nested displays
- Standard WPF practice to organize converters separately

## Technical Changes

### 1. Column Reordering
**Change**: Reordered columns to match business requirements.

**New Order**:
1. Исполнитель (ФИО) - RecipientName (executor)
2. Срок - Term (deadline)
3. Состояние - Status (current state)
4. Результат - Result (outcome)
5. Дата поступления - ReceiptDate (receipt date)
6. Дата завершения - EffectiveCompletionDate (completion date)
7. Комментарий - Comment

**Removed Columns**:
- Роль получателя (RecipientRole) - redundant
- Получатель - merged into Исполнитель
- Роль отправителя (SenderRole) - not required
- Отправитель (SenderName) - not required
- Доработка visual indicator (IsRework) - represented by tree hierarchy

### 2. Column Width Adjustments
**Problem**: Fixed widths caused text overflow when indentation was applied (18px + 20px per level).

**Solution**:
- **Исполнитель**: Increased from 120px to 200px to accommodate:
  - Expander button: 18px
  - Level 0 indentation: 0px
  - Level 1 indentation: 20px
  - Level 2 indentation: 40px
  - Level 3 indentation: 60px
  - Remaining space for text: ~120-140px at deepest levels
- **Date columns**: Increased to 130px to fit full datetime format (dd.MM.yyyy HH:mm)
- **Состояние & Результат**: Set to 120px with ellipsis
- **Комментарий**: Set to `*` (flexible width) with text wrapping enabled

**Text Overflow Handling**:
- Most columns use `TextTrimming="CharacterEllipsis"` with ToolTip showing full text
- Comment column uses `TextWrapping="Wrap"` to show full content
- This prevents layout breakage while maintaining readability

### 3. Computed Completion Date

**Change**: Added `EffectiveCompletionDate` property to `ApprovalHistoryNode`.

**Implementation**:
```csharp
public DateTime? EffectiveCompletionDate
{
    get
    {
        if (HasChildren)
        {
            return GetMaxCompletionDateRecursive(this);
        }
        return Record?.CompletionDate;
    }
}
```

**Behavior**:
- **Parent nodes with children**: Returns the maximum completion date from all descendants
- **Leaf nodes**: Returns the node's own completion date
- **Recursive**: Works correctly at any nesting level
- **Null-safe**: Handles nodes without completion dates

**Business Logic**: This reflects when the entire approval chain (including all rework cycles) was completed.

### 4. Multi-Level Nesting Support

**Problem**: Original implementation only supported 2 levels (parent → child).

**Solution**: Enhanced `BuildTree` method to use a `Stack<ApprovalHistoryNode>`:

```csharp
var parentStack = new Stack<ApprovalHistoryNode>();

foreach (var record in sortedHistory)
{
    if (record.IsRework == true)
    {
        // Add as child and push to stack for potential grandchildren
        var currentParent = parentStack.Peek();
        var childNode = new ApprovalHistoryNode(record, currentParent.Level + 1);
        currentParent.Children.Add(childNode);
        parentStack.Push(childNode);
    }
    else
    {
        // New root-level node resets the stack
        var node = new ApprovalHistoryNode(record, 0);
        rootNodes.Add(node);
        parentStack.Clear();
        parentStack.Push(node);
    }
}
```

**Behavior**:
- Supports arbitrary nesting depth
- Each rework can have further rework (e.g., sent back → fixed → sent back again → fixed again)
- Level is automatically calculated based on parent's level + 1
- Stack tracks the current path through the tree

**Use Case**: Approval → Returned for rework → Resubmitted → Returned again → Finally approved

## Testing Considerations

### Unit Tests (Recommended)
Since no test framework exists in the project, tests are not added. However, these areas should be tested:

1. **ApprovalHistoryTreeBuilder.BuildTree**:
   - Single-level trees (no rework)
   - Two-level trees (one rework cycle)
   - Three+ level trees (multiple rework cycles)
   - Edge cases: all rework items, mixed scenarios

2. **ApprovalHistoryNode.EffectiveCompletionDate**:
   - Leaf node (returns own date)
   - Parent with one child (returns max)
   - Parent with multiple children (returns max)
   - Multi-level (returns deepest max)
   - Null dates handling

3. **LevelToIndentConverter**:
   - Level 0 (18px base)
   - Level 1 (38px = 18 + 20)
   - Level 2 (58px = 18 + 40)
   - Level 3+ (arbitrary depth)

### Manual Testing
1. View approval history with no rework items
2. View approval history with one-level rework
3. View approval history with multi-level rework (if data exists)
4. Verify column alignment with nested items
5. Verify text doesn't overlap
6. Verify completion dates are computed correctly for parent nodes
7. Test expand/collapse functionality at all levels

## Future Improvements

1. **Performance**: For very deep trees (10+ levels), consider:
   - Caching EffectiveCompletionDate instead of computing on each access
   - Virtualizing the TreeView for large datasets

2. **Accessibility**: 
   - Add keyboard navigation hints
   - Improve screen reader support for tree structure

3. **Responsiveness**:
   - Consider making column widths responsive to window size
   - Add horizontal scrolling for narrow windows

4. **Sorting/Filtering**:
   - Add ability to sort by completion date, status, etc.
   - Filter by specific executors or date ranges

## Breaking Changes

None. The changes are backward compatible:
- Existing data model unchanged
- Existing bindings work (using same property names)
- Only UI presentation modified
- TreeBuilder API unchanged (input/output same)

## Migration Notes

No migration required. Changes are purely in the presentation layer.
