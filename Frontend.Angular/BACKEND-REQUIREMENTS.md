# Backend Requirements for Category Management

## 🚨 **CRITICAL: Unique SortOrder Constraint**

**All `sortOrder` values MUST be unique.** No two categories should ever have the same sortOrder.

### **Database Constraint**

```sql
ALTER TABLE Categories
ADD CONSTRAINT UQ_Categories_SortOrder UNIQUE (SortOrder);
```

This prevents duplicates at the database level.

---

## 🔄 **Required Endpoints**

### **1. Reorder Endpoint** (Drag-Drop)

```http
PUT /api/subject-categories/reorder
Content-Type: application/json

{
  "categoryIds": [3, 1, 2, 4, 5]
}
```

**Implementation (C#):**

```csharp
[HttpPut("reorder")]
public async Task<IActionResult> Reorder([FromBody] ReorderRequest request)
{
    const int Interval = 10; // Space between items (10, 20, 30...)
    
    // Assign new sortOrder to each category in the order received
    for (int i = 0; i < request.CategoryIds.Length; i++)
    {
        var category = await _context.Categories
            .FindAsync(request.CategoryIds[i]);
            
        if (category != null)
        {
            category.SortOrder = (i + 1) * Interval; // 10, 20, 30, 40...
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

**Behavior:**
- Receives array of category IDs in desired order
- Assigns sortOrder: 10, 20, 30, 40...
- All values are unique (guaranteed by interval)
- Returns 204 No Content on success

---

### **2. Move to Position Endpoint** (Cross-Page with Swap)

```http
PUT /api/subject-categories/{id}/move
Content-Type: application/json

{
  "targetSortOrder": 25
}
```

**Implementation (C#):**

```csharp
[HttpPut("{id}/move")]
public async Task<IActionResult> MoveToPosition(int id, [FromBody] MoveRequest request)
{
    var categoryToMove = await _context.Categories.FindAsync(id);
    if (categoryToMove == null)
    {
        return NotFound();
    }

    var currentSortOrder = categoryToMove.SortOrder;
    var targetSortOrder = request.TargetSortOrder;

    // Check if target position is already taken
    var categoryAtTarget = await _context.Categories
        .FirstOrDefaultAsync(c => c.SortOrder == targetSortOrder);

    if (categoryAtTarget != null)
    {
        // SWAP: Exchange sortOrder values
        categoryAtTarget.SortOrder = currentSortOrder;
        categoryToMove.SortOrder = targetSortOrder;
        
        _context.Categories.Update(categoryAtTarget);
    }
    else
    {
        // Target is free, just move
        categoryToMove.SortOrder = targetSortOrder;
    }

    _context.Categories.Update(categoryToMove);
    await _context.SaveChangesAsync();
    
    return NoContent();
}

public class MoveRequest
{
    public int TargetSortOrder { get; set; }
}
```

**Behavior:**
- If target sortOrder is **available**: Move category to that position
- If target sortOrder is **taken**: **SWAP** the two categories
- Ensures all sortOrder values remain unique
- Returns 204 No Content on success

**Example:**
```
Before:
Category A: sortOrder = 10
Category B: sortOrder = 20
Category C: sortOrder = 30

Move A to position 20:

After:
Category A: sortOrder = 20 (moved)
Category B: sortOrder = 10 (swapped)
Category C: sortOrder = 30 (unchanged)
```

---

## 🔒 **Ensuring Uniqueness**

### **Option 1: Database Constraint (Recommended)**

```sql
ALTER TABLE Categories
ADD CONSTRAINT UQ_Categories_SortOrder UNIQUE (SortOrder);
```

**Pros:**
- ✅ Enforced at database level
- ✅ Cannot be bypassed
- ✅ Works with any code

**Cons:**
- ⚠️ May cause issues during migrations
- ⚠️ Requires careful transaction handling for swaps

---

### **Option 2: Application-Level Validation**

If database constraint causes issues, validate in code:

```csharp
private async Task<bool> IsSortOrderUnique(int sortOrder, int? excludeId = null)
{
    return !await _context.Categories
        .AnyAsync(c => c.SortOrder == sortOrder && c.Id != excludeId);
}

// Use before assigning sortOrder
if (!await IsSortOrderUnique(newSortOrder, categoryId))
{
    // Handle conflict (e.g., swap or reject)
}
```

---

## ⚠️ **Important: Transaction Handling for Swaps**

When swapping, you might temporarily violate the unique constraint. Use transactions:

```csharp
[HttpPut("{id}/move")]
public async Task<IActionResult> MoveToPosition(int id, [FromBody] MoveRequest request)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    
    try
    {
        var categoryToMove = await _context.Categories.FindAsync(id);
        if (categoryToMove == null)
        {
            return NotFound();
        }

        var currentSortOrder = categoryToMove.SortOrder;
        var targetSortOrder = request.TargetSortOrder;

        var categoryAtTarget = await _context.Categories
            .FirstOrDefaultAsync(c => c.SortOrder == targetSortOrder);

        if (categoryAtTarget != null)
        {
            // Temporarily set to a unique value to avoid constraint violation
            var tempSortOrder = -1; // Or use DateTime.Now.Ticks
            
            categoryAtTarget.SortOrder = tempSortOrder;
            await _context.SaveChangesAsync();
            
            categoryToMove.SortOrder = targetSortOrder;
            await _context.SaveChangesAsync();
            
            categoryAtTarget.SortOrder = currentSortOrder;
            await _context.SaveChangesAsync();
        }
        else
        {
            categoryToMove.SortOrder = targetSortOrder;
            await _context.SaveChangesAsync();
        }

        await transaction.CommitAsync();
        return NoContent();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

---

## 📋 **Query Sorting**

Always sort by sortOrder with ID as tiebreaker (though IDs shouldn't be needed if sortOrder is unique):

```csharp
var categories = await _context.Categories
    .OrderBy(c => c.SortOrder)
    .ThenBy(c => c.Id)
    .ToListAsync();
```

---

## 🧰 **Testing Checklist**

### **Uniqueness:**
- [ ] Cannot create two categories with same sortOrder
- [ ] Cannot update category to duplicate sortOrder
- [ ] Swap correctly exchanges values

### **Reorder:**
- [ ] Drag-drop assigns 10, 20, 30... (all unique)
- [ ] Works with any number of categories
- [ ] Returns 204 on success

### **Move to Position:**
- [ ] Moving to empty position works
- [ ] Moving to occupied position swaps correctly
- [ ] Moving to same position does nothing
- [ ] Returns 404 if category not found
- [ ] Returns 204 on success

### **Edge Cases:**
- [ ] Moving last category to first position
- [ ] Moving first category to last position
- [ ] Swapping adjacent categories
- [ ] Concurrent updates handled correctly

---

## 📝 **API Response Examples**

### **Success (204 No Content)**
```http
HTTP/1.1 204 No Content
```

### **Not Found (404)**
```http
HTTP/1.1 404 Not Found
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Category with ID 123 not found"
}
```

### **Conflict (409) - If Swap Fails**
```http
HTTP/1.1 409 Conflict
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.8",
  "title": "Conflict",
  "status": 409,
  "detail": "Unable to move category: position conflict"
}
```

---

## ✅ **Summary**

| Requirement | Solution |
|-------------|----------|
| **Unique sortOrder** | Database constraint + validation |
| **Drag-drop reorder** | `PUT /reorder` assigns 10, 20, 30... |
| **Move to position** | `PUT /{id}/move` with swap logic |
| **Transaction safety** | Use transactions for swaps |
| **Query sorting** | `ORDER BY sortOrder, id` |

---

## 🚀 **Next Steps**

1. **Add database constraint** for sortOrder uniqueness
2. **Implement both endpoints** with examples above
3. **Use transactions** for swap operations
4. **Test thoroughly** with checklist above
5. **Return proper error codes** (204, 404, 409)
