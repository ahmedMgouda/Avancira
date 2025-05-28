import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpRequestAcceptedComponent } from './help-request-accepted.component';

describe('HelpRequestAcceptedComponent', () => {
  let component: HelpRequestAcceptedComponent;
  let fixture: ComponentFixture<HelpRequestAcceptedComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpRequestAcceptedComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpRequestAcceptedComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
