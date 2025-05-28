import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MultiStepModalComponent } from './multi-step-modal.component';

describe('MultiStepModalComponent', () => {
  let component: MultiStepModalComponent;
  let fixture: ComponentFixture<MultiStepModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MultiStepModalComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MultiStepModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
