import {
  Component, OnInit, OnDestroy, AfterViewInit, Input, Output, EventEmitter, ViewChild, TemplateRef, OnChanges, SimpleChanges
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { DataTablesModule } from 'angular-datatables';
import { Subject } from 'rxjs';
import { DataTableDirective } from 'angular-datatables';

@Component({
  selector: 'app-datatable',
  standalone: true,
  imports: [CommonModule, DataTablesModule],
  templateUrl: './datatable.component.html',
  styleUrls: ['./datatable.component.scss'],
})
export class DataTableComponent implements OnInit, AfterViewInit, OnDestroy, OnChanges {
  @ViewChild(DataTableDirective, { static: false }) dtElement?: DataTableDirective;

  @Input() tableData: any[] = [];
  @Input() columns: { key: string; title: string }[] = [];
  @Input() actionTemplate?: TemplateRef<any>;

  @Output() pageChanged = new EventEmitter<{ page: number; totalPages: number }>();
  @Output() searchEvent = new EventEmitter<string>();
  @Output() sortingEvent = new EventEmitter<{ column: string; direction: string }>();
  @Output() pageLengthChanged = new EventEmitter<number>();
  @Output() addClicked = new EventEmitter<void>();
  @Output() editClicked = new EventEmitter<any>();
  @Output() deleteClicked = new EventEmitter<any>();

  dtOptions: any = {};
  dtTrigger: Subject<any> = new Subject();

  ngOnInit(): void {
    console.log('ngOnInit: Initializing table options');
    this.dtOptions = {
      paging: true,
      searching: true,
      ordering: true,
      lengthMenu: [5, 10, 25, 50],
      pageLength: 5,
    };
  }

  ngAfterViewInit(): void {
    console.log('ngAfterViewInit: Checking DataTable');

    setTimeout(() => {
      if (this.dtElement) {
        console.log('dtElement is available, initializing DataTable...');
        this.dtTrigger.next(true);
        this.initializeEvents();
      } else {
        console.error('dtElement is STILL undefined! DataTable is not initialized.');
      }
    }, 100);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['tableData'] && !changes['tableData'].firstChange) {
      console.log('Table data changed, reloading DataTable...');
      this.refreshTable();
    }
  }

  refreshTable(): void {
    if (this.dtElement) {
      this.dtElement.dtInstance.then((dtInstance: any) => {
        console.log('Refreshing table...');
        dtInstance.clear();
        dtInstance.destroy();
        this.dtTrigger.next(true);
      });
    } else {
      console.error('dtElement is undefined in refreshTable!');
    }
  }

  ngOnDestroy(): void {
    console.log('ngOnDestroy: Cleaning up');
    this.dtTrigger.unsubscribe();
  }

  initializeEvents(): void {
    console.log('Initializing DataTable events...');
    this.dtElement?.dtInstance.then((dtInstance: any) => {
      dtInstance.on('page.dt', () => {
        const info = dtInstance.page.info();
        this.pageChanged.emit({ page: info.page + 1, totalPages: info.pages });
      });

      dtInstance.on('length.dt', (_e: any, _settings: any, len: number) => {
        this.pageLengthChanged.emit(len);
      });

      dtInstance.on('search.dt', () => {
        const searchTerm = dtInstance.search();
        this.searchEvent.emit(searchTerm);
      });

      dtInstance.on('order.dt', () => {
        const order = dtInstance.order()[0];
        const columnIdx = order[0];
        const direction = order[1] as "asc" | "desc";
        const columnKey = this.columns[columnIdx]?.key;

        if (columnKey) {
          this.sortingEvent.emit({ column: columnKey, direction });
        }
      });
    });
  }

  onAdd(): void {
    this.addClicked.emit();
  }
  
  onEdit(item: any): void {
    this.editClicked.emit(item);
  }

  onDelete(item: any): void {
    this.deleteClicked.emit(item);
  }
}
