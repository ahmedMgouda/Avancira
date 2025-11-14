# Category Management Improvements

## ✅ Completed Features

### Phase 1: Enhanced Forms + Cross-Cutting Services

#### 1. **Validator Service** (`category-validator.service.ts`)
- ✅ Async name uniqueness validator
- ✅ Custom name format validator (alphanumeric + spaces + hyphens)
- ✅ Positive sort order validator
- ✅ Reusable across all category forms

**Usage Example:**
```typescript
name: [
  '',
  [
    Validators.required,
    this.validatorService.validName()
  ],
  [this.validatorService.uniqueName(categoryId)]
]
```

#### 2. **Enhanced Form Component** (`category-form.component.ts`)
- ✅ Network awareness (disables form when offline)
- ✅ Better error handling with StandardError
- ✅ Toast notifications for user feedback
- ✅ Logger integration for debugging
- ✅ Custom validators integrated
- ✅ Improved error messages

### Phase 2: Drag-Drop Reordering

#### 3. **Drag-Drop Functionality**
- ✅ Visual drag-and-drop using Angular CDK
- ✅ Optimistic UI updates (instant feedback)
- ✅ Error rollback on failed reorder
- ✅ Only enabled when sorted by sortOrder (ascending)
- ✅ Helpful tip messages
- ✅ Loading indicators

**How It Works:**
1. User drags category row to new position
2. UI updates immediately (optimistic)
3. New order sent to backend API
4. Backend recalculates sortOrder (10, 20, 30...)
5. On success: toast notification + refresh
6. On error: revert to original order + error toast

---

## 🛠 Backend Requirements

### 1. **Reorder Endpoint**

```http
PUT /api/subject-categories/reorder
Content-Type: application/json

{
  "categoryIds": [3, 1, 2, 4, 5]
}
```

**Expected Behavior:**
- Receive array of category IDs in new order
- Recalculate sortOrder values (e.g., 10, 20, 30, 40, 50)
- Save to database
- Return 200 OK (or 204 No Content)

**Example Implementation (C#):**
```csharp
[HttpPut("reorder")]
public async Task<IActionResult> ReorderCategories([FromBody] ReorderRequest request)
{
    const int SortOrderInterval = 10;
    
    for (int i = 0; i < request.CategoryIds.Length; i++)
    {
        var category = await _context.Categories.FindAsync(request.CategoryIds[i]);
        if (category != null)
        {
            category.SortOrder = (i + 1) * SortOrderInterval;
        }
    }
    
    await _context.SaveChangesAsync();
    return NoContent();
}

public class ReorderRequest
{
    public int[] CategoryIds { get; set; }
}
```

### 2. **Unique Name Check Endpoint** (Optional - for async validator)

```http
GET /api/subject-categories/check-name?name=Electronics&excludeId=5

Response:
{
  "exists": false
}
```

**Note:** Currently disabled in validator service. Uncomment when backend is ready.

---

## 🎯 Next Steps (Not Yet Implemented)

### Phase 2 Remaining:

#### 4. **Bulk Operations**
- [ ] Multi-select checkboxes
- [ ] Bulk delete
- [ ] Bulk status change (activate/deactivate)
- [ ] Bulk visibility change

#### 5. **Enhanced Search & Filters**
- [ ] More filter options (date range, description search)
- [ ] Advanced filter panel
- [ ] URL state persistence (shareable filter URLs)
- [ ] Filter chips (active filters display)

---

## 📝 Usage Guide

### Drag-Drop Reordering

1. **Enable Drag-Drop:**
   - Click on "Sort Order" column header
   - Ensure ascending order (↑ icon)
   - Drag handle (☰) appears in first column

2. **Reorder Categories:**
   - Click and hold drag handle (☰)
   - Drag row to new position
   - Release to drop
   - Wait for "Order Saved" confirmation

3. **Troubleshooting:**
   - If drag handle not visible: Sort by "Sort Order" ascending
   - If drag disabled: Check network connection
   - If save fails: Check backend logs, order will revert

### Form Validation

1. **Real-time Validation:**
   - Errors show after field is touched
   - Async name check runs after 500ms pause

2. **Network Awareness:**
   - Form disables when offline
   - Warning toast appears on submit attempt when offline

---

## 🐛 Known Limitations

1. **Drag-drop only works on current page**
   - Cannot drag across pages
   - Reorder applies to visible items only
   - Solution: Increase page size or use filters

2. **Async validator delay**
   - 500ms debounce before checking name
   - Prevents excessive API calls
   - May feel slow for very fast typists

3. **No undo for reorder**
   - Once saved, cannot undo
   - Consider adding undo button in future

---

## 📊 Testing Checklist

### Drag-Drop:
- [ ] Can drag categories when sorted by sortOrder asc
- [ ] Cannot drag when sorted by other fields
- [ ] Loading indicator shows during save
- [ ] Success toast appears on successful reorder
- [ ] Error toast appears on failed reorder (test by stopping backend)
- [ ] Order reverts on error
- [ ] Refresh button reloads correct order

### Form Validation:
- [ ] Name required error shows
- [ ] Name format validator works (test with special chars)
- [ ] Async name check works (when backend ready)
- [ ] Form disabled when offline
- [ ] Toast shows when submitting offline
- [ ] All existing form features still work

### Cross-Cutting:
- [ ] Logger writes to console in dev mode
- [ ] Toast notifications appear and dismiss
- [ ] Network status accurate
- [ ] Loading states work correctly

---

## ⚙️ Configuration

### Angular CDK

If not already installed:
```bash
ng add @angular/cdk
```

Or manually:
```bash
npm install @angular/cdk
```

### Environment Variables

No new environment variables required. Uses existing:
- `environment.bffBaseUrl` - API base URL
- `environment.production` - Determines logging behavior

---

## 📚 Resources

- [Angular CDK Drag-Drop](https://material.angular.io/cdk/drag-drop/overview)
- [Angular Reactive Forms](https://angular.io/guide/reactive-forms)
- [Angular Custom Validators](https://angular.io/guide/form-validation#defining-custom-validators)
