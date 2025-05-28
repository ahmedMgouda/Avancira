import { Injectable } from '@angular/core';
import { DataStateChangeEventArgs } from '@syncfusion/ej2-angular-grids';

/**
 * Generic interface representing the grid state.
 */
export interface GridState<T> {
  currentPage: number;
  pageSize: number;
  sortField?: keyof T;
  sortDirection?: string;
  searchParams: Partial<T>;
}

/**
 * Represents the grid action payload.
 * This can be refined to match the structure of your grid's action object.
 */
export interface GridAction {
  currentPage?: number;
  pageSize?: number;
  requestType: string;
  currentSortingObject?: any; // Consider creating a more specific type if needed.
  columns?: any[]; // Consider creating a specific type for filtering columns.
}

@Injectable({
  providedIn: 'root'
})
export class GridStateService {
  /**
   * Extracts the grid state (paging, sorting, filtering) from the grid event.
   * @param state The DataStateChangeEventArgs from the grid.
   * @returns A GridState object for the generic model T.
   */
  updateState<T>(state: DataStateChangeEventArgs): GridState<T> {
    // Set default state values.
    const gridState: GridState<T> = {
      currentPage: 1,
      pageSize: 10,
      searchParams: {} as Partial<T>
    };

    if (state.action) {
      const action = state.action as GridAction;

      // --- Handle Paging ---
      this.handlePaging(action, gridState);

      // --- Handle Sorting ---
      if (action.requestType === 'sorting') {
        this.handleSorting(action, gridState);
      }

      // --- Handle Filtering ---
      if (action.requestType === 'filtering') {
        this.handleFiltering(action, gridState);
      }
    }

    return gridState;
  }

  /**
   * Updates the grid state with paging information.
   * @param action The grid action object.
   * @param gridState The grid state to update.
   */
  private handlePaging<T>(action: GridAction, gridState: GridState<T>): void {
    if (action.currentPage !== undefined && action.pageSize !== undefined) {
      gridState.currentPage = Number(action.currentPage);
      gridState.pageSize = Number(action.pageSize);

      console.log("current page: "+   gridState.currentPage  + " page size: "+ gridState.pageSize);
    }
  }

  /**
   * Updates the grid state with sorting information.
   * @param action The grid action object.
   * @param gridState The grid state to update.
   */
  private handleSorting<T>(action: GridAction, gridState: GridState<T>): void {
    // Try to get the sorting object from currentSortingObject.
    let sortingObj = action.currentSortingObject;
    // If it's undefined, and the action has sorting properties, fallback to action.
    if (!sortingObj && action.requestType === 'sorting') {
      sortingObj = action;
    }
    if (sortingObj) {
      // Use "field" if it exists, otherwise use "columnName" from the action.
      const sortFieldKey = sortingObj.field || sortingObj.columnName;
      console.log(
        "Sorted Field:",
        sortFieldKey
      );
      if (sortFieldKey) {
        gridState.sortField = sortFieldKey as keyof T;
        gridState.sortDirection = sortingObj.direction;
        console.log(
          "Sorted Field:",
          gridState.sortField,
          "Direction:",
          gridState.sortDirection
        );
      } else {
        console.warn("No valid sort field found in sorting details:", sortingObj);
      }
    }
  }

  /**
   * Updates the grid state with filtering information.
   * @param action The grid action object.
   * @param gridState The grid state to update.
   */
  private handleFiltering<T>(action: GridAction, gridState: GridState<T>): void {
    gridState.searchParams = {} as Partial<T>;
    if (action.columns && action.columns.length > 0) {
      action.columns.forEach((col: any) => {
        if (col.value != null && col.value !== '') {
          // Cast col.field to a key of T for type safety.
          gridState.searchParams[col.field as keyof T] = col.value;
        }
      });
    }
  }
}
