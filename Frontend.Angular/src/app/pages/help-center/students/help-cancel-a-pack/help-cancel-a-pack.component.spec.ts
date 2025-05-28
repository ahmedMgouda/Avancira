import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpCancelAPackComponent } from './help-cancel-a-pack.component';

describe('HelpCancelAPackComponent', () => {
  let component: HelpCancelAPackComponent;
  let fixture: ComponentFixture<HelpCancelAPackComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpCancelAPackComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpCancelAPackComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
