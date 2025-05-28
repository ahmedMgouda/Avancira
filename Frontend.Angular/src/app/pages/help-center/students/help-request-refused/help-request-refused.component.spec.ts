import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpRequestRefusedComponent } from './help-request-refused.component';

describe('HelpRequestRefusedComponent', () => {
  let component: HelpRequestRefusedComponent;
  let fixture: ComponentFixture<HelpRequestRefusedComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpRequestRefusedComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpRequestRefusedComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
