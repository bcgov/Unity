# DataTables Plugins

This directory contains custom DataTables plugins following the official DataTables.net plugin development model.

## FilterRow Plugin

**File:** `filterRow.js`

A DataTables Feature Plugin that adds a filter row with individual column search inputs.

### Features

- Individual column filtering with text inputs
- Toggle button with Bootstrap popover controls
- Show/hide filter row
- Clear all filters functionality
- Automatic filter state persistence
- Proper lifecycle management (auto-cleanup on table destroy)

### Usage

#### For DataTables 1.x (Manual Initialization)

```javascript
// Initialize your DataTable
const table = $('#myTable').DataTable({
    // ... your options
});

// Initialize the FilterRow plugin
new $.fn.dataTable.FilterRow(table.settings()[0], {
    buttonId: 'btn-toggle-filter',
    buttonText: 'Filter',
    buttonTextActive: 'Filter*',
    enablePopover: true
});
```

#### For DataTables 2.x (Layout Integration)

```javascript
const table = $('#myTable').DataTable({
    layout: {
        top2: 'filterRow'
    },
    // ... other options
});
```

### Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `buttonId` | string | `'btn-toggle-filter'` | ID of the filter toggle button |
| `buttonText` | string | `'Filter'` | Text when no filters applied |
| `buttonTextActive` | string | `'Filter*'` | Text when filters active |
| `placeholderPrefix` | string | `''` | Prefix for input placeholders |
| `autoShow` | boolean | `false` | Auto-show filter row on init |
| `enablePopover` | boolean | `true` | Enable Bootstrap popover controls |
| `popoverPlacement` | string | `'bottom'` | Popover placement |

### API Methods

Access the FilterRow instance:

```javascript
const filterRow = table.filterRow();

// Public methods
filterRow.show();       // Show the filter row
filterRow.hide();       // Hide the filter row
filterRow.toggle();     // Toggle visibility
filterRow.clearFilters(); // Clear all filters
```

### Requirements

- DataTables 1.13+ or 2.x
- jQuery
- Bootstrap (for popover functionality - optional)

---

## Button Extensions

### csvNoPlaceholder

A CSV export button extension that automatically removes null placeholder characters from exported data.

### Usage

```javascript
buttons: [
    {
        extend: 'csvNoPlaceholder',
        text: 'Export',
        title: 'MyData'
    }
]
```

The button extends the standard DataTables `csv` button and includes automatic placeholder removal in the export format.

---

## API Extensions

### externalSearch()

Links an external search input to DataTable's search functionality with proper debouncing and cleanup.

### Usage

```javascript
const table = $('#myTable').DataTable({
    // ... options
});

// Bind external search input
table.externalSearch('#mySearchBox');

// With options
table.externalSearch('#mySearchBox', {
    delay: 500,           // Debounce delay in ms
    syncOnInit: true      // Sync search value on init
});
```

### Features

- Debounced input (default 300ms)
- Automatic event cleanup on table destroy
- Namespaced events to prevent conflicts
- Optional initial value synchronization

---

## Migration Guide

### From Old Implementation

**Old (Custom Functions):**
```javascript
// Manual filter row management
initializeFilterButtonPopover(dataTable);
updateFilter(dataTable, 'TableId', filterData);
searchFilter(dataTable);

// Custom column visibility
let buttons = getColumnToggleButtonsSorted(columns, dataTable);

// Manual CSV placeholder handling
let updatedButtons = removePlaceholderFromCvsExportButton(buttons, true, '—');

// Manual external search
setExternalSearchFilter(dataTable);
```

**New (Plugin-Based):**
```javascript
// FilterRow plugin handles everything
new $.fn.dataTable.FilterRow(dataTable.settings()[0], {
    buttonId: 'btn-toggle-filter'
});

// Official colvis button
buttons: [{
    extend: 'colvis',
    text: 'Columns'
}]

// CSV button extension
buttons: [{
    extend: 'csvNoPlaceholder',
    text: 'Export'
}]

// External search API
dataTable.externalSearch('#search');
```

### Benefits of New Approach

1. **Follows DataTables.net best practices** - Uses official plugin patterns
2. **Better lifecycle management** - Automatic cleanup on destroy
3. **Reduced code** - ~400 lines removed from table-utils.js
4. **More maintainable** - Plugins are self-contained and reusable
5. **Less complex state management** - Relies on DataTables' built-in state system
6. **Better performance** - Less manual DOM manipulation

---

## Loading the Plugins

### Recommended Load Order

```html
<!-- DataTables core -->
<script src="datatables.net/js/jquery.dataTables.min.js"></script>

<!-- DataTables Buttons extension -->
<script src="datatables.net-buttons/js/dataTables.buttons.min.js"></script>
<script src="datatables.net-buttons/js/buttons.html5.min.js"></script>

<!-- Custom plugins -->
<script src="themes/ux2/plugins/filterRow.js"></script>

<!-- Main table utilities -->
<script src="themes/ux2/table-utils.js"></script>
```

### Module Bundler

```javascript
import 'datatables.net';
import 'datatables.net-buttons';
import './plugins/filterRow.js';
import './table-utils.js';
```

---

## Browser Support

All plugins support the same browsers as DataTables:

- Chrome (latest)
- Firefox (latest)
- Safari (latest)
- Edge (latest)
- IE11 (with polyfills)

---

## License

These plugins are part of the Unity Grant Manager project and follow the same license as the main application.
