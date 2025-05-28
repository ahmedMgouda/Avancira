import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpTheAvanciraGuidelinesComponent } from './help-the-avancira-guidelines.component';

describe('HelpTheAvanciraGuidelinesComponent', () => {
  let component: HelpTheAvanciraGuidelinesComponent;
  let fixture: ComponentFixture<HelpTheAvanciraGuidelinesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpTheAvanciraGuidelinesComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpTheAvanciraGuidelinesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
