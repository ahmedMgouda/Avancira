import { Component, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DataTablesModule } from 'angular-datatables';
import { DataTableComponent } from '../../components/datatable/datatable.component';
import { ModalService } from '../../services/modal.service';


@Component({
  selector: 'app-tabletest',
  standalone: true,
  imports: [CommonModule, DataTablesModule, DataTableComponent],
  templateUrl: './tabletest.component.html',
  styleUrls: ['./tabletest.component.scss'],
})
export class TabletestComponent implements OnInit {
  @ViewChild('addModalContent') addModalContent!: TemplateRef<any>;
  @ViewChild('editModalContent') editModalContent!: TemplateRef<any>;

  constructor(private modalService: ModalService) { }

  // Define columns
  columns = [
    { key: 'name', title: 'Product Name' },
    { key: 'description', title: 'Description' },
    { key: 'dateAdded', title: 'Date Added' },
  ];

  // Sample data
  tableData = [
    { name: 'Aquatak High Pressure Wash.', description: 'Lorem ipsum dolor sit amet.', dateAdded: '2022-03-17' },
    { name: 'Bosch 1300W High Pressure...', description: 'Lorem ipsum dolor sit amet.', dateAdded: '2022-03-07' },
    { name: 'Car Wash Shampoo 250ml...', description: 'Lorem ipsum dolor sit amet.', dateAdded: '2022-03-14' },
    { name: 'Clear Coat Spray -Quick Dr...', description: 'Lorem ipsum dolor sit amet.', dateAdded: '2022-03-24' },
    { name: 'GoMechanic Neutron 600...', description: 'Lorem ipsum dolor sit amet.', dateAdded: '2022-03-16' },
  ];

  // Event handlers
  onPageChanged(event: { page: number; totalPages: number }) {
    console.log('📌 Page changed:', event);
  }

  onSearch(event: string) {
    console.log('🔍 Searching:', event);
  }

  onSorting(event: { column: string; direction: string }) {
    console.log('📌 Sorting:', event);
  }

  onPageLengthChanged(event: number) {
    console.log('📌 Items per page changed:', event);
  }

  onAdd(): void {
    console.log('Add button clicked! Implement logic here...');
    this.modalService.open('Add Item', this.addModalContent, this.saveEditItem.bind(this), this.closeModal.bind(this),"md");
  }
  
  onEdit(item: any) {
    console.log('✏️ Edit clicked:', item);
    this.modalService.open('Edit Item', this.editModalContent, this.saveEditItem.bind(this), this.closeModal.bind(this));
  }

  onDelete(item: any) {
    console.log('🗑️ Delete clicked:', item);
    this.modalService.openConfirmation(
      'Delete Item',
      'Are you sure you want to delete this item?',
      () => {
        console.log('Item deleted');
        // Perform delete action
      },
      () => {
        console.log('Action canceled');
      }
    );
  }
  saveEditItem() {
    console.log('✅ Edit Item: Saving updated data...');
  }

  closeModal() {
    console.log('❌ Modal closed without saving.');
  }

  ngOnInit(): void { }
}
